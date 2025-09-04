namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AICore

open System
open System.Collections.Generic
open System.Text
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Features.AICore.Summarization
open JetBrains.ReSharper.Psi.Files
open FSharp.Compiler.Symbols
open JetBrains.Util

type private SummarizerContext() =
    let scopes =  Stack<string>()
    let summary = StringBuilder()

    member x.CurrentScope = scopes.TryPeek()
    member x.Summary = summary.ToString()

    member x.AddEntity(node: ITreeNode, representation: string) =
        let range = node.GetDocumentRange()
        let startLine, endLine =
            range.StartOffset.ToDocumentCoords().Line.Plus1(),
            range.EndOffset.ToDocumentCoords().Line.Plus1()

        [x.CurrentScope; representation; $"({startLine}-{endLine})"]
        |> Seq.filter _.IsNotEmpty()
        |> String.concat " "
        |> summary.AppendLine
        |> ignore

    member x.OpenScope(node: ITreeNode, scopeRepresentation: string, ?declarationInfo: string) =
        let currentScope = x.CurrentScope
        let scopeRepresentation =
            match node with
            | :? IModuleDeclaration -> scopeRepresentation
            | _ -> if isNull currentScope then scopeRepresentation else " " + scopeRepresentation

        scopes.Push(currentScope + scopeRepresentation)
        x.AddEntity(node, defaultArg declarationInfo "")
        { new IDisposable with member this.Dispose() = scopes.Pop() |> ignore }

type private FileSummarizerVisitor() =
    inherit TreeNodeVisitor<SummarizerContext>()

    static let displayContext = emptyDisplayContext

    static let formatFcsSymbolType (fcsSymbol: FSharpSymbol) =
        match fcsSymbol with
        | :? FSharpEntity as entity -> entity.AsType().Format(displayContext)
        | :? FSharpMemberOrFunctionOrValue as mfv -> formatMfv mfv displayContext true
        | _ -> "_"

    static let rec formatTypeUsage (typeUsage: ITypeUsage) =
        match typeUsage with
        | :? INamedTypeUsage as typeUsage ->
            formatTypeReferenceName typeUsage.ReferenceName

        | :? ITupleTypeUsage as typeUsage ->
            typeUsage.Items |> Seq.map formatTypeUsage |> String.concat " * "

        | :? IParenTypeUsage as typeUsage ->
            $"({formatTypeUsage typeUsage.InnerTypeUsage})"

        | :? IFunctionTypeUsage as typeUsage ->
            $"{formatTypeUsage typeUsage.ArgumentTypeUsage} -> {formatTypeUsage typeUsage.ReturnTypeUsage}"

        | :? IArrayTypeUsage as typeUsage ->
            $"{formatTypeUsage typeUsage.TypeUsage}[]"

        | :? IAnonRecordTypeUsage as typeUsage ->
           let fields =
               typeUsage.Fields
               |> Seq.map (fun field -> $"{field.ReferenceName.ShortName}: {formatTypeUsage field.TypeUsage}")
               |> String.concat "; "
           $"{{|{fields}|}}"

        | :? IWithNullTypeUsage as typeUsage ->
            $"{formatTypeUsage typeUsage.TypeUsage} | null"

        | _ -> ""

    and formatTypeReferenceName (typeName: ITypeReferenceName) =
        let name = typeName.Identifier.GetTypeParameterOrSourceName()
        let typeArgs = typeName.TypeArgumentList
        if isNull typeArgs then name else
        let typeUsages = typeArgs.TypeUsages
        if Seq.isEmpty typeUsages then name else

        let typeArgsRepresentation =
            typeUsages
            |> Seq.map formatTypeUsage
            |> String.concat ", "

        match typeArgs with
        | :? IPrefixAppTypeArgumentList -> $"{name}<{typeArgsRepresentation}>"
        | _ -> $"{typeArgsRepresentation} {name}"

    let formatTypeOrExtDeclaration (typeDecl: IFSharpTypeOrExtensionDeclaration) =
        let name = typeDecl.SourceName
        let typeArgs = typeDecl.TypeParameterDeclarationList
        if isNull typeArgs then name else
        let typeParams = typeArgs.TypeParameters
        if Seq.isEmpty typeParams then name else

        let typeArgsRepresentation =
            typeParams
            |> Seq.map _.NameIdentifier.GetTypeParameterOrSourceName()
            |> String.concat ", "

        match typeArgs with
        | :? IPostfixTypeParameterDeclarationList ->  $"{name}<{typeArgsRepresentation}>"
        | _ -> $"{typeArgsRepresentation} {name}"

    static let addBinding (binding: IBindingLikeDeclaration) (context: SummarizerContext) =
        let referencePat = binding.HeadPattern.As<IReferencePat>()
        if isNull referencePat then () else

        let typeRepr = referencePat.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"val {referencePat.GetFcsSymbol().DisplayName}: {typeRepr}"
        context.AddEntity(binding, representation)

    static let addConstructor (constructor: IConstructorSignatureOrDeclaration) (context: SummarizerContext) =
        let typeRepr = constructor.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"new: {typeRepr}"
        context.AddEntity(constructor, representation)

    static let addMember (memberDecl: IOverridableMemberDeclaration) (context: SummarizerContext) =
        let typeRepr = memberDecl.GetFcsSymbol() |> formatFcsSymbolType
        let accessorNames =
            match memberDecl with
            | :? IAccessorsNamesClauseOwner as prop ->
                match prop.AccessorsClause with
                | null -> Seq.empty
                | clause -> clause.AccessorsNamesEnumerable |> Seq.map _.Name

            | :? IMemberSignatureOrDeclaration as memberDecl ->
                memberDecl.AccessorDeclarationsEnumerable |> Seq.map _.NameIdentifier.Name

            | _ -> Seq.empty

        let accessors =
            if Seq.isEmpty accessorNames then "" else
            accessorNames
            |> String.concat ", "
            |> (+) " with "

        let representation = $"member {memberDecl.SourceName}: {typeRepr}" + accessors
        context.AddEntity(memberDecl, representation)

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode ->
                try
                    treeNode.Accept(x, context)
                with _ -> ()
            | _ -> ()

    override x.VisitGlobalNamespaceDeclaration(namespaceDecl, context) =
        context.AddEntity(namespaceDecl, "namespace global")
        x.VisitNode(namespaceDecl, context)

    override x.VisitNamedNamespaceDeclaration(namespaceDecl, context) =
        // Currently, we're not adding a namespace to the scope to not duplicate its name in tokens
        context.AddEntity(namespaceDecl, "namespace " + namespaceDecl.QualifiedName)
        x.VisitNode(namespaceDecl, context)

    override x.VisitNamedModuleDeclaration(moduleDecl, context) =
        // Currently, we're not adding a top-level module to the scope to not duplicate its name in tokens
        context.AddEntity(moduleDecl, "module " + moduleDecl.ClrName)
        x.VisitNode(moduleDecl, context)

    override x.VisitNestedModuleDeclaration(moduleDecl, context) =
        let scopeRepr =
            match context.CurrentScope with
            | null -> "module " + moduleDecl.SourceName
            | _ -> "." + moduleDecl.SourceName

        use _ = context.OpenScope(moduleDecl, scopeRepr)
        x.VisitNode(moduleDecl, context)

    override x.VisitFSharpTypeDeclaration(typeDecl: IFSharpTypeDeclaration, context) =
        let inheritMembers = typeDecl.TypeOrInterfaceInheritMembers
        let inheritsRepr =
            if Seq.isEmpty inheritMembers then "" else
            let inherits =
                inheritMembers
                |> Seq.map _.TypeName
                |> Seq.filter isNotNull
                |> Seq.map formatTypeReferenceName
                |> String.concat ", "
            if inherits = "" then "" else $"inherit {inherits}"

        let typeRepr = formatTypeOrExtDeclaration typeDecl

        use _ = context.OpenScope(typeDecl, $"type {typeRepr}", inheritsRepr)
        x.VisitNode(typeDecl, context)

    override x.VisitExceptionDeclaration(exceptionDecl, context) =
        use _ = context.OpenScope(exceptionDecl, $"exception {exceptionDecl.SourceName}")
        x.VisitNode(exceptionDecl, context)

    override x.VisitTypeExtensionDeclaration(extensionDecl: ITypeExtensionDeclaration, context) =
        let extensionRepr = formatTypeOrExtDeclaration extensionDecl

        use _ = context.OpenScope(extensionDecl, $"type {extensionRepr}", "with")
        x.VisitNode(extensionDecl, context)

    override x.VisitInterfaceImplementation(interfaceImpl, context) =
        let typeName = interfaceImpl.TypeName
        if isNull typeName then () else

        let interfaceRepr = formatTypeReferenceName typeName

        use _ = context.OpenScope(interfaceImpl, $"interface {interfaceRepr}")
        x.VisitNode(interfaceImpl, context)

    override x.VisitTopBinding(binding, context) =
        addBinding binding context

    override x.VisitBindingSignature(binding, context) =
        addBinding binding context

    override x.VisitMemberDeclaration(memberDecl, context) =
        addMember memberDecl context

    override x.VisitAbstractMemberDeclaration(memberDecl, context) =
        addMember memberDecl context

    override x.VisitMemberSignature(memberSign, context) =
        addMember memberSign context

    override x.VisitAutoPropertyDeclaration(autoPropDecl, context) =
        addMember autoPropDecl context

    override x.VisitPrimaryConstructorDeclaration(constructor, context) =
        addConstructor constructor context

    override x.VisitSecondaryConstructorDeclaration(constructor, context) =
        addConstructor constructor context

    override x.VisitConstructorSignature(constructor, context) =
        addConstructor constructor context


[<Language(typeof<FSharpLanguage>)>]
type FSharpFileSummarizer() =
    let summarizeFSharpFile (fsharpFile: IFSharpFile) =
        let context = SummarizerContext()
        fsharpFile.Accept(FileSummarizerVisitor(), context)
        context.Summary

    let processFile(file: IPsiSourceFile): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> summarizeStructure fsharpFile
        | _ -> ""

    let processImports(file: IPsiSourceFile, language: FSharpLanguage): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> getImportsFromFSharpFile fsharpFile
        | _ -> ""

    interface IRiderFileSummarizer with
        member this.GetSummary(file: IPsiSourceFile, _flavor: SummarizationFlavor) =
            file.GetPsiServices().Files.AssertAllDocumentAreCommitted()
            let fsharpLang = FSharpLanguage.Instance
            match fsharpLang with
            | null -> ""
            | _ ->
                let imports = processImports(file, language)
                let structure = processFile(file)
                let sb = StringBuilder()
                sb.AppendLine("##### Imports") |> ignore
                if imports = "" then sb.AppendLine("(none)") |> ignore else sb.AppendLine(imports) |> ignore
                sb.AppendLine() |> ignore
                sb.AppendLine("##### Structure") |> ignore
                sb.Append(structure) |> ignore
                sb.ToString()
