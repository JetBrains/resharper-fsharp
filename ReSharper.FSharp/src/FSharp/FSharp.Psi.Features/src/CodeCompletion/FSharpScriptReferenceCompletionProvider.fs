namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.Util

type FSharpScriptReferenceCompletionContext(context, token, completedPath, ranges, supportsNuget) =
    inherit SpecificCodeCompletionContext(context)

    override x.ContextId = "FSharpScriptReferenceCompletionContext"
    member x.Token = token
    member x.Ranges = ranges
    member x.CompletedPath = completedPath
    member x.SupportsNuget = supportsNuget


[<IntellisensePart>]
type FSharpScriptReferenceCompletionContextProvider() =
    inherit CodeCompletionContextProviderBase()

    override x.IsApplicable(context) =
        match context.File with
        | :? IFSharpFile as fsFile ->
            let caretOffset = context.CaretTreeOffset
            let token = fsFile.FindTokenAt(caretOffset - 1).As<ITokenNode>()
            if isNull token then false else

            match token.GetTokenType() with
            | tokenType when (tokenType == FSharpTokenType.STRING ||
                              tokenType == FSharpTokenType.VERBATIM_STRING ||
                              tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING) && (token.Parent :? IHashDirective) ->
                let caretOffset = caretOffset.Offset
                let tokenOffset = token.GetTreeStartOffset().Offset
                let tokenText = token.GetText()
                let valueStart = getStringStartingQuotesLength token
                let valueStartOffset = tokenOffset + valueStart
                let tokenEndOffset = tokenOffset + tokenText.Length

                let unfinishedLiteral = isUnfinishedString token
                caretOffset >= valueStartOffset &&
                (caretOffset = valueStartOffset || tokenText.IndexOf("nuget:", valueStart) = -1) &&
                (caretOffset < tokenEndOffset || caretOffset = tokenEndOffset && unfinishedLiteral)
            | _ -> false
        | _ -> false

    override x.GetCompletionContext(context) =
        let fsFile = context.File :?> IFSharpFile
        let token = fsFile.FindTokenAt(context.CaretTreeOffset - 1).As<ITokenNode>()
        let hashDirective = token.Parent :?> IHashDirective

        let startQuoteLength = getStringStartingQuotesLength token
        let argValue = getStringContent token

        let argOffset = token.GetTreeStartOffset().Offset
        let valueOffset = argOffset + startQuoteLength
        let caretOffset = context.CaretTreeOffset.Offset

        let caretValueOffset = caretOffset - argOffset - startQuoteLength
        let prefixValue = argValue.Substring(0, caretValueOffset)
        let supportsNuget =
            match hashDirective.HashToken.GetText() with
            | "#r" | "#reference" -> "nuget:".StartsWith(prefixValue.TrimStart([|' '|]))
            | _ -> false

        let prevSeparatorValueOffset = prefixValue.LastIndexOfAny(FileSystemDefinition.SeparatorChars)

        let rangesStart =
            let separatorLength = if prevSeparatorValueOffset < 0 then 0 else 1
            valueOffset + (max prevSeparatorValueOffset 0) + separatorLength

        let replaceRangeEnd = valueOffset + argValue.Length

        let document = context.Document
        let insertRange = DocumentRange(document, TextRange(rangesStart, caretOffset))
        let replaceRange = DocumentRange(document, TextRange(rangesStart, replaceRangeEnd))
        let ranges = TextLookupRanges(insertRange, replaceRange)

        let completedPath = VirtualFileSystemPath.TryParse(argValue.Substring(0, prevSeparatorValueOffset + 1), InteractionContext.SolutionContext)
        FSharpScriptReferenceCompletionContext(context, token, completedPath, ranges, supportsNuget) :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpScriptReferenceCompletionProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpScriptReferenceCompletionContext>()

    let getCompletionTarget (hashDirective: IHashDirective) =
        if isNull hashDirective then None else

        let hastToken = hashDirective.HashToken
        if isNull hastToken then None else

        let hashTokenText = hashDirective.HashToken.GetText()
        if hashTokenText.Length < 2 then None else

        match hashTokenText.Substring(1) with
        | "l"
        | "load" -> Some (fsExtensions, ProjectModelThemedIcons.Fsharp.Id)
        | "r"
        | "reference" -> Some (dllExtensions, ProjectModelThemedIcons.Assembly.Id)
        | "I"    -> Some (Set.empty, null)
        | _ -> None

    let getNugetItem (context: FSharpScriptReferenceCompletionContext) =
        let tailNodeTypes =
           [| FSharpTokenType.WHITESPACE
              TailType.CaretTokenNodeType.Instance |]

        let item =
            let info = TextualInfo("nuget:", "nuget", Ranges = context.Ranges)
            LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(fun _ -> TextPresentation(info, "", true))
                .WithBehavior(fun _ -> TextualBehavior(info))
                .WithMatcher(fun _ -> TextualMatcher(info))

        item.Placement.SetSelectionPriority(SelectionPriority.High)
        markRelevance item CLRLookupItemRelevance.ExpectedTypeMatchKeyword

        LookupItemUtil.SubscribeAfterComplete(item, fun textControl _ _ _ _ _ ->
            textControl.RescheduleCompletion(context.BasicContext.Solution))

        item.SetTailType(SimpleTailType(": ", tailNodeTypes, SkipTypings = [|":"; " "; ": "|]))
        item

    override x.IsAvailable _ = true
    override x.GetDefaultRanges(context) = context.Ranges
    override x.GetLookupFocusBehaviour _ = LookupFocusBehaviour.Soft

    override x.AddLookupItems(context, collector) =
        let filePath = context.BasicContext.SourceFile.GetLocation()
        let completedPath = context.CompletedPath.MakeAbsoluteBasedOn(filePath.Directory)

        let token = context.Token
        let hashDirective = token.Parent :?> _
        match getCompletionTarget hashDirective with
        | Some (allowedExtensions, iconId) ->
            let isAllowed (path: VirtualFileSystemPath) =
                Set.contains (path.ExtensionNoDot.ToLower()) allowedExtensions && not (path.Equals(filePath)) ||
                path.ExistsDirectory

            let addItem (lookupItem: TextLookupItemBase) =
                lookupItem.InitializeRanges(context.Ranges, context.BasicContext)
                collector.Add(lookupItem)

            let addFileItem path =
                if isAllowed path then
                    FSharpPathLookupItem(path, completedPath, iconId)
                    |> addItem

            if context.SupportsNuget then
                let item = getNugetItem context
                collector.Add(item)

            match context.BasicContext.CodeCompletionType with
            | BasicCompletion ->
                if not allowedExtensions.IsEmpty then
                    completedPath.GetChildFiles()
                    |> Seq.iter addFileItem

                completedPath.GetChildDirectories()
                |> Seq.iter (FSharpFolderCompletionItem >> addItem)

            | SmartCompletion ->
                if not allowedExtensions.IsEmpty then
                    let getFilesAsync = async {
                        return completedPath.GetChildFiles(flags = PathSearchFlags.RecurseIntoSubdirectories) }
                    getFilesAsync.RunAsTask()
                    |> Seq.iter addFileItem
            | _ -> ()
        | _ -> ()

        true


type FSharpPathLookupItem(path: VirtualFileSystemPath, basePath, iconId) =
    inherit TextLookupItemBase()

    override x.Image = iconId
    override x.Text = path.MakeRelativeTo(basePath).NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(path.Name, x.TextColor)
        if path.Parent <> basePath then
            let directoryPath = path.Parent.MakeRelativeTo(basePath)
            let directoryString = directoryPath.NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)
            LookupUtil.AddInformationText(name, $"(in {directoryString})")
        name


type FSharpFolderCompletionItem(path: VirtualFileSystemPath) =
    inherit TextLookupItemBase()

    override x.Image = ProjectModelThemedIcons.Directory.Id
    override x.Text = path.Name + "/"
    override x.GetDisplayName() = LookupUtil.FormatLookupString(path.Name, x.TextColor)

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)
        textControl.RescheduleCompletion(solution)


[<SolutionComponent>]
type FSharpScriptReferenceAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with

        member x.Language = FSharpLanguage.Instance :> _
        member x.AcceptsFile(file, textControl) =
            match file.As<IFSharpFile>() with
            | null -> false
            | fsFile ->

            let offset = TreeOffset(textControl.Caret.Offset() - 1)
            let token = fsFile.FindTokenAt(offset)
            if isNull token then false else

            match token.GetTokenType() with
            | null -> false
            | tokenType -> tokenType.IsStringLiteral && token.Parent :? IHashDirective

        member x.AcceptTyping(char, _, _) =
            not (Array.contains char FileSystemDefinition.InvalidPathChars)

        member x.ProcessSubsequentTyping(_, _) = true

        member x.IsEnabledInSettings(_, _) = AutopopupType.SoftAutopopup
        member x.ForceHideCompletion = false
