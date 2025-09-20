namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedNamedPatternFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()
    
    let fieldPat = FieldPatNavigator.GetByPattern(warning.Pat)
    
    let listPat = NamedPatternsListPatNavigator.GetByFieldPattern(fieldPat)

    /// Remove the middle field and keep separators/comments stable.
    let deleteMiddleFieldPreservingTrailingComment (prevFieldPat: IFieldPat) (fieldPat: IFieldPat) (nextFieldPat: IFieldPat) =
        let fieldPatNode = fieldPat.Pattern
        // preserve an inline comment on the same line as the field being deleted
        let trailingComment = findSubsequentComment fieldPatNode
        // Decide rangeStart so that we preserve the preceding semicolon when prev/field/next are on the same line.
        // - If prev and field are on the same line AND next is also on the same line, start at the field itself
        //   to keep the semicolon after prev (so it becomes the separator between prev and next).
        // - If prev and field are on the same line BUT next starts on the next line, start from the token after prev
        //   to remove the trailing semicolon after prev. (We don't want a dangling semicolon at the end of the line.)
        // - Otherwise, start at the field itself to avoid consuming the preceding newline.
        let prevSameLine = isNotNull prevFieldPat && prevFieldPat.EndLine = fieldPat.EndLine
        let nextSameLine = nextFieldPat.StartLine = fieldPat.EndLine
        let erasePrevSemicolon = prevSameLine && not nextSameLine && shouldEraseSemicolon prevFieldPat

        let rangeStart =
            if erasePrevSemicolon then prevFieldPat.NextSibling else fieldPat
        // If there is an inline comment on the same line, stop before it; otherwise stop before the newline
        // preceding the next field to keep the next line and its indentation intact.
        let rangeEnd =
            if not (isNull trailingComment) then trailingComment.PrevSibling
            else
                if fieldPat.EndLine < nextFieldPat.StartLine then
                    let beforeNext = getFirstMatchingNodeBefore isInlineSpaceOrComment nextFieldPat
                    let maybeNewLine = getThisOrPrevNewLine beforeNext
                    // If we are removing the first field and the next field starts on a new line,
                    // also remove the newline and its indentation so that the opening '{' stays
                    // on the same line as the first remaining field (if any).
                    if isNull prevFieldPat && isNotNull maybeNewLine && isNewLine maybeNewLine then
                        beforeNext
                    else
                        if isNotNull maybeNewLine && isNewLine maybeNewLine then maybeNewLine.PrevSibling else nextFieldPat.PrevSibling
                else
                    nextFieldPat.PrevSibling

        deleteChildRange rangeStart rangeEnd

    /// Remove the last field; if the previous field has an inline end-of-line comment, keep it attached to the previous field.
    let liftPrevInlineCommentThenRemoveLast (prevFieldPat: IFieldPat) (lastFieldPat: IFieldPat) =
        let prevFieldNode = prevFieldPat.Pattern
        // Finds an inline // comment on the same line as the given node (if any).
        let inlineCommentOnSameLine node =
            let comment = findSubsequentComment node
            if isNotNull comment && comment.EndLine = node.EndLine then comment else null

        let movedByBlock =
            let inlineComment = inlineCommentOnSameLine prevFieldNode
            if isNull inlineComment && isLastMeaningfulNodeOnLine prevFieldPat then
                let anchor = getThisOrPrevNewLine prevFieldPat
                if isNotNull anchor then
                    moveCommentsAndWhitespaceAfterAnchor anchor prevFieldNode
                    true
                else false
            else false
        
        /// Attempts to move the node to a new line using resilient line ending/indent fallbacks.
        let tryMoveToNewLine (node: ITreeNode) =
            let lineEnding =
                try
                    node.GetLineEnding()
                with _ ->
                    "\n"
            let indent =
                try node.Indent with _ ->
                    match node.PrevSibling with
                    | :? Whitespace as ws -> ws.GetTextLength()
                    | _ -> 0
            try
                moveToNewLine lineEnding indent node
            with _ -> ()

        if not movedByBlock then
            let inlineCommentPrev = inlineCommentOnSameLine prevFieldNode
            if isNotNull inlineCommentPrev then
                addNodeBefore prevFieldPat inlineCommentPrev
                tryMoveToNewLine prevFieldPat

        let lastWithTrivia =
            lastFieldPat
            |> skipSemicolonsAndWhiteSpacesAfter
            |> getThisOrNextNewLine // keep the next line intact if any

        deleteChildRange prevFieldPat.NextSibling lastWithTrivia

    override x.Text = "Remove unused named pattern"

    override x.IsAvailable _ =
       isNotNull fieldPat && isNotNull listPat && listPat.FieldPatterns.Count > 1

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(fieldPat.IsPhysical())

        let fieldPatterns = listPat.FieldPatterns
        let indexPat = fieldPatterns.IndexOf(fieldPat) // index of the unused field
        let isLast = indexPat = fieldPatterns.Count - 1 // is the unused field the last one?

        if not isLast then
            let prevPat = if indexPat > 0 then fieldPatterns[indexPat - 1] else null
            deleteMiddleFieldPreservingTrailingComment prevPat fieldPat fieldPatterns[indexPat + 1]
        else
            liftPrevInlineCommentThenRemoveLast fieldPatterns[indexPat - 1] fieldPat

