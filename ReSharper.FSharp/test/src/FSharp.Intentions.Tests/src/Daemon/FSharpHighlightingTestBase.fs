namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests

[<FSharpTest; AbstractClass>]
type FSharpHighlightingTestBase() =
    inherit HighlightingTestBase()

    override x.HighlightingPredicate(highlighting, sourceFile, settingsStore) =
        base.HighlightingPredicate(highlighting, sourceFile, settingsStore) ||
        match highlighting with
        | :? ICustomSeverityHighlighting as h -> h.Severity = Severity.INFO
        | _ -> false

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _
