namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl

type IntroduceVarFix(expr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    new (warning: UnitTypeExpectedWarning) =
        IntroduceVarFix(warning.Expr)

    new (warning: FunctionValueUnexpectedWarning) =
        IntroduceVarFix(warning.Expr)

    new (error: UnitTypeExpectedError) =
        IntroduceVarFix(error.Expr)

    override x.Text = "Introduce 'let' binding"

    override x.IsAvailable _ =
        FSharpIntroduceVariable.CanIntroduceVar(expr)

    override x.Execute(solution, textControl) =
        base.Execute(solution, textControl)

        textControl.Selection.SetRange(expr.GetDocumentRange().TextRange)
        FSharpIntroduceVariable.IntroduceVar(expr, textControl, true)
