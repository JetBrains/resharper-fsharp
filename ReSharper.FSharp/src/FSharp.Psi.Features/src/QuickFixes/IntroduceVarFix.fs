namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type IntroduceVarFix(expr: ISynExpr) =
    inherit QuickFixBase()

    new (warning: UnitTypeExpectedWarning) =
        IntroduceVarFix(warning.Expr)

    new (warning: FunctionValueUnexpectedWarning) =
        IntroduceVarFix(warning.Expr)

    override x.Text = "Introduce 'let' binding"

    override x.IsAvailable _ =
        FSharpIntroduceVariable.CanIntroduceVar(expr)

    override x.ExecutePsiTransaction(_, _) =
        Action<_>(fun textControl ->
            FSharpIntroduceVariable.IntroduceVar(expr, textControl))
