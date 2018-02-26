namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion

open System
open System.Collections.Generic
open JetBrains.DocumentModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.Icons
open JetBrains.Util

type FSharpPathCompletionContext(context, token, ranges) =
    inherit SpecificCodeCompletionContext(context)

    override x.ContextId = "FSharpPathCompletionContext"
    member x.Token = token
    member x.Ranges = ranges


[<IntellisensePart>]
type FSharpPathCompletionContextProvider() =
    inherit CodeCompletionContextProviderBase()

    override x.IsApplicable(context) =
        match context.File with
        | :? IFSharpFile as fsFile ->
            let token = fsFile.FindTokenAt(context.CaretTreeOffset - 1)
            match token.GetTokenType() with
            | null -> false
            | tokenType -> isNotNull tokenType && tokenType.IsStringLiteral && token.Parent :? IHashDirective
        | _ -> false

    override x.GetCompletionContext(context) =
        let fsFile = context.File :?> IFSharpFile
        let token = fsFile.FindTokenAt(context.CaretTreeOffset)
        
        let document = context.Document
        let caretOffset = context.CaretTreeOffset.Offset
        let docRange = DocumentRange(document, TextRange(caretOffset, caretOffset))
        let ranges = TextLookupRanges(docRange, docRange)
        
        FSharpPathCompletionContext(context, token, ranges) :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpPathCompletionProvider() =
    inherit ItemsProviderOfSpecificContext<FSharpPathCompletionContext>()

    static let fsExtensions = ["fs"; "fsi"; "fsx"; "fsscript"] |> Set.ofList
    static let dllExtensions = ["dll"; "exe"] |> Set.ofList

    let getCompletionTarget (hashDirective: IHashDirective) =
        let hashTokenText = hashDirective.HashToken.GetText()
        if hashTokenText.Length < 2 then None else

        let id = hashTokenText.Substring(1)
        if id.Equals("load", StringComparison.Ordinal) then Some (fsExtensions, ProjectModelThemedIcons.Fsharp.Id) else
        if id.Equals("r", StringComparison.Ordinal) then Some (dllExtensions, ProjectModelThemedIcons.Assembly.Id) else
        if id.Equals("I", StringComparison.Ordinal) then Some (Set.empty, null) else
        None

    override x.IsAvailable(_) = true
    override x.GetDefaultRanges(context) = context.Ranges

    override x.AddLookupItems(context, collector) =
        let basicContext = context.BasicContext
        let caretOffset = basicContext.CaretTreeOffset.Offset

        let token = context.Token
        let arg = token.GetText()
        let stringValueOffset = arg.IndexOf('"') + 1
        let insideTokenCaretOffset = caretOffset - token.GetTreeStartOffset().Offset - stringValueOffset

        let completedPart = arg.Substring(stringValueOffset, insideTokenCaretOffset)
        let completedPartLenght =
            let separatorIndex = completedPart.LastIndexOf('/')
            if separatorIndex < 0 then 0 else separatorIndex

        let filePath = basicContext.SourceFile.GetLocation()
        let completedRelativePath = FileSystemPath.TryParse(completedPart.Substring(0, completedPartLenght))
        let completedPath = completedRelativePath.MakeAbsoluteBasedOn(filePath.Directory)

        let hashDirective = token.Parent :?> _
        match getCompletionTarget hashDirective with
        | Some (allowedExtensions, iconId) ->
            let isAllowed (path: FileSystemPath) =
                Set.contains (path.ExtensionNoDot.ToLower()) allowedExtensions && not (path.Equals(filePath)) ||
                path.ExistsDirectory

            let addLookupItem iconId path =
                if isAllowed path then
                    let lookupItem = FSharpPathLookupItem(path, completedPath, filePath, iconId)
                    lookupItem.InitializeRanges(context.Ranges, context.BasicContext)
                    collector.Add(lookupItem)

            match basicContext.CodeCompletionType with
            | BasicCompletion ->
                if not allowedExtensions.IsEmpty then
                    completedPath.GetChildFiles()
                    |> Seq.iter (addLookupItem iconId)

                completedPath.GetChildDirectories()
                |> Seq.iter (addLookupItem ProjectModelThemedIcons.Directory.Id)

            | SmartCompletion ->
                // todo: change insert range
                if not allowedExtensions.IsEmpty then
                    let getFilesAsync = async {
                        return completedPath.GetChildFiles(flags = PathSearchFlags.RecurseIntoSubdirectories) }
                    getFilesAsync.RunAsTask()
                    |> Seq.iter (addLookupItem iconId)
            | _ -> ()
        | _ -> ()

        true


type FSharpPathLookupItem(path: FileSystemPath, basePath: FileSystemPath, filePath: FileSystemPath, iconId: IconId) =
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


