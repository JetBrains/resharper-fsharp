namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.QuickFixes
open JetBrains.ReSharper.Feature.Services.QuickFixes

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: solution: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null

[<AbstractClass>]
type FSharpScopedQuickFixBase() =
    inherit ScopedQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null
