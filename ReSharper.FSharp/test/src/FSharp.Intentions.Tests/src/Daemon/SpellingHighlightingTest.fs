namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.Application.Components
open JetBrains.ReSharper.Features.ReSpeller.ReSharperSpecific.Highlightings
open JetBrains.ReSharper.FeaturesTestFramework.SpellEngineStub
open NUnit.Framework

type SpellingHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/spelling"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? ReSpellerHighlightingBase

    override x.TestFixtureSetUp() =
        base.TestFixtureSetUp()

        let testSpellService = x.ShellInstance.GetComponent<TestSpellService>()
        testSpellService.ConfigureDefault(x.TestFixtureLifetime)

    [<Test>] member x.``Strings 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings 02 - Concatenation``() = x.DoNamedTest()
