namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open FSharp.Compiler.Tokenization
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Host.Core.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FcsTaggedText
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell
open JetBrains.UI.RichText
open JetBrains.Util

type FSharpLookupCandidate(description: RichText, xmlDoc: FSharpXmlDoc, xmlDocService: FSharpXmlDocService) =
    member x.Description = description
    member x.XmlDoc = xmlDoc

    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = description
        member x.GetDescription() = xmlDocService.GetXmlDoc(xmlDoc)
        member x.Matches _ = true

        member x.GetParametersInfo(_, _) = ()
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
        member val IsFilteredOut = false with get, set


type FSharpErrorLookupItem(item: DeclarationListItem) =
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


type FSharpLookupItem(item: DeclarationListItem, context: FSharpCodeCompletionContext) =
    inherit TextLookupItemBase()

    let mutable candidates = Unchecked.defaultof<_>

    let addOpen ns =
        let offset = context.Ranges.InsertRange.StartOffset
        let fsFile = context.BasicContext.SourceFile.FSharpFile
        let psiServices = fsFile.GetPsiServices()

        use writeCookie = WriteLockCookie.Create(fsFile.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, "Add open")

        addOpen offset fsFile context.BasicContext.ContextBoundSettingsStore ns

    member x.Candidates =
        match candidates with
        | null ->
            let result = LocalList<ICandidate>()
            let (ToolTipText(tooltips)) = item.Description
            for tooltip in tooltips do
                match tooltip with
                | ToolTipElement.Group(overloads) ->
                    for overload in overloads do
                        result.Add(FSharpLookupCandidate(richTextR overload.MainDescription, overload.XmlDoc, context.XmlDocService))
                | ToolTipElement.CompositionError error ->
                    result.Add(FSharpLookupCandidate(RichText(error), FSharpXmlDoc.None, context.XmlDocService))
                | _ -> ()
            candidates <- result.ResultingList()
            candidates

        | candidates -> candidates

    override x.Image =
        try getIconId item.FSharpSymbol
        with _ -> null

    override x.Text =
        FSharpKeywords.QuoteIdentifierIfNeeded item.Name

    override x.DisplayTypeName =
        try
            match getReturnType item.FSharpSymbol with
            | Some t -> RichText(t.Format(context.FcsCompletionContext.DisplayContext))
            | _ -> null
        with _ -> null

    override x.DisableFormatter = true

    override x.OnAfterComplete(textControl, nameRange, decorationRange, tailType, suffix, caretPositionRangeMarker) =
        base.OnAfterComplete(textControl, &nameRange, &decorationRange, tailType, &suffix, &caretPositionRangeMarker)

        let ns = item.NamespaceToOpen
        if ns.IsEmpty() then () else

        let ns = ns |> Array.map FSharpKeywords.QuoteIdentifierIfNeeded |> String.concat "."

        let solution = context.BasicContext.Solution
        solution.GetPsiServices().Files.CommitAllDocuments()    
        addOpen ns

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(item.Name, x.TextColor)

        let ns = item.NamespaceToOpen
        if not (ns.IsEmpty()) then
            let ns = String.concat "." ns
            LookupUtil.AddInformationText(name, "(in " + ns + ")", itemInfoTextStyle)

        name

    interface IParameterInfoCandidatesProvider with
        member x.HasCandidates =
            match item.Kind with
            | CompletionItemKind.Method _ -> true
            | _ ->
                x.Candidates.Count > 1

        member x.CreateCandidates() = x.Candidates :> _

    interface IDescriptionProvidingLookupItem with
        /// Called when x.HasCandidates is false.
        member x.GetDescription() =
            let candidates = x.Candidates
            if candidates.Count = 0 then null else

            let candidate = candidates.[0] :?> FSharpLookupCandidate
            let isNullOrWhiteSpace = RichTextBlock.IsNullOrWhiteSpace

            let mainDescription = RichTextBlock(candidate.Description)
            match context.XmlDocService.GetXmlDoc(candidate.XmlDoc) with
            | null -> ()
            | xmlDoc ->
                if not (isNullOrWhiteSpace mainDescription || isNullOrWhiteSpace xmlDoc) then
                    mainDescription.AddLines(RichTextBlock(" "))
                mainDescription.AddLines(xmlDoc)
            mainDescription

    interface IRiderAsyncCompletionLookupItem
