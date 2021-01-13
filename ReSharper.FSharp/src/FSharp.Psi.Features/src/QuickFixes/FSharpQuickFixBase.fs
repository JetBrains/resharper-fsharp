namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Psi

[<AbstractClass>]
type FSharpQuickFixBase() =
    inherit QuickFixBase()

    abstract ExecutePsiTransaction: solution: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        x.ExecutePsiTransaction(solution)
        null

[<AbstractClass>]
type FSharpScopedQuickFixBase() =
    inherit ScopedQuickFixBase()

    abstract ExecutePsiTransaction: ISolution -> unit
    default x.ExecutePsiTransaction _ = ()

    override x.ExecutePsiTransaction(solution, _) =
        use formatterCookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        x.ExecutePsiTransaction(solution)
        null


type IFSharpQuickFixUtilComponent =
    inherit IQuickFixUtilComponent

    abstract BindTo: FSharpSymbolReference * ITypeElement -> FSharpSymbolReference
