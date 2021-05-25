namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceIfByConditionOperandTest() =
    inherit FSharpQuickFixTestBase<ReplaceIfByConditionOperandFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceIfByConditionOperand"

    [<Test>] member x.``Condition operand 01``() = x.DoNamedTest()
    [<Test>] member x.``Condition operand 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Condition operand negation 01``() = x.DoNamedTest()
    [<Test>] member x.``Condition operand negation 02 - Multiline``() = x.DoNamedTest()


[<FSharpTest>]
type  ReplaceIfByConditionOperandAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceIfByConditionOperand"

    [<Test>] member x.``Text - Condition operand``() = x.DoNamedTest()
    [<Test>] member x.``Text - Condition operand negation``() = x.DoNamedTest()
