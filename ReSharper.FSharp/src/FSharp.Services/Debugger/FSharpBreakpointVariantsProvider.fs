namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open System
open System.Collections.Generic
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Model
open JetBrains.Util
open JetBrains.Util.dataStructures.TypedIntrinsics

[<Language(typeof<FSharpLanguage>)>]
type FSharpBreakpointVariantsProvider() =
    let [<Literal>] multilineSuffix = " ..."

    interface IBreakpointVariantsProvider with
        member x.GetSupportedFileExtensions() = List([|"fs"|])

        member x.GetBreakpointVariants(file, line, solution) =
            match file.GetPrimaryPsiFile() with
            | :? IFSharpFile as fsFile ->
                match fsFile.ParseResults with
                | Some parseResults ->
                    let document = file.ToSourceFile().Document
                    let docLine = docLine line
                    let lineStart = document.GetLineStartOffset(docLine)
                    let lineEnd = document.GetLineEndOffsetWithLineBreak(docLine)
                    let variants: BreakpointVariantModelBase seq = 
                        fsFile.FindTokensAt(TreeTextRange(TreeOffset(lineStart), TreeOffset(lineEnd)))
                        |> Seq.choose (fun token ->
                            let pos = document.GetPos(token.GetTreeEndOffset().Offset)
                            match parseResults.ValidateBreakpointLocation(pos) with
                            | Some range when range.StartLine - 1 = line ->
                                let startOffset = document.GetTreeStartOffset(range).Offset
                                let endOffset = document.GetTreeEndOffset(range).Offset
                                let breakpointText = document.GetText(TextRange(startOffset, Math.Min(lineEnd, endOffset)))
                                let text = if endOffset > lineEnd then breakpointText + multilineSuffix else breakpointText
                                Some (BreakpointVariantModel(startOffset, endOffset, text) :> _)
                            | _ -> None)
                    JetHashSet(variants).AsList()
                | _ -> null
            | _ -> null
