namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.QuickFixes

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit

    override x.ExecutePsiTransaction(solution, _) =
        x.ExecutePsiTransaction(solution)
        null
