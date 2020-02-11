namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type FSharpElementFactory(languageService: IFSharpLanguageService, psiModule: IPsiModule) =
    let [<Literal>] moniker = "F# element factory"

    let namingService = NamingManager.GetNamingLanguageService(FSharpLanguage.Instance)

    let createDocument source =
        let documentFactory = Shell.Instance.GetComponent<IInMemoryDocumentFactory>()
        documentFactory.CreateSimpleDocumentFromText(source, moniker)

    let createFile source =
        let document = createDocument source
        let parser = languageService.CreateParser(document)

        let fsFile = parser.ParseFSharpFile(StandaloneDocument = document)
        SandBox.CreateSandBoxFor(fsFile, psiModule)
        fsFile

    let getModuleDeclaration source =
        let fsFile = createFile source
        fsFile.ModuleDeclarations.First()

    let getModuleMember source =
        let moduleDeclaration = getModuleDeclaration source
        moduleDeclaration.Members.First()

    let getDoDecl source =
        let moduleMember = getModuleMember source
        moduleMember.As<IDo>().NotNull()

    let getExpression source =
        let doDecl = getDoDecl source
        doDecl.Expression.NotNull()

    let createAppExpr addSpace =
        let space = if addSpace then " " else ""
        let source = sprintf "()%s()" space
        getExpression source :?> IAppExpr

    let createLetBinding bindingName =
        let source = sprintf "do (let %s = ())" bindingName
        let newExpr = getExpression source
        newExpr.As<IDoExpr>().Expression.As<IParenExpr>().InnerExpression.As<ILetOrUseExpr>()

    let setBindingExpression (expr: ISynExpr) (letBindings: #ILetBindings) =
        let newExpr = letBindings.Bindings.[0].SetExpression(expr.Copy())
        if not expr.IsSingleLine then
            let indentSize = expr.GetIndentSize()
            ModificationUtil.AddChildBefore(newExpr, NewLine(expr.GetLineEnding())) |> ignore
            ModificationUtil.AddChildBefore(newExpr, Whitespace(expr.Indent + indentSize)) |> ignore
            shiftExpr indentSize newExpr
        letBindings

    let createParenExpr (expr: ISynExpr) =
        let parenExpr = getExpression "(())" :?> IParenExpr
        ModificationUtil.ReplaceChild(parenExpr.InnerExpression, expr.Copy()) |> ignore
        parenExpr
    
    interface IFSharpElementFactory with
        member x.CreateOpenStatement(ns) =
            // todo: mangle ns
            let source = "open " + ns
            let moduleDeclaration = getModuleDeclaration source

            moduleDeclaration.Members.First() :?> _

        member x.CreateWildPat() =
            let source = "let _ = ()"
            let moduleDeclaration = getModuleDeclaration source

            let letModuleDecl = moduleDeclaration.Members.First().As<ILetModuleDecl>()
            let binding = letModuleDecl.Bindings.First()
            binding.HeadPattern :?> _

        member x.CreateIgnoreApp(expr, newLine) =
            let source = "() |> ignore"

            let indent = expr.Indent
            let newExpr = getExpression source

            match newExpr.As<IAppExpr>() with
            | null -> failwith "Could not get outer appExpr"
            | outerAppExpr ->

            match outerAppExpr.FunctionExpression.As<IAppExpr>() with
            | null -> failwith "Could not get inner appExpr"
            | innerAppExpr ->

            let expr = ModificationUtil.ReplaceChild(innerAppExpr.ArgumentExpression, expr.Copy())

            if newLine then
                ModificationUtil.ReplaceChild(expr.NextSibling, Whitespace(indent)) |> ignore
                ModificationUtil.AddChildBefore(expr.NextSibling, NewLine(expr.GetLineEnding())) |> ignore

            outerAppExpr

        member x.CreateRecordExprBinding(field, addSemicolon) =
            let field = namingService.MangleNameIfNecessary(field)
            let semicolon = if addSemicolon then ";" else ""

            let source = sprintf """{ %s = failwith "todo"%s }""" field semicolon
            let newExpr = getExpression source

            match newExpr.As<IRecordExpr>() with
            | null -> failwith "Could not get record expr"
            | recordExpr -> recordExpr.ExprBindings.First()

        member x.CreateAppExpr(funcName, argExpr) =
            let source = sprintf "%s ()" funcName
            let newExpr = getExpression source :?> IAppExpr
            let argExpr = if not (needsParens argExpr) then argExpr.Copy() else createParenExpr argExpr :> _
            newExpr.SetArgumentExpression(argExpr) |> ignore
            newExpr

        member x.CreateAppExpr(addSpace) =
            createAppExpr addSpace

        member x.CreateAppExpr(funExpr, argExpr, addSpace) =
            let appExpr = createAppExpr addSpace
            appExpr.SetFunctionExpression(funExpr.Copy()) |> ignore
            appExpr.SetArgumentExpression(argExpr.Copy()) |> ignore
            appExpr

        member x.CreateLetBindingExpr(bindingName) =
            createLetBinding bindingName

        member x.CreateLetBindingExpr(bindingName, expr) =
            let letOrUseExpr = createLetBinding bindingName
            setBindingExpression expr letOrUseExpr

        member x.CreateLetModuleDecl(bindingName, expr) =
            let source = sprintf "let %s = ()" bindingName
            let letModuleDecl = getModuleMember source  :?> ILetModuleDecl
            setBindingExpression expr letModuleDecl

        member x.CreateConstExpr(text) =
            getExpression text :?> _

        member x.CreateMatchExpr(expr) =
            let source = "match () with | _ -> ()"

            let indent = expr.Indent
            let newExpr = getExpression source

            match newExpr.As<IMatchExpr>() with
            | null -> failwith "Could not get outer appExpr"
            | matchExpr ->

            match matchExpr.Clauses.[0].As<IMatchClause>() with
            | null -> failwith "Could not get inner appExpr"
            | matchClause ->

            let expr = ModificationUtil.ReplaceChild(matchExpr.Expression, expr.Copy())

            let whitespace = ModificationUtil.ReplaceChild(matchClause.PrevSibling, Whitespace(indent))
            ModificationUtil.AddChildBefore(whitespace, NewLine(expr.GetLineEnding())) |> ignore

            matchExpr

        member x.CreateParenExpr() =
            getExpression "(())" :?> _

        member x.CreateParenExpr(expr) =
            createParenExpr expr

        member x.AsReferenceExpr(typeReference: ITypeReferenceName) =
            getExpression (typeReference.GetText()) :?> _

        member x.CreateReferenceExpr(name) =
            let source = sprintf "do %s" name
            let newExpr = getExpression source
            newExpr.As<IDoExpr>().Expression.As<IReferenceExpr>() :> _

        member x.CreateForEachExpr(expr) =
            let sourceFile = expr.GetSourceFile()
            let indent = expr.Indent + sourceFile.GetFormatterSettings(expr.Language).INDENT_SIZE

            let forExpr = getExpression "for _ in () do ()" :?> IForEachExpr

            let expr = ModificationUtil.ReplaceChild(forExpr.InClause, expr.Copy())
            let whitespace = ModificationUtil.ReplaceChild(forExpr.DoExpression.PrevSibling, Whitespace(indent))
            ModificationUtil.AddChildBefore(whitespace, NewLine(expr.GetLineEnding())) |> ignore

            forExpr

        member x.CreateExpr(text) =
            getExpression text

        member x.CreateBinaryAppExpr(text, arg1, arg2) =
            let source = "() " + text + " ()"
            let expr = getExpression source
            let appExpr = expr.As<IPrefixAppExpr>()
            ModificationUtil.ReplaceChild(appExpr.ArgumentExpression, arg2) |> ignore
            
            let infixApp = appExpr.FunctionExpression.As<IInfixAppExpr>()
            ModificationUtil.ReplaceChild(infixApp.ArgumentExpression, arg1) |> ignore

            expr
