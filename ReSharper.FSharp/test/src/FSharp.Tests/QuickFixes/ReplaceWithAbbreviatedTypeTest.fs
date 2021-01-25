namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages(FSharpCorePackage)>]
type ReplaceWithAbbreviatedTypeTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithAbbreviatedTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAbbreviatedType"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Type parameters 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Type parameters 02``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters 03``() = x.DoNamedTest()
