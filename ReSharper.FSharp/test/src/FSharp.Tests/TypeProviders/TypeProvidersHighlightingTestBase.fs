namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypeProviders

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests

[<FSharpTest; AbstractClass>]
type TypeProvidersHighlightingTestBase() =
    inherit HighlightingTestBase()

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _

    override x.HighlightingPredicate(_,_,_) = true
