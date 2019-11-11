namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests

[<FSharpTest; AbstractClass>]
type FSharpHighlightingTestBase() =
    inherit HighlightingTestBase()

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _
