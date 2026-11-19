namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.Application.Components
open JetBrains.ReSharper.Features.ReSpeller.ReSharperSpecific.QuickFixes
open JetBrains.ReSharper.FeaturesTestFramework.SpellEngineStub
open NUnit.Framework

type TypoQuickFixTest() =
    inherit FSharpQuickFixTestBase<TypoQuickFix>()

    override x.RelativeTestDataPath = "features/quickFixes/typo"

    override x.TestFixtureSetUp() =
        base.TestFixtureSetUp()

        let testSpellService = x.ShellInstance.GetComponent<TestSpellService>()
        testSpellService.AddWrongWord(x.TestFixtureLifetime, "wrongword", "rightword")

    [<Test>] member x.``String 01 - Between escapes``() = x.DoNamedTest()
