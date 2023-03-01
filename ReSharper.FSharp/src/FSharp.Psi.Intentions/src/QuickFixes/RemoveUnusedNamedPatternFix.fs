namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
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
            let parent = fieldPatterns.[0].Parent
            match parent with
            | :? IRecordPat
            | :? INamedUnionCaseFieldsPat ->
                let sourceFile = fieldPat.Pattern.GetSourceFile()
                let psiModule = fieldPat.Pattern.GetPsiModule()
                let wildPat = fieldPat.Pattern.GetFSharpLanguageService().CreateElementFactory(sourceFile, psiModule).CreateWildPat()
                replace fieldPat.Pattern wildPat
            | _ -> ()
                
            null
        elif indexPat < fieldPatterns.Count - 1 then
            let nextFieldPat = fieldPatterns.[indexPat + 1]
            let fieldPatNode = fieldPat.Pattern
            let isLastFieldPatOnLine = fieldPatNode.EndLine <> nextFieldPat.EndLine
           
            let rangeStart =
                if isLastFieldPatOnLine then
                    getFirstMatchingNodeBefore isInlineSpace fieldPat |> getThisOrPrevNewLine
                else
                    fieldPat
                    
            let rangeEnd = nextFieldPat.PrevSibling
            deleteChildRange rangeStart rangeEnd
            null
        else
            let prevFieldPat = fieldPatterns.[indexPat - 1]
            let rangeStart = prevFieldPat.NextSibling
            let rangeEnd = fieldPat 
            deleteChildRange rangeStart rangeEnd
            null