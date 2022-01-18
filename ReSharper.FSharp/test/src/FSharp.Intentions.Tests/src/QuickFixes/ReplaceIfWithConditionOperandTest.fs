namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithConditionTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithConditionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithCondition"

    [<Test>] member x.``Condition 01``() = x.DoNamedTest()
    [<Test>] member x.``Condition 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Condition negation 01``() = x.DoNamedTest()
    [<Test>] member x.``Condition negation 02 - Multiline``() = x.DoNamedTest()


[<FSharpTest>]
type  ReplaceWithConditionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithCondition"

    [<Test>] member x.``Text - Condition``() = x.DoNamedTest()
    [<Test>] member x.``Text - Condition negation``() = x.DoNamedTest()
