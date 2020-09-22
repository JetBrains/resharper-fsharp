module JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips

open System
open FSharp.Compiler.Layout
open FSharp.Compiler.SourceCodeServices
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText
open JetBrains.Util

let [<Literal>] RiderTooltipSeparator = "_RIDER_HORIZONTAL_LINE_TOOLTIP_SEPARATOR_"

[<SolutionComponent>]
type FSharpIdentifierTooltipProvider
        (lifetime, solution, presenter, xmlDocService: FSharpXmlDocService, textStylesService) =
    inherit IdentifierTooltipProvider<FSharpLanguage>(lifetime, solution, presenter, textStylesService)

    let layoutTagLookup =
        [
            LayoutTag.ActivePatternCase, FSharpHighlightingAttributeIds.ActivePatternCase
            LayoutTag.ActivePatternResult, FSharpHighlightingAttributeIds.ActivePatternCase
            LayoutTag.Alias, FSharpHighlightingAttributeIds.Class
            LayoutTag.Class, FSharpHighlightingAttributeIds.Class
            LayoutTag.Enum, FSharpHighlightingAttributeIds.Enum
            LayoutTag.Union, FSharpHighlightingAttributeIds.Union
            LayoutTag.UnionCase, FSharpHighlightingAttributeIds.UnionCase
            LayoutTag.Delegate, FSharpHighlightingAttributeIds.Delegate
            LayoutTag.Event, FSharpHighlightingAttributeIds.Event
            LayoutTag.Field, FSharpHighlightingAttributeIds.Field
            LayoutTag.Interface, FSharpHighlightingAttributeIds.Interface
            LayoutTag.Keyword, FSharpHighlightingAttributeIds.Keyword
            //LayoutTag.LineBreak
            LayoutTag.Local, FSharpHighlightingAttributeIds.Value
            LayoutTag.Record, FSharpHighlightingAttributeIds.Record
            LayoutTag.RecordField, FSharpHighlightingAttributeIds.Property
            LayoutTag.Method, FSharpHighlightingAttributeIds.Method
            LayoutTag.Member, FSharpHighlightingAttributeIds.Value
            LayoutTag.ModuleBinding, FSharpHighlightingAttributeIds.Value
            LayoutTag.Module, FSharpHighlightingAttributeIds.Module
            LayoutTag.Namespace, FSharpHighlightingAttributeIds.Namespace
            LayoutTag.NumericLiteral, FSharpHighlightingAttributeIds.Literal
            LayoutTag.Operator, FSharpHighlightingAttributeIds.Operator
            //LayoutTag.Parameter, FSharpHighlightingAttributeIds.Parameter
            LayoutTag.Parameter, FSharpHighlightingAttributeIds.Value
            LayoutTag.Property, FSharpHighlightingAttributeIds.Property
            //LayoutTag.Space
            LayoutTag.StringLiteral, FSharpHighlightingAttributeIds.String
            LayoutTag.Struct, FSharpHighlightingAttributeIds.Struct
            LayoutTag.TypeParameter, FSharpHighlightingAttributeIds.TypeParameter
            //LayoutTag.Text
            //LayoutTag.Punctuation
            // todo: what to use for these?
            LayoutTag.UnknownType, FSharpHighlightingAttributeIds.Class
            LayoutTag.UnknownEntity, FSharpHighlightingAttributeIds.Class
        ]
        |> List.map (fun (tag, attributeId) -> tag, TextStyle attributeId)
        |> readOnlyDict

    let emptyPresentation = RichTextBlock()

    let taggedTextToRichText layout =
        let result = RichText()

        layout
        |> renderL (taggedTextListR (fun text ->
            let style =
                match layoutTagLookup.TryGetValue text.Tag with
                | true, style -> style
                | false, _ -> TextStyle.Default

            // todo: replace spaces at the start of a line with \xA0 (non-breaking space)
            result.Append(text.Text, style) |> ignore
        ))
        |> ignore

        result

    let richTextJoin (sep : string) (parts : RichText seq) =
        let sep = RichText(sep, TextStyle.Default)
        parts
        |> Seq.fold
            (fun (result: RichText) part -> if result.IsEmpty then part else result + sep + part)
            RichText.Empty

    let [<Literal>] opName = "FSharpIdentifierTooltipProvider"

    static member GetFSharpToolTipText(userOpName: string, checkResults: FSharpCheckFileResults, token: FSharpIdentifierToken) =
        // todo: fix getting qualifiers
        let tokenNames = [token.Name]

        let sourceFile = token.GetSourceFile()
        let coords = sourceFile.Document.GetCoordsByOffset(token.GetTreeEndOffset().Offset)
        let lineText = sourceFile.Document.GetLineText(coords.Line)
        use cookie = CompilationContextCookie.GetOrCreate(sourceFile.GetPsiModule().GetContextFromModule())

        use op = token.CheckerService.FcsReactorMonitor.MonitorOperation userOpName

        // todo: provide tooltip for #r strings in fsx, should pass String tag
        let getTooltip = checkResults.GetStructuredToolTipText(int coords.Line + 1, int coords.Column, lineText, tokenNames, FSharpTokenTag.Identifier, op.OperationName)
        getTooltip.RunAsTask()

    override x.GetRichTooltip(highlighter) =
        if not highlighter.IsValid then emptyPresentation else

        let psiServices = solution.GetPsiServices()
        if not psiServices.Files.AllDocumentsAreCommitted || psiServices.Caches.HasDirtyFiles then emptyPresentation else

        let document = highlighter.Document
        match document.GetPsiSourceFile(solution) with
        | null -> emptyPresentation
        | sourceFile when not (sourceFile.IsValid()) -> emptyPresentation
        | sourceFile ->

        let documentRange = DocumentRange(document, highlighter.Range)
        match x.GetPsiFile(sourceFile, documentRange).As<IFSharpFile>() with
        | null -> emptyPresentation
        | fsFile ->

        match fsFile.FindTokenAt(documentRange.StartOffset).As<FSharpIdentifierToken>() with
        | null -> emptyPresentation
        | token ->

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> emptyPresentation
        | Some results ->

        let (FSharpToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(opName, results.CheckResults, token)

        layouts
        |> List.collect (function
            | FSharpStructuredToolTipElement.None
            | FSharpStructuredToolTipElement.CompositionError _ -> []

            | FSharpStructuredToolTipElement.Group(overloads) ->
                overloads
                |> List.map (fun overload ->
                    [ if not (isEmptyL overload.MainDescription) then
                          yield taggedTextToRichText overload.MainDescription

                      if not overload.TypeMapping.IsEmpty then
                          yield overload.TypeMapping |> List.map taggedTextToRichText |> richTextJoin "\n"

                      match xmlDocService.GetXmlDoc(overload.XmlDoc) with
                      | null -> ()
                      | xmlDocText when xmlDocText.Text.IsNullOrWhitespace() -> ()
                      | xmlDocText -> yield xmlDocText.RichText

                      match overload.Remarks with
                      | Some remarks when not (isEmptyL remarks) ->
                          yield taggedTextToRichText remarks
                      | _ -> () ]
                    |> richTextJoin "\n\n"))
        |> richTextJoin RiderTooltipSeparator
        |> RichTextBlock

    interface IFSharpIdentifierTooltipProvider
