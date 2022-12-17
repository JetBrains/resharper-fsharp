namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open System
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree

module ReplaceWithWildPatX =
    let replaceWithWildPat (pat: IFSharpPattern) =
        if isIdentifierOrKeyword (pat.GetPreviousToken()) then
            ModificationUtil.AddChildBefore(pat, Whitespace()) |> ignore

        if isIdentifierOrKeyword (pat.GetNextToken()) then
            ModificationUtil.AddChildAfter(pat, Whitespace()) |> ignore

        for pat in pat.GetPartialDeclarations() do
            let sourceFile = pat.GetSourceFile()
            let psiModule = pat.GetPsiModule()
            replace pat (pat.GetFSharpLanguageService().CreateElementFactory(sourceFile, psiModule).CreateWildPat())


type RemoveUnusedNamedPatternFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()
    
    let fieldAndRecord =
        match warning.Pat.IgnoreInnerParens().Parent with
        | :? IFieldPat as fieldPat ->
            match fieldPat.Parent with
            | :? IRecordPat as recordPat ->
                Some(fieldPat, recordPat)
            | _ -> None
        | _ -> None
        
    override x.Text = "Remove unused value"

    override x.IsAvailable _ =
        match fieldAndRecord with
        | None -> false
        | Some(fieldPat, recordPat) ->
            isValid fieldPat && isValid recordPat

    override x.ExecutePsiTransaction(_, _) =
        match fieldAndRecord with
        | None -> null
        | Some(fieldPat, recordPat) ->
        use writeLock = WriteLockCookie.Create(fieldPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let indexPat = recordPat.FieldPatterns.IndexOf(fieldPat)
        if recordPat.FieldPatterns.Count = 1 then
            let factory = recordPat.CreateElementFactory()
            ModificationUtil.ReplaceChild(recordPat, factory.CreateSelfId("_")) |> ignore
            let offset = recordPat.GetTreeEndOffset().Offset  
            Action<_>(fun textControl -> textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
            
        elif indexPat < recordPat.FieldPatterns.Count - 1 then
            let nextFieldPat = recordPat.FieldPatterns.[indexPat + 1]
            
            let rangeToDelete =
                let rangeStart = fieldPat
                let rangeEnd =
                    nextFieldPat.PrevSibling
                TreeRange(rangeStart, rangeEnd)
            
            ModificationUtil.DeleteChildRange(rangeToDelete)
            
            let offset =nextFieldPat.GetNavigationRange().StartOffsetRange().StartOffset
            Action<_>(fun textControl ->
                textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
        else
            let prevFieldPat = recordPat.FieldPatterns.[indexPat - 1]
            let rangeToDelete =
                let rangeStart = prevFieldPat.NextSibling
                let rangeEnd = fieldPat
                TreeRange(rangeStart, rangeEnd)
                
            ModificationUtil.DeleteChildRange(rangeToDelete)
            let offset = prevFieldPat.GetNavigationRange().EndOffsetRange().EndOffset

            Action<_>(fun textControl ->
                textControl.Caret.MoveTo(offset, CaretVisualPlacement.DontScrollIfVisible))
