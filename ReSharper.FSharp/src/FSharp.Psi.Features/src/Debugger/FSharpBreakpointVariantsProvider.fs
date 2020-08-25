namespace JetBrains.ReSharper.Plugins.FSharp.Services.Debugger

open System
open System.Collections.Generic
open FSharp.Compiler
open JetBrains.ReSharper.Feature.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpBreakpointVariantsProvider() =

    let [<Literal>] multilineSuffix = " ..."

    interface IBreakpointVariantsProvider with
        member x.GetSupportedFileExtensions() =
            // It's OK to create the list each time, it's called once per solution lifetime.
            List([| FSharpProjectFileType.FsExtension
                    FSharpProjectFileType.MlExtension
                    FSharpScriptProjectFileType.FsxExtension
                    FSharpScriptProjectFileType.FsScriptExtension |])

        member x.GetBreakpointVariants(file, line, _) =
            match file.GetPrimaryPsiFile().AsFSharpFile() with
            | null -> null
            | fsFile ->

            match fsFile.ParseResults with
            | None -> null
            | Some parseResults ->

            let document = file.ToSourceFile().Document
            let lineStart = document.GetLineStartOffset(docLine line)
            let lineEnd = document.GetLineEndOffsetWithLineBreak(docLine line)

            let variants = Dictionary<Range.range, IBreakpoint>() 
            for token in fsFile.FindTokensAt(TreeTextRange(TreeOffset(lineStart), TreeOffset(lineEnd))) do
                let documentEndOffset = token.GetDocumentEndOffset()
                let pos = getPosFromDocumentOffset documentEndOffset
                match parseResults.ValidateBreakpointLocation(pos) with
                | Some range when range.StartLine - 1 = line ->
                    if variants.ContainsKey(range) then () else

                    let startOffset = getStartOffset document range
                    let endOffset = getEndOffset document range

                    let text =
                        let breakpointText = document.GetText(TextRange(startOffset, Math.Min(lineEnd, endOffset)))
                        if endOffset > lineEnd then breakpointText + multilineSuffix else breakpointText

                    // Multi-method breakpoints allow us to set up multiple breakpoints across multiple methods if they
                    // all point to the same place in the source code. This is the case e.g. for async CE (since there
                    // may be more than one function generated for a particular CE call).
                    variants.[range] <- TextRangeBreakpoint(TextRange(startOffset, endOffset),
                                                            text,
                                                            containingFunctionName = null,
                                                            isMultiMethodBreakpoint = true)
                | _ -> ()

            variants.Values.AsList()
