namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.LanguageService

open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open FSharp.Compiler.Tokenization
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
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

        let fsFile = parser.ParseFSharpFile(noCache = true, StandaloneDocument = document)
        SandBox.CreateSandBoxFor(fsFile, psiModule)
        fsFile

    let getModuleDeclaration source =
        let fsFile = createFile source
        fsFile.ModuleDeclarations.First()

    let getModuleMember source =
        let moduleDeclaration = getModuleDeclaration source
        moduleDeclaration.Members.First()

    let getTypeDecl memberSource =
        let source = "type T =\n  " + memberSource
        let moduleMember = getModuleMember source
        moduleMember.As<ITypeDeclarationGroup>().TypeDeclarations.[0] :?> IFSharpTypeDeclaration

    let getExpressionStatement source =
        let moduleMember = getModuleMember source
        moduleMember.As<IExpressionStatement>().NotNull()

    let getExpression source =
        let exprStatement = getExpressionStatement source
        exprStatement.Expression.NotNull()

    let createAppExpr addSpace =
        let space = if addSpace then " " else ""
        let source = sprintf "()%s()" space
        getExpression source :?> IPrefixAppExpr

    let createLetExpr patternText =
        let newExpr = getExpression $"(let {patternText} = ())"
        newExpr.As<IParenExpr>().InnerExpression.As<ILetOrUseExpr>()

    let createLetDecl patternText =
        getModuleMember $"let {patternText} = ()" :?> ILetBindingsDeclaration

    let createMemberDecl logicalName typeParameters parameters =
        let typeParametersSource =
            match typeParameters with
            | [] -> ""
            | parameters -> parameters |> List.map ((+) "'") |> String.concat ", " |> sprintf "<%s>"

        let name =
            let name = PrettyNaming.DecompileOpName logicalName
            if PrettyNaming.IsMangledOpName logicalName then
                sprintf "( %s )" name
            else
                FSharpKeywords.AddBackticksToIdentifierIfNeeded name

        let memberSource = sprintf "member this.%s%s%s = failwith \"todo\"" name typeParametersSource parameters
        let typeDecl = getTypeDecl memberSource
        typeDecl.TypeMembers.[0] :?> IMemberDeclaration

    let createAttributeList attrName: IAttributeList =
        let source = sprintf "[<%s>] ()" attrName
        let exprStatement = getExpressionStatement source
        exprStatement.AttributeLists.[0]

    let createTypeUsage usage: ITypeUsage =
        let expr = createLetExpr (sprintf "(a: %s)" usage)
        expr.Bindings.[0].HeadPattern.As<IParenPat>().Pattern.As<ITypedPat>().TypeUsage

    interface IFSharpElementFactory with
        member x.CreateOpenStatement(ns) =
            // todo: mangle ns
            let source = "open " + ns
            let moduleDeclaration = getModuleDeclaration source

            moduleDeclaration.Members.First() :?> _

        member x.CreateWildPat() =
            let source = "let _ = ()"
            let moduleDeclaration = getModuleDeclaration source

            let letBindings = moduleDeclaration.Members.First().As<ILetBindingsDeclaration>()
            let binding = letBindings.Bindings.First()
            binding.HeadPattern :?> _

        member x.CreateSelfId(name: string) =
            let typeDecl = getTypeDecl $"member {name}.P = 1"
            let memberDecl = typeDecl.TypeMembers.[0] :?> IMemberDeclaration
            memberDecl.SelfId

        member x.CreateIgnoreApp(expr, newLine) =
            let source = "() |> ignore"

            let indent = expr.Indent
            let newExpr = getExpression source

            match newExpr.As<IBinaryAppExpr>() with
            | null -> failwith "Could not get outer appExpr"
            | binaryAppExpr ->

            let expr = ModificationUtil.ReplaceChild(binaryAppExpr.LeftArgument, expr.Copy())

            if newLine then
                ModificationUtil.ReplaceChild(expr.NextSibling, Whitespace(indent)) |> ignore
                ModificationUtil.AddChildBefore(expr.NextSibling, NewLine(expr.GetLineEnding())) |> ignore

            binaryAppExpr

        member x.CreateRecordFieldBinding(field, addSemicolon) =
            let field = namingService.MangleNameIfNecessary(field)
            let semicolon = if addSemicolon then ";" else ""

            let source = sprintf """{ %s = failwith "todo"%s }""" field semicolon
            let newExpr = getExpression source

            match newExpr.As<IRecordExpr>() with
            | null -> failwith "Could not get record expr"
            | recordExpr -> recordExpr.FieldBindings.First()

        member x.CreateAppExpr(funcName, argExpr) =
            let source = sprintf "%s ()" funcName
            let newExpr = getExpression source :?> IPrefixAppExpr
            let newArg = newExpr.SetArgumentExpression(argExpr.Copy())
            addParensIfNeeded newArg |> ignore
            newExpr

        member x.CreateAppExpr(funExpr, argExpr, addSpace) =
            let appExpr = createAppExpr addSpace
            appExpr.SetFunctionExpression(funExpr.Copy()) |> ignore
            appExpr.SetArgumentExpression(argExpr.Copy()) |> ignore
            appExpr

        member x.CreateLetBindingExpr(bindingName) =
            createLetExpr bindingName

        member x.CreateLetModuleDecl(patternText) =
            let source = $"let {patternText} = ()"
            getModuleMember source :?> ILetBindingsDeclaration

        member x.CreateMemberParamDeclarations(curriedParameterNames, isSpaceAfterComma, addTypes, displayContext) =
            let printParam (name, fcsType: FSharpType) =
                let name = FSharpKeywords.AddBackticksToIdentifierIfNeeded name
                if not addTypes then name else

                let fcsType = fcsType.Format(displayContext)
                sprintf "%s: %s" name fcsType

            let parametersSource =
                curriedParameterNames
                |> List.map (List.map printParam >> String.concat (if isSpaceAfterComma then ", " else ","))
                |> List.map (sprintf "(%s)")
                |> String.concat " "

            let memberBinding = createMemberDecl "P" List.empty parametersSource
            memberBinding.ParametersDeclarations |> Seq.toList

        member x.CreateMemberBindingExpr(name, typeParameters, parameters) =
            let parsedParams = "()" |> List.replicate parameters.Length |> String.concat " "
            let memberDecl = createMemberDecl name typeParameters parsedParams

            for realArg, fakeArg in Seq.zip parameters memberDecl.ParametersDeclarations do
                ModificationUtil.ReplaceChild(fakeArg, realArg) |> ignore
            memberDecl

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

        member x.CreateMatchClause() =
            let source = "match () with | _ -> failwith \"todo\""

            let matchExpr = getExpression source :?> IMatchExpr
            matchExpr.Clauses.[0]

        member x.CreateParenExpr() =
            getExpression "(())" :?> _

        member x.AsReferenceExpr(typeReference: ITypeReferenceName) =
            getExpression (typeReference.GetText()) :?> _

        member x.CreateReferenceExpr(name) =
            let source = sprintf "%s" name
            getExpression source :?> IReferenceExpr

        member x.CreateForEachExpr(expr) =
            let sourceFile = expr.GetSourceFile()
            let indent = expr.Indent + sourceFile.GetFormatterSettings(expr.Language).INDENT_SIZE

            let forExpr = getExpression "for _ in () do ()" :?> IForEachExpr

            let expr = ModificationUtil.ReplaceChild(forExpr.InExpression, expr.Copy())
            let whitespace = ModificationUtil.ReplaceChild(forExpr.DoExpression.PrevSibling, Whitespace(indent))
            ModificationUtil.AddChildBefore(whitespace, NewLine(expr.GetLineEnding())) |> ignore

            forExpr

        member x.CreateExpr(text) =
            getExpression text

        member x.CreateBinaryAppExpr(text, arg1: IFSharpExpression, arg2: IFSharpExpression) =
            let source = "() " + text + " ()"
            let expr = getExpression source
            let appExpr = expr :?> IBinaryAppExpr

            let leftArg = ModificationUtil.ReplaceChild(appExpr.LeftArgument, arg1.IgnoreInnerParens())
            addParensIfNeeded leftArg |> ignore

            let rightArg = ModificationUtil.ReplaceChild(appExpr.RightArgument, arg2.IgnoreInnerParens())
            addParensIfNeeded rightArg |> ignore

            expr

        member x.CreatePattern(text, topLevel) =
            let letBindings: ILetBindings =
                match topLevel with
                | true -> createLetDecl text :> _
                | _ -> createLetExpr text :> _

            letBindings.Bindings.[0].HeadPattern

        member x.CreateParenPat() =
            let expr = createLetExpr "(())"
            expr.Bindings.[0].HeadPattern.As<IParenPat>()

        member x.CreateTypedPat(pattern, typeUsage: ITypeUsage) =
            let settingsStore = typeUsage.GetSettingsStoreWithEditorConfig()
            let spaceBeforeColon = settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.SpaceBeforeColon)
            let preColonSpace = if spaceBeforeColon then " " else ""

            let expr = createLetExpr (sprintf "(_%s: _)" preColonSpace)
            let typedPat = expr.Bindings.[0].HeadPattern.As<IParenPat>().Pattern.As<ITypedPat>()

            ModificationUtil.ReplaceChild(typedPat.Pattern, pattern.Copy()) |> ignore
            ModificationUtil.ReplaceChild(typedPat.TypeUsage, typeUsage) |> ignore
            typedPat

        member x.CreateReturnTypeInfo(typeUsage: ITypeUsage): IReturnTypeInfo =
            let expr = createLetExpr "_: _"
            let returnTypeInfo = expr.Bindings.[0].ReturnTypeInfo
            ModificationUtil.ReplaceChild(returnTypeInfo.ReturnType, typeUsage) |> ignore
            returnTypeInfo

        member x.CreateTypeUsage(typeUsage: string) : ITypeUsage =
            createTypeUsage typeUsage

        member x.CreateSetExpr(left: IFSharpExpression, right: IFSharpExpression) =
            let source = "() <- ()"
            let expr = getExpression source
            let setExpr = expr :?> ISetExpr

            let leftArg = ModificationUtil.ReplaceChild(setExpr.LeftExpression, left.IgnoreInnerParens())
            addParensIfNeeded leftArg |> ignore

            let rightArg = ModificationUtil.ReplaceChild(setExpr.RightExpression, right.IgnoreInnerParens())
            addParensIfNeeded rightArg |> ignore

            expr

        member x.CreateExpressionReferenceName(name) =
            let source = sprintf "let %s = ()" name
            let letBindings = getModuleMember source :?> ILetBindingsDeclaration
            letBindings.Bindings.[0].HeadPattern.As<IReferencePat>().ReferenceName

        member x.CreateTypeReferenceName(name) =
            let source = sprintf "type T = %s" name
            let typeDeclarationGroup = getModuleMember source :?> ITypeDeclarationGroup
            let typeDeclaration = typeDeclarationGroup.TypeDeclarations.[0].As<IFSharpTypeDeclaration>()
            let typeAbbreviation = typeDeclaration.TypeRepresentation.As<ITypeAbbreviationRepresentation>()
            typeAbbreviation.AbbreviatedType.As<INamedTypeUsage>().ReferenceName

        member x.CreateEmptyAttributeList() =
            let attributeList = createAttributeList "Foo"
            ModificationUtil.DeleteChild(attributeList.Attributes.[0])
            attributeList

        member x.CreateAttribute(attrName) =
            let attributeList = createAttributeList attrName
            attributeList.Attributes.[0]

        member this.CreateTypeParameterOfTypeList(names) =
            let names = names |> List.map ((+) "'") |> String.concat ", "
            let source = $"type T<{names}> = class end"
            let moduleMember = getModuleMember source

            let typeDeclaration =
                moduleMember.As<ITypeDeclarationGroup>().TypeDeclarations.[0] :?> IFSharpTypeDeclaration

            typeDeclaration.TypeParameterDeclarationList
