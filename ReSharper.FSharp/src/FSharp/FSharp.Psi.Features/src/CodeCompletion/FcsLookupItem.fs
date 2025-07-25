namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.RdBackend.Common.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

[<AllowNullLiteral>]
type FcsLookupCandidate(fcsTooltip: ToolTipElementData, xmlDocService: FSharpXmlDocService, psiModule: IPsiModule) =
    member x.Description = richText fcsTooltip.MainDescription
    member x.XmlDoc = fcsTooltip.XmlDoc

    member x.FcsTooltip = fcsTooltip

    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = x.Description
        member x.GetDescription _ = xmlDocService.GetXmlDocSummary(x.XmlDoc, fcsTooltip.Symbol, psiModule)
        member x.Matches _ = true

        member x.GetParametersInfo(_, _) = ()
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
        member val IsFilteredOut = false with get, set

module FcsLookupCandidate =
    let getOverloads (ToolTipText(tooltips)) =
        tooltips |> List.collect (function ToolTipElement.Group(overloads) -> overloads | _ -> [])

    let getDescription (xmlDocService: FSharpXmlDocService) (psiModule: IPsiModule) (fcsTooltip: ToolTipElementData) =
        let mainDescription = RichTextBlock(richText fcsTooltip.MainDescription)
        match xmlDocService.GetXmlDocSummary(fcsTooltip.XmlDoc, fcsTooltip.Symbol, psiModule) with
        | null -> ()
        | xmlDoc ->
            if not (RichTextBlock.IsNullOrWhiteSpace(mainDescription) || RichTextBlock.IsNullOrWhiteSpace(xmlDoc)) then
                mainDescription.AddLines(RichTextBlock(" "))
            mainDescription.AddLines(xmlDoc)
        mainDescription

type FcsErrorLookupItem(item: DeclarationListItem) =
    inherit TextLookupItemBase()

    override x.Image = null
    override x.Text = item.NameInList
    override x.Accept(_, _, _, _, _, _) = ()

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() =
            let (ToolTipText(tooltips)) = item.Description
            tooltips
            |> List.tryHead
            |> Option.bind (function | ToolTipElement.CompositionError e -> Some (RichTextBlock(e)) | _ -> None)
            |> Option.toObj

[<AllowNullLiteral>]
type IFcsLookupItemInfo =
    abstract FcsSymbol: FSharpSymbol
    abstract FcsSymbolUse: FSharpSymbolUse
    abstract NamespaceToOpen: string array

type FcsLookupItem(items: RiderDeclarationListItems, context: FSharpCodeCompletionContext) =
    inherit TextLookupItemBase()

    let mutable emphasize = false

    member this.AllFcsSymbolUses = items.SymbolUses
    member this.FcsSymbolUse = items.SymbolUses.Head
    member this.FcsSymbol = this.FcsSymbolUse.Symbol
    member this.NamespaceToOpen = items.NamespaceToOpen

    member this.Emphasize() =
        emphasize <- true
        this.Invalidate()

    interface IFcsLookupItemInfo with
        member this.FcsSymbol = this.FcsSymbol
        member this.NamespaceToOpen = this.NamespaceToOpen
        member this.FcsSymbolUse = this.FcsSymbolUse

    member x.Candidates =
        FcsLookupCandidate.getOverloads items.Description
        |> List.map (fun overload -> FcsLookupCandidate(overload, context.XmlDocService, context.PsiModule) :> ICandidate)

    member x.DisplayName =
        x.FcsSymbol.DisplayNameCore

    override x.Image =
        try getIconId x.FcsSymbol
        with _ -> null

    override x.Text =
        let name = x.DisplayName
        let name = if context.IsInAttributeContext then name.DropAttributeSuffix() else name
        FSharpNamingService.normalizeBackticks name

    override x.DisplayTypeName =
        try
            match getReturnType x.FcsSymbol with
            | Some t -> RichText(t.Format())
            | _ -> null
        with _ -> null

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        use pinCheckResultsCookie =
            Assertion.Assert(context.ParseAndCheckResults.IsValueCreated)
            textControl.GetFSharpFile(solution).PinTypeCheckResults(context.ParseAndCheckResults.Value)

        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

    override x.OnAfterComplete(textControl, nameRange, decorationRange, tailType, suffix, caretPositionRangeMarker) =
        base.OnAfterComplete(textControl, &nameRange, &decorationRange, tailType, &suffix, &caretPositionRangeMarker)

        // todo: 213: exit early if there's no need in additional binding
        context.BasicContext.Solution.GetPsiServices().Files.CommitAllDocuments()

        let fsFile = context.BasicContext.SourceFile.FSharpFile
        let psiServices = fsFile.GetPsiServices()

        use writeCookie = WriteLockCookie.Create(fsFile.IsPhysical())
        use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "Add open")

        let declaredElement = x.FcsSymbol.GetDeclaredElement(context.PsiModule)

        let typeElement =
            // todo: other declared elements
            match declaredElement with
            | :? ITypeElement as typeElement -> typeElement
            | :? IField as field when (field.ContainingType :? IEnum) -> field.ContainingType
            | _ -> null

        let ns = items.NamespaceToOpen
        let moduleToImport =
            if isNotNull typeElement then
                let moduleToOpen = getModuleToOpen typeElement
                ModuleToImport.DeclaredElement(moduleToOpen) else

            let ns = ns |> Array.map FSharpNamingService.normalizeBackticks |> String.concat "."
            ModuleToImport.FullName(ns)

        let offset = context.Ranges.InsertRange.StartOffset
        // todo: getting reference owner in parse errors, e.g. unfinished `if`

        let referenceOwner = fsFile.GetNode<IFSharpReferenceOwner>(offset)
        if isNotNull referenceOwner && isNotNull typeElement && not referenceOwner.Reference.IsQualified then
            let clrDeclaredElement: IClrDeclaredElement =
                // todo: other elements: union cases
                match declaredElement with
                | :? ITypeElement as typeElement -> typeElement :> _
                | :? IField as field when (field.ContainingType :? IEnum) -> field :> _
                | _ -> null

            if isNotNull clrDeclaredElement then
                referenceOwner.Reference.SetRequiredQualifiers(clrDeclaredElement, referenceOwner)

        if not (ns.IsEmpty()) then
            addOpen offset fsFile moduleToImport

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(items.Name, x.TextColor)

        if emphasize then
            LookupUtil.AddEmphasize(name, TextRange(0, name.Length))

        let ns = items.NamespaceToOpen
        if not (ns.IsEmpty()) then
            let ns = String.concat "." ns
            LookupUtil.AddInformationText(name, $"(in {ns})")

        name

    interface IParameterInfoCandidatesProvider with
        member x.HasCandidates = x.Candidates.Length > 1
        member x.CreateCandidates _ = x.Candidates :> _

    interface IDescriptionProvidingLookupItem with
        /// Called when x.HasCandidates is false.
        member x.GetDescription() =
            match x.Candidates with
            | [] -> null
            | candidate :: _ ->

            let candidate = candidate :?> FcsLookupCandidate
            FcsLookupCandidate.getDescription context.XmlDocService context.PsiModule candidate.FcsTooltip

    interface IRiderAsyncCompletionLookupItem
