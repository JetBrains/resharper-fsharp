namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
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

    let getDoDecl source =
        let moduleDeclaration = getModuleDeclaration source
        moduleDeclaration.Members.First().As<IDo>().NotNull()
    
    let getExpression source =
        let doDecl = getDoDecl source
        doDecl.Expression.NotNull()
    
    let createAppExpr addSpace =
        let space = if addSpace then " " else ""
        let source = sprintf "()%s()" space
        getExpression source :?> IAppExpr

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
                ModificationUtil.AddChildBefore(expr.NextSibling, NewLine(expr.FSharpFile.GetLineEnding())) |> ignore

            outerAppExpr

        member x.CreateRecordExprBinding(field, addSemicolon) =
            let field = namingService.MangleNameIfNecessary(field)
            let semicolon = if addSemicolon then ";" else ""

            let source = sprintf """{ %s = failwith "todo"%s }""" field semicolon
            let newExpr = getExpression source

            match newExpr.As<IRecordExpr>() with
            | null -> failwith "Could not get record expr"
            | recordExpr -> recordExpr.ExprBindings.First()

        member x.CreateAppExpr(funcName, expr) =
            let source = sprintf "%s ()" funcName
            let doDecl = getDoDecl source
            let appExpr = doDecl.Expression.As<IAppExpr>()
            ModificationUtil.ReplaceChild(appExpr.ArgumentExpression, expr) |> ignore
            appExpr

        member x.CreateAppExpr(addSpace) =
            createAppExpr addSpace

        member x.CreateAppExpr(funExpr, argExpr, addSpace) =
            let appExpr = createAppExpr addSpace
            appExpr.SetFunctionExpression(funExpr.Copy()) |> ignore
            appExpr.SetArgumentExpression(argExpr.Copy()) |> ignore
            appExpr

        member x.CreateLetBindingExpr(bindingName, expr) =
            let source = sprintf "do (let %s = ())" bindingName
            let newExpr = getExpression source
            let letOrUseExpr = newExpr.As<IDoExpr>().Expression.As<IParenExpr>().InnerExpression.As<ILetOrUseExpr>()
            ModificationUtil.ReplaceChild(letOrUseExpr.Bindings.[0].Expression, expr) |> ignore
            letOrUseExpr

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
            ModificationUtil.AddChildBefore(whitespace, NewLine(expr.FSharpFile.GetLineEnding())) |> ignore

            matchExpr

        member x.CreateParenExpr() =
            getExpression "(())" :?> _

        member x.AsReferenceExpr(typeReference: ITypeReferenceName) =
            getExpression (typeReference.GetText()) :?> _

        member x.CreateReferenceExpr(name) =
            let source = sprintf "do %s" name
            let newExpr = getExpression source
            newExpr.As<IDoExpr>().Expression.As<IReferenceExpr>() :> _
