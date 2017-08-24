namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open Fantomas
open Fantomas.FormatConfig
open JetBrains.Application.Infra
open JetBrains.DocumentModel
open JetBrains.DocumentModel.Impl
open JetBrains.ReSharper.Feature.Services.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CodeStyle
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open Microsoft.FSharp.Compiler

[<CodeCleanupModule>]
type ReformatCode() =
    interface ICodeCleanupModule with
        member x.LanguageType = FSharpLanguage.Instance :> _
        member x.Descriptors = EmptyList<_>.Instance :> _
        member x.IsAvailableOnSelection = false
        member x.SetDefaultSetting(_,_) = ()
        member x.IsAvailable(file) = file.PrimaryPsiLanguage :? FSharpLanguage

        member x.Process(sourceFile,rangeMarker,_,_) =
            match sourceFile.GetTheOnlyPsiFile() with
            | :? IFSharpFile as fsFile ->
                match fsFile.ParseResults with // todo: completion on enter after with
                | Some parseResults when parseResults.ParseTree.IsSome ->
                    let parsedInput = parseResults.ParseTree.Value
                    let filePath = sourceFile.GetLocation().FullPath
                    let document = sourceFile.Document :?> DocumentBase
                    let source = document.GetText()

                    let settings = sourceFile.GetFormatterSettings(fsFile.Language) :?> FSharpFormatSettingsKey
                    let formatConfig = { FormatConfig.Default with
                                             PageWidth = settings.WRAP_LIMIT
                                             IndentSpaceNum = settings.INDENT_SIZE
                                             ReorderOpenDeclaration = settings.ReorderOpenDeclarations
                                             SpaceBeforeColon = settings.SpaceBeforeColon
                                             SpaceAfterComma = settings.SpaceAfterComma
                                             SpaceAfterSemicolon = settings.SpaceAfterSemicolon
                                             IndentOnTryWith = settings.IndentOnTryWith
                                             SpaceAroundDelimiter = settings.SpaceAroundDelimiter }
    
                    let stamp = document.LastModificationStamp
                    let modificationSide = TextModificationSide.NotSpecified
    
                    let change = 
                        if isNotNull rangeMarker then None
//                            let getRange (range: DocumentRange) =
//                                let startCoords = document.GetCoordsByOffset(range.StartOffset.Offset)
//                                let mutable endCoords = document.GetCoordsByOffset(range.EndOffset.Offset)
//                                // todo: rewrite extend selection and remove this step
//                                if endCoords.Column = Column.O && endCoords.Line > Line.O then
//                                    let prevLineEndOffset = document.GetLineEndOffsetNoLineBreak(endCoords.Line - Line.I)
//                                    endCoords <- document.GetCoordsByOffset(prevLineEndOffset)
//                                Range.mkRange "" (startCoords.GetPos()) (endCoords.GetPos())
//    
//                            try
//                                let range = getRange rangeMarker.DocumentRange
//                                let formatted = CodeFormatter.FormatSelection(filePath, range, source, formatConfig)
//                                let startOffset = rangeMarker.DocumentRange.StartOffset.Offset
//                                let oldLength = rangeMarker.Range.Length
//                                Some(DocumentChange(document, startOffset, oldLength, formatted, stamp, modificationSide))
//                            with _ -> None
                        else
                            let formatted = CodeFormatter.FormatAST(parsedInput, filePath, Some source, formatConfig)
                            Some(DocumentChange(document, 0, source.Length, formatted, stamp, modificationSide))
    
                    match change with
                    | Some change -> 
                        document.ChangeDocument(change, TimeStamp.NextValue)
                        sourceFile.GetPsiServices().Files.CommitAllDocuments()
                    | _ -> ()
                | _ -> ()
            | _ -> ()
