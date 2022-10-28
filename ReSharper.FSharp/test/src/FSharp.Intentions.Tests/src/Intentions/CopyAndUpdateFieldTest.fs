namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions
open NUnit.Framework

type CopyAndUpdateFieldActionTest() =
    inherit FSharpContextActionExecuteTestBase<CopyAndUpdateFieldAction>()

    override x.ExtraPath = "copyAndUpdateField"

    [<Test>] member x.``Test 01``() = x.DoNamedTest()
