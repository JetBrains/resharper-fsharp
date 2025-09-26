namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedNamedPatternFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()
    
    let fieldPat = FieldPatNavigator.GetByPattern(warning.Pat)
    let listPat = NamedPatternsListPatNavigator.GetByFieldPattern(fieldPat)

    override x.Text = $"Remove unused pattern"

    override x.IsAvailable _ = isNotNull listPat

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(fieldPat.IsPhysical())

        let fieldPatterns = listPat.FieldPatterns

        // If there is only a single named field, and it is unused, collapse the whole pattern:
        // - For records: replace the record pattern with '_'
        // - For unions with named fields: replace the field list with a single '_'
        let collapseSingleNamedField (lp: INamedPatternsListPat) =
            let patToReplace: IFSharpTreeNode =
                match lp with
                | :? IRecordPat
                | :? INamedUnionCaseFieldsPat as pat -> pat
                | _ -> failwith "Unexpected named pattern list"
            
            let factory = patToReplace.CreateElementFactory()
            replace patToReplace (factory.CreateWildPat())
            
        if fieldPatterns.Count = 1 then
            collapseSingleNamedField listPat
        else
            // Prefer removing a preceding semicolon (…; field) to avoid leaving a dangling separator, scanning back over whitespace and comments.
            let prevNonWsOrComment = skipMatchingNodesBefore isWhitespaceOrComment fieldPat
            if isNotNull prevNonWsOrComment && prevNonWsOrComment != fieldPat && isSemicolon prevNonWsOrComment then
                deleteChildRange prevNonWsOrComment fieldPat
            else
                // Otherwise, if the next sibling skipping whitespace or comments is a semicolon (field ; …), remove it too.
                let nextNonWsOrComment = skipMatchingNodesAfter isWhitespaceOrComment fieldPat
                if isNotNull nextNonWsOrComment && isSemicolon nextNonWsOrComment then
                    deleteChildRange fieldPat nextNonWsOrComment
                else
                    // No adjacent separator: just delete the field and let the formatter handle whitespace/newlines.
                    deleteChild fieldPat


