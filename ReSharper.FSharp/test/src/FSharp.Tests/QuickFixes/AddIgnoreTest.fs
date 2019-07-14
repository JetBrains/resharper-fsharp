namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type AddIgnoreTest() =
    inherit QuickFixTestBase<AddIgnoreFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addIgnore"

    [<Test>] member x.``Module 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Expression 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expression 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Expression 03 - Multiline``() = x.DoNamedTest()