namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AICore

open System
open System.Collections.Generic
open System.Text
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Features.AICore.Summarization
open JetBrains.ReSharper.Psi.Files
open FSharp.Compiler.Symbols
open JetBrains.Util

type private SummarizeContext =
    {
        Scopes: Stack<string>
        Summary: StringBuilder
    }
    member x.CurrentScope = x.Scopes.TryPeek()

    member x.AddEntity(node: ITreeNode, representation: string) =
        let textRange = node.GetDocumentRange().TextRange
        [x.CurrentScope; representation; $"({textRange.StartOffset}-{textRange.EndOffset})"]
        |> Seq.filter (fun x -> not (x.IsNullOrWhitespace()))
        |> String.concat " "
        |> x.Summary.AppendLine
        |> ignore

    member x.OpenScope(node: ITreeNode, scopeRepresentation: string, ?declarationInfo: string) =
        x.Scopes.Push(x.CurrentScope + scopeRepresentation)
        x.AddEntity(node, defaultArg declarationInfo "")
        { new IDisposable with
            member this.Dispose() = x.Scopes.Pop() |> ignore }


/// Надо ли для сигнатуры и реализации делать одинаково
type private FileSummarizerVisitor() =
    inherit TreeNodeVisitor<SummarizeContext>()

    //TODO: common
    static let displayContext = FSharpDisplayContext.Empty.WithShortTypeNames(true)
    // F# field?
    let formatFcsSymbolType (fcsSymbol: FSharpSymbol) =
        match fcsSymbol with
        | :? FSharpEntity as entity -> entity.AsType().Format(displayContext)
        | :? FSharpMemberOrFunctionOrValue as mfv -> formatMfv mfv displayContext true
        | _ -> "_"

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    //TODO: qualifiers
    override x.VisitNamedNamespaceDeclaration(namespaceDecl, context) =
        // Currently we're not adding namespace to the scope to not duplicate it name in tokens
        context.AddEntity(namespaceDecl, "namespace " + namespaceDecl.QualifiedName)
        x.VisitNode(namespaceDecl, context)

    /// module A.B.C
    override x.VisitNamedModuleDeclaration(moduleDecl, context) =
        use _ = context.OpenScope(moduleDecl, "module " + moduleDecl.SourceName)
        x.VisitNode(moduleDecl, context)

    override x.VisitNestedModuleDeclaration(moduleDecl, context) =
        let scopeRepr =
            match context.CurrentScope with
            | null -> "module " + moduleDecl.SourceName
            | _ -> "." + moduleDecl.SourceName

        use _ = context.OpenScope(moduleDecl, scopeRepr)
        x.VisitNode(moduleDecl, context)

    /// module A.B.C type T<...> inherits B<...>
    override x.VisitFSharpTypeDeclaration(typeDecl, context) =
        let inheritRepr =
            if isNull typeDecl.TypeInheritMember then "" else
            "inherit " + formatFcsSymbolType (typeDecl.TypeInheritMember.TypeName.Reference.GetFcsSymbol())

        let typeRepr = typeDecl.GetFcsSymbol() |> formatFcsSymbolType

        use _ = context.OpenScope(typeDecl, $" type {typeRepr}", inheritRepr)
        x.VisitNode(typeDecl, context)

    /// module A.B.C exception E
    override x.VisitExceptionDeclaration(exceptionDecl, context) =
        use _ = context.OpenScope(exceptionDecl, $" exception {exceptionDecl.SourceName}")
        x.VisitNode(exceptionDecl, context)

    /// module A.B.C type T<...> with
    override x.VisitTypeExtensionDeclaration(extensionDecl, context) =
        let extensionRepr = extensionDecl.GetFcsSymbol() |> formatFcsSymbolType

        use _ = context.OpenScope(extensionDecl, $" type {extensionRepr}", "with")
        x.VisitNode(extensionDecl, context)

    /// module A.B.C type T<...> interface I<...>
    override x.VisitInterfaceImplementation(interfaceImpl, context) =
        let interfaceRepr = interfaceImpl.FcsEntity |> formatFcsSymbolType

        use _ = context.OpenScope(interfaceImpl, $" interface {interfaceRepr}")
        x.VisitNode(interfaceImpl, context)

    /// module A.B.C let x: ...
    /// module A.B.C type T<...> let x: ...
    override x.VisitTopBinding(binding, context) =
        let referencePat = binding.HeadPattern.As<IReferencePat>()
        if isNull referencePat then () else

        let fcsSymbol = referencePat.GetFcsSymbol()
        let typeRepr = fcsSymbol |> formatFcsSymbolType
        let representation = $"val {fcsSymbol.DisplayName}: {typeRepr}"
        context.AddEntity(binding, representation)

    /// module A.B.C type T<...> member M: ...
    /// module A.B.C type T<...> interface I<...> member M: ...
    override x.VisitMemberDeclaration(memberDecl, context) =
        let typeRepr = memberDecl.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"member {memberDecl.SourceName}: {typeRepr}"
        context.AddEntity(memberDecl, representation)

    override x.VisitAutoPropertyDeclaration(autoPropDecl, context) =
        let typeRepr = autoPropDecl.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"member {autoPropDecl.SourceName}: {typeRepr} with get, set"
        context.AddEntity(autoPropDecl, representation)

    /// module A.B.C type T<...> new: ...
    override x.VisitPrimaryConstructorDeclaration(constructor, context) =
        let typeRepr = constructor.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"new: {typeRepr}"
        context.AddEntity(constructor, representation)

    /// module A.B.C type T<...> new: ...
    override x.VisitSecondaryConstructorDeclaration(constructor, context) =
        let typeRepr = constructor.GetFcsSymbol() |> formatFcsSymbolType
        let representation = $"new: {typeRepr}"
        context.AddEntity(constructor, representation)


[<Language(typeof<FSharpLanguage>)>]
type FSharpFileSummarizer() =
    let summarizeFSharpFile (fsharpFile: IFSharpFile) =
        let context = {
            Scopes = Stack()
            Summary = StringBuilder()
        }
        fsharpFile.Accept(FileSummarizerVisitor(), context)
        context.Summary.ToString()

    let processFile(file: IPsiSourceFile, language: FSharpLanguage): string =
        let psiFile = file.GetPrimaryPsiFile()
        match psiFile with
        | :? IFSharpFile as fsharpFile -> summarizeFSharpFile(fsharpFile)
        | _ -> ""

    interface IRiderFileSummarizer with
        member this.GetSummary(file: IPsiSourceFile, flavor: SummarizationFlavor) =
            file.GetPsiServices().Files.AssertAllDocumentAreCommitted()
            let fsharpLang = FSharpLanguage.Instance
            match fsharpLang with
            | null -> ""
            | language -> processFile(file, language)
