namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open System.Collections.Generic
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl
open JetBrains.UI.Icons
open JetBrains.Util

type FSharpPathCompletionContext(context, token, completedPath, ranges) =
    inherit SpecificCodeCompletionContext(context)

    override x.ContextId = "FSharpPathCompletionContext"
    member x.Token = token
    member x.Ranges = ranges
    member x.CompletedPath = completedPath


[<IntellisensePart>]
type FSharpPathCompletionContextProvider() =
    inherit CodeCompletionContextProviderBase()

    override x.IsApplicable(context) =
        match context.File with
        | :? IFSharpFile as fsFile ->
            let caretOffset = context.CaretTreeOffset
            let token = fsFile.FindTokenAt(caretOffset - 1)
            if isNull token then false else

            match token.GetTokenType() with
            | tokenType when tokenType.IsStringLiteral && (token.Parent :? IHashDirective) ->
                let caretOffset = caretOffset.Offset
                let tokenOffset = token.GetTreeStartOffset().Offset
                let tokenText = token.GetText()
                let valueStartOffset = tokenOffset + tokenText.IndexOf("\"") + 1
                let tokenEndOffset = tokenOffset + tokenText.Length

                let unfinishedLiteral = tokenText.IndexOf('"') = tokenText.LastIndexOf('"')
                caretOffset >= valueStartOffset &&
                (caretOffset < tokenEndOffset || caretOffset = tokenEndOffset && unfinishedLiteral)
            | _ -> false
        | _ -> false

    override x.GetCompletionContext(context) =
        let fsFile = context.File :?> IFSharpFile
        let token = fsFile.FindTokenAt(context.CaretTreeOffset - 1)

        let arg = token.GetText()
        let startQuoteLength = arg.IndexOf('"') + 1
        let argValue = arg.Substring(startQuoteLength).TrimFromEnd("\"")

        let argOffset = token.GetTreeStartOffset().Offset
        let valueOffset = argOffset + startQuoteLength
        let caretOffset = context.CaretTreeOffset.Offset

        let caretValueOffset = caretOffset - argOffset - startQuoteLength
        let prevSeparatorValueOffset =
            argValue.Substring(0, caretValueOffset).LastIndexOfAny(FileSystemDefinition.SeparatorChars)

        let rangesStart =
            let separatorLength = if prevSeparatorValueOffset < 0 then 0 else 1 
            valueOffset + (max prevSeparatorValueOffset 0) + separatorLength

        let replaceRangeEnd =
            let valueReplaceEndOffset =
                match context.CodeCompletionType with
                | BasicCompletion ->
                    let nextSeparatorOffset = argValue.IndexOfAny(FileSystemDefinition.SeparatorChars, caretValueOffset)
                    if nextSeparatorOffset < 0 then argValue.Length else nextSeparatorOffset
                | SmartCompletion -> argValue.Length
                | completionType -> sprintf "Got completion type: %O" completionType |> failwith
            valueOffset + valueReplaceEndOffset

        let document = context.Document
        let insertRange = DocumentRange(document, TextRange(rangesStart, caretOffset))
        let replaceRange = DocumentRange(document, TextRange(rangesStart, replaceRangeEnd))
        let ranges = TextLookupRanges(insertRange, replaceRange)

        let completedPath = FileSystemPath.TryParse(argValue.Substring(0, prevSeparatorValueOffset + 1))
        FSharpPathCompletionContext(context, token, completedPath, ranges) :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpPathCompletionProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpPathCompletionContext>()

    let getCompletionTarget (hashDirective: IHashDirective) =
        if isNull hashDirective then None else

        let hastToken = hashDirective.HashToken
        if isNull hastToken then None else 

        let hashTokenText = hashDirective.HashToken.GetText()
        if hashTokenText.Length < 2 then None else

        match hashTokenText.Substring(1) with
        | "load" -> Some (fsExtensions, ProjectModelThemedIcons.Fsharp.Id)
        | "r"    -> Some (dllExtensions, ProjectModelThemedIcons.Assembly.Id)
        | "I"    -> Some (Set.empty, null)
        | _ -> None

    override x.IsAvailable(_) = true
    override x.GetDefaultRanges(context) = context.Ranges

    override x.AddLookupItems(context, collector) =
        let filePath = context.BasicContext.SourceFile.GetLocation()
        let completedPath = context.CompletedPath.MakeAbsoluteBasedOn(filePath.Directory)

        let token = context.Token
        let hashDirective = token.Parent :?> _
        match getCompletionTarget hashDirective with
        | Some (allowedExtensions, iconId) ->
            let isAllowed (path: FileSystemPath) =
                Set.contains (path.ExtensionNoDot.ToLower()) allowedExtensions && not (path.Equals(filePath)) ||
                path.ExistsDirectory

            let addItem (lookupItem: TextLookupItemBase) =
                lookupItem.InitializeRanges(context.Ranges, context.BasicContext)
                collector.Add(lookupItem)

            let addFileItem path =
                if isAllowed path then
                    FSharpPathLookupItem(path, completedPath, iconId)
                    |> addItem

            match context.BasicContext.CodeCompletionType with
            | BasicCompletion ->
                if not allowedExtensions.IsEmpty then
                    completedPath.GetChildFiles()
                    |> Seq.iter addFileItem

                let solution = context.BasicContext.Solution
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


type FSharpPathLookupItem(path: FileSystemPath, basePath, iconId) =
    inherit TextLookupItemBase()

    override x.Image = iconId
    override x.Text = path.MakeRelativeTo(basePath).NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)

    override x.GetDisplayName() =
        let name = LookupUtil.FormatLookupString(path.Name, x.TextColor)
        if path.Parent <> basePath then
            let directoryPath = path.Parent.MakeRelativeTo(basePath)
            let directoryString = directoryPath.NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)
            LookupUtil.AddInformationText(name, "(in " + directoryString + ")", itemInfoTextStyle)
        name


type FSharpFolderCompletionItem(path: FileSystemPath) =
    inherit TextLookupItemBase()

    override x.Image = ProjectModelThemedIcons.Directory.Id
    override x.Text = path.Name + "/"
    override x.GetDisplayName() = LookupUtil.FormatLookupString(path.Name, x.TextColor)

    override x.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret) =
        base.Accept(textControl, nameRange, insertType, suffix, solution, keepCaret)
        textControl.RescheduleCompletion(solution)
        

[<SolutionComponent>]
type FSharpPathAutocompletionStrategy() =
    interface IAutomaticCodeCompletionStrategy with

        member x.Language = FSharpLanguage.Instance :> _
        member x.AcceptsFile(file, textControl) =
            match file with
            | :? IFSharpFile as fsFile ->
                let offset = TreeOffset(textControl.Caret.Offset() - 1)
                let token = fsFile.FindTokenAt(offset)
                if isNull token then false else

                match token.GetTokenType() with
                | null -> false
                | tokenType -> tokenType.IsStringLiteral && token.Parent :? IHashDirective
            | _ -> false

        member x.AcceptTyping(char, textControl, _) = Array.contains char FileSystemDefinition.InvalidPathChars |> not
        member x.ProcessSubsequentTyping(_, _) = true

        member x.IsEnabledInSettings(_, _) = AutopopupType.HardAutopopup
        member x.ForceHideCompletion = false
