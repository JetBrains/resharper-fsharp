namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Feature.Services.ParameterInfo
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
open JetBrains.UI.Icons
open JetBrains.UI.RichText
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

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


type FSharpLookupItem
        (item: FSharpDeclarationListItem, context: FSharpCodeCompletionContext, displayContext: FSharpDisplayContext,
         xmlDocService: FSharpXmlDocService) =
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
                        result.Add(FSharpLookupCandidate(overload.MainDescription, overload.XmlDoc, xmlDocService))
                | FSharpToolTipElement.CompositionError error ->
                    result.Add(FSharpLookupCandidate(error, FSharpXmlDoc.None, xmlDocService))
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
            | Some t -> RichText(t.Format(displayContext))
            | _ -> null
        with _ -> null

    override x.DisableFormatter = true

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)

        // todo: move it to a separate type (and reuse in open namespace Quick Fix)
        if item.NamespaceToOpen.IsSome then
            let line = int context.Coords.Line + 1
            let parseTree = (context.BasicContext.File :?> IFSharpFile).ParseResults.Value.ParseTree.Value
            let insertionPoint = OpenStatementInsertionPoint.Nearest

            let context = ParsedInput.findNearestPointToInsertOpenDeclaration line parseTree [||] insertionPoint
            let document = textControl.Document
            let getLineText = fun lineNumber -> document.GetLineText(docLine lineNumber)
            let pos = context |> ParsedInput.adjustInsertionPoint getLineText

            let isSystem = item.NamespaceToOpen.Value.StartsWith("System.")
            let openPrefix = String(' ', pos.Column) + "open "
            let textToInsert = openPrefix + item.NamespaceToOpen.Value

            let line = pos.Line - 1 |> max 0
            let lineToInsert =
                seq { line - 1 .. -1 .. 0 }
                |> Seq.takeWhile (fun i ->
                    let lineText = document.GetLineText(docLine i)
                    lineText.StartsWith(openPrefix) &&
                    (textToInsert < lineText || isSystem && not (lineText.StartsWith("open System")))) // todo: System<smth> namespaces
                |> Seq.tryLast
                |> Option.defaultValue line

            // add empty line after all open expressions if needed
            let insertEmptyLine = not (document.GetLineText(docLine line).IsNullOrWhitespace())

            let prevLineEndOffset =
                if lineToInsert > 0 then document.GetLineEndOffsetWithLineBreak(docLine (max 0 (lineToInsert - 1)))
                else 0

            document.InsertText(prevLineEndOffset, textToInsert + "\n" + (if insertEmptyLine then "\n" else ""))

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(item.Name, x.TextColor)
        item.NamespaceToOpen
        |> Option.iter (fun ns -> LookupUtil.AddInformationText(name, "(in " + ns + ")", itemInfoTextStyle))
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
            match xmlDocService.GetXmlDoc(candidate.XmlDoc) with
            | null -> ()
            | xmlDoc ->
                if not (isNullOrWhiteSpace mainDescription || isNullOrWhiteSpace xmlDoc) then
                    mainDescription.AddLines(RichTextBlock(" "))
                mainDescription.AddLines(xmlDoc)
            mainDescription


type FSharpLookupAdditionalInfo =
    { Icon: IconId
      ReturnType: string }
