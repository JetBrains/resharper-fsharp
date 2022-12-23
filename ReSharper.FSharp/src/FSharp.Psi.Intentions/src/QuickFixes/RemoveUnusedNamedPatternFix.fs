namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

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
            let factory = fieldPatterns.[0].CreateElementFactory()
            let wildPat = factory.CreateWildPat()
            for pat in fieldPatterns do
                match pat.Parent with
                | :? IRecordPat as recordPat ->
                    ModificationUtil.ReplaceChild(recordPat, wildPat) |> ignore
                | :? INamedUnionCaseFieldsPat as unionPat ->
                    ModificationUtil.ReplaceChild(unionPat.IgnoreParentChameleonExpr(), wildPat) |> ignore
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