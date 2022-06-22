namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Tokenization
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.RdBackend.Common.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

[<AllowNullLiteral>]
type FcsLookupCandidate(fcsTooltip: ToolTipElementData, xmlDocService: FSharpXmlDocService) =
    member x.Description = richText fcsTooltip.MainDescription
    member x.XmlDoc = fcsTooltip.XmlDoc

    member x.FcsTooltip = fcsTooltip

    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = x.Description
        member x.GetDescription() = xmlDocService.GetXmlDocSummary(x.XmlDoc)
        member x.Matches _ = true

        member x.GetParametersInfo(_, _) = ()
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
        member val IsFilteredOut = false with get, set

module FcsLookupCandidate =
    let getOverloads (ToolTipText(tooltips)) =
        tooltips |> List.collect (function ToolTipElement.Group(overloads) -> overloads | _ -> [])

    let getDescription (xmlDocService: FSharpXmlDocService) (fcsTooltip: ToolTipElementData) =
        let mainDescription = RichTextBlock(richText fcsTooltip.MainDescription)
        match xmlDocService.GetXmlDocSummary(fcsTooltip.XmlDoc) with
        | null -> ()
        | xmlDoc ->
            if not (RichTextBlock.IsNullOrWhiteSpace(mainDescription) || RichTextBlock.IsNullOrWhiteSpace(xmlDoc)) then
                mainDescription.AddLines(RichTextBlock(" "))
            mainDescription.AddLines(xmlDoc)
        mainDescription

type FcsErrorLookupItem(item: DeclarationListItem) =
    inherit TextLookupItemBase()

    override x.Image = null
    override x.Text = item.Name
    override x.Accept(_, _, _, _, _, _) = ()

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() =
            let (ToolTipText(tooltips)) = item.Description
            tooltips
            |> List.tryHead
            |> Option.bind (function | ToolTipElement.CompositionError e -> Some (RichTextBlock(e)) | _ -> None)
            |> Option.toObj


type FcsLookupItem(items: RiderDeclarationListItems, context: FSharpCodeCompletionContext) =
    inherit TextLookupItemBase()

    let [<Literal>] Id = "FcsLookupItem.OnAfterComplete"
    
    member this.FcsSymbolUse = items.SymbolUses.Head 
    member this.FcsSymbol = this.FcsSymbolUse.Symbol
    member this.NamespaceToOpen = items.NamespaceToOpen

    member x.Candidates =
        FcsLookupCandidate.getOverloads items.Description
        |> List.map (fun overload -> FcsLookupCandidate(overload, context.XmlDocService) :> ICandidate)

    override x.Image =
        try getIconId x.FcsSymbol
        with _ -> null

    override x.Text =
        FSharpKeywords.AddBackticksToIdentifierIfNeeded items.Name

    override x.DisplayTypeName =
        try
            match getReturnType x.FcsSymbol with
            | Some t -> RichText(t.Format(x.FcsSymbolUse.DisplayContext))
            | _ -> null
        with _ -> null

    override x.DisableFormatter = true

    override this.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill) =
        use pinCheckResultsCookie = textControl.GetFSharpFile(solution).PinTypeCheckResults(true, Id)
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill)

    override x.OnAfterComplete(textControl, nameRange, decorationRange, tailType, suffix, caretPositionRangeMarker) =
        base.OnAfterComplete(textControl, &nameRange, &decorationRange, tailType, &suffix, &caretPositionRangeMarker)

        // todo: 213: exit early if there's no need in additional binding
        context.BasicContext.Solution.GetPsiServices().Files.CommitAllDocuments()

        let fsFile = context.BasicContext.SourceFile.FSharpFile
        let psiServices = fsFile.GetPsiServices()

        use writeCookie = WriteLockCookie.Create(fsFile.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
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

            let ns = ns |> Array.map FSharpKeywords.AddBackticksToIdentifierIfNeeded |> String.concat "."
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
                FSharpReferenceBindingUtil.SetRequiredQualifiers(referenceOwner.Reference, clrDeclaredElement)

        if ns.IsEmpty() then () else
        addOpen offset fsFile context.BasicContext.ContextBoundSettingsStore moduleToImport

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(items.Name, x.TextColor)

        let ns = items.NamespaceToOpen
        if not (ns.IsEmpty()) then
            let ns = String.concat "." ns
            LookupUtil.AddInformationText(name, $"(in {ns})")

        name

    interface IParameterInfoCandidatesProvider with
        member x.HasCandidates = x.Candidates.Length > 1
        member x.CreateCandidates() = x.Candidates :> _

    interface IDescriptionProvidingLookupItem with
        /// Called when x.HasCandidates is false.
        member x.GetDescription() =
            match x.Candidates with
            | [] -> null
            | candidate :: _ ->

            let candidate = candidate :?> FcsLookupCandidate
            FcsLookupCandidate.getDescription context.XmlDocService candidate.FcsTooltip

    interface IRiderAsyncCompletionLookupItem
