namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Host.Features.Completion
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker.Settings
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.Text

type FSharpLookupCandidate(description: string, xmlDoc: FSharpXmlDoc, xmlDocService: FSharpXmlDocService) =
    member x.Description = description
    member x.XmlDoc = xmlDoc

    interface ICandidate with
        member x.GetSignature(_, _, _, _, _) = RichText(description)
        member x.GetDescription() = xmlDocService.GetXmlDoc(xmlDoc)
        member x.Matches(_) = true

        member x.GetParametersInfo(_, _) = ()
        member x.PositionalParameterCount = 0
        member x.IsObsolete = false
        member x.ObsoleteDescription = null
        member val IsFilteredOut = false with get, set


type FSharpErrorLookupItem(item: FSharpDeclarationListItem) =
    inherit TextLookupItemBase()

    override x.Image = null
    override x.Text = item.NameInCode
    override x.Accept(_, _, _, _, _, _) = ()

    interface IDescriptionProvidingLookupItem with
        member x.GetDescription() =
            let (FSharpToolTipText(tooltips)) = item.DescriptionTextAsync.RunAsTask()
            tooltips
            |> List.tryHead
            |> Option.bind (function | FSharpToolTipElement.CompositionError e -> Some (RichTextBlock(e)) | _ -> None)
            |> Option.toObj


type FSharpLookupItem(item: FSharpDeclarationListItem, context: FSharpCodeCompletionContext) =
    inherit TextLookupItemBase()

    let mutable candidates = Unchecked.defaultof<_>

    member x.Candidates =
        match box candidates with
        | null ->
            let result = LocalList<ICandidate>()
            let (FSharpToolTipText(tooltips)) = item.DescriptionTextAsync.RunAsTask()
            for tooltip in tooltips do
                match tooltip with
                | FSharpToolTipElement.Group(overloads) ->
                    for overload in overloads do
                        result.Add(FSharpLookupCandidate(overload.MainDescription, overload.XmlDoc, context.XmlDocService))
                | FSharpToolTipElement.CompositionError error ->
                    result.Add(FSharpLookupCandidate(error, FSharpXmlDoc.None, context.XmlDocService))
                | _ -> ()
            candidates <- result.ResultingList()
            candidates

        | candidates -> candidates :?> _

    override x.Image =
        try getIconId item.FSharpSymbol
        with _ -> null

    override x.Text = item.NameInCode

    override x.DisplayTypeName =
        try
            match getReturnType item.FSharpSymbol with
            | Some t -> RichText(t.Format(context.DisplayContext))
            | _ -> null
        with _ -> null

    override x.DisableFormatter = true

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)

        match item.NamespaceToOpen with
        | None -> ()
        | Some namespaceToOpen ->

        let line = int context.Coords.Line + 1
        let parseTree = context.FSharpFile.ParseResults.Value.ParseTree.Value
        let insertionPoint =
            let settings = context.BasicContext.ContextBoundSettingsStore
            if settings.GetValue(fun (key: FSharpOptions) -> key.TopLevelOpenCompletion) then
                OpenStatementInsertionPoint.TopLevel
            else
                OpenStatementInsertionPoint.Nearest

        let document = textControl.Document
        let context = ParsedInput.findNearestPointToInsertOpenDeclaration line parseTree [||] insertionPoint
        let pos = ParsedInput.adjustInsertionPoint (docLine >> document.GetLineText) context

        let isSystem = namespaceToOpen.StartsWith("System.", StringComparison.Ordinal) || namespaceToOpen = "System"
        let openPrefix = String(' ', pos.Column) + "open "
        let textToInsert = openPrefix + namespaceToOpen

        let line = pos.Line - 1 |> max 0
        let lineToInsert =
            seq { line - 1 .. -1 .. 0 }
            |> Seq.takeWhile (fun i ->
                let lineText = document.GetLineText(docLine i)
                lineText.StartsWith(openPrefix) &&
                (textToInsert < lineText || isSystem && not (lineText.StartsWith("open System"))))
            |> Seq.tryLast
            |> Option.defaultValue line

        // add empty line after all open expressions if needed
        let insertEmptyLine = not (document.GetLineText(docLine line).IsNullOrWhitespace())

        let prevLineEndOffset =
            if lineToInsert > 0 then document.GetLineEndOffsetWithLineBreak(docLine (max 0 (lineToInsert - 1)))
            else 0

        let newLineText = document.GetPsiSourceFile(solution).DetectLineEnding().GetPresentation()
        let emptyLine = (if insertEmptyLine then newLineText else "")
        document.InsertText(prevLineEndOffset, textToInsert + newLineText + emptyLine)

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(item.Name, x.TextColor)
        match item.NamespaceToOpen with
        | None -> ()
        | Some ns -> LookupUtil.AddInformationText(name, "(in " + ns + ")", itemInfoTextStyle)
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
