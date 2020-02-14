namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Intentions.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type ImportTypeTest() =
    inherit QuickFixTestBase<ImportTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/importType"

    [<Test>] member x.``Type 01``() = x.DoNamedTest()

    [<Test>] member x.``Type extension 01``() = x.DoNamedTest()
