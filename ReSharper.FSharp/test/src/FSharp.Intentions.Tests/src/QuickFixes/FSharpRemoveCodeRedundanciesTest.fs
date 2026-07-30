namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpRemoveCodeRedundanciesCleanupModuleTest() =
    inherit CodeCleanupTestBase()

    override x.RelativeTestDataPath = "features/service/codeCleanup"

    member x.DoNamedTestWithProfile() =
        x.DoTestFilesWithProfileAndSuffix(x.TestMethodName + ".profile", "Code cleanup.fs")

    [<Test>] member x.``Full cleanup without reformat``() = x.DoNamedTestWithProfile()
    [<Test>] member x.``Remove code redundancies only``() = x.DoNamedTestWithProfile()
    [<Test>] member x.``Simplify lambdas only``() = x.DoNamedTestWithProfile()
