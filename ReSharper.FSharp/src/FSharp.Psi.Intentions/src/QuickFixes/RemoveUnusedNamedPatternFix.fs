namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree

module RemoveUnusedNamedPatternFix =
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
    
    let fieldPatterns =
        NamedPatternsListPatNavigator.GetByFieldPattern(warning.Pat.IgnoreInnerParens().Parent.As<IFieldPat>()).FieldPatterns
        
    override x.Text = "Remove unused value"

    override x.IsAvailable _ =
        fieldPatterns |> Seq.exists isValid

    override x.ExecutePsiTransaction(_, _) =
        let fieldPat = warning.Pat.IgnoreInnerParens().Parent.As<IFieldPat>()

        use writeLock = WriteLockCookie.Create(fieldPat.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let indexPat = fieldPatterns.IndexOf(fieldPat)
        
        if fieldPatterns.Count = 1 then
            let parent = fieldPatterns.[0].Parent
            match parent with
            | :? IRecordPat
            | :? INamedUnionCaseFieldsPat ->
                RemoveUnusedNamedPatternFix.replaceWithWildPat warning.Pat
            | _ -> ()
                
            null
            
        elif indexPat < fieldPatterns.Count - 1 then
            let nextFieldPat = fieldPatterns.[indexPat + 1]
            
            let rangeToDelete =
                let rangeStart = fieldPat
                let rangeEnd =
                    nextFieldPat.PrevSibling
                TreeRange(rangeStart, rangeEnd)
            ModificationUtil.DeleteChildRange(rangeToDelete)
            null
        else
            let prevFieldPat = fieldPatterns.[indexPat - 1]
            let rangeToDelete =
                let rangeStart = prevFieldPat.NextSibling
                let rangeEnd = fieldPat
                TreeRange(rangeStart, rangeEnd)    
            ModificationUtil.DeleteChildRange(rangeToDelete)
            null