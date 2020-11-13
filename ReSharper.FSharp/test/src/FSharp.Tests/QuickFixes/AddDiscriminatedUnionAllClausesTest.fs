namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type AddDiscriminatedUnionAllClausesAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addDiscriminatedUnionAllClauses"

    [<Test>] member x.``Union literal availability``() = x.DoNamedTest()
    [<Test>] member x.``Non-union variable``() = x.DoNamedTest()

[<FSharpTest; TestPackages("FSharp.Core")>]
type AddDiscriminatedUnionAllClausesTest() =
    inherit QuickFixTestBase<AddDiscriminatedUnionAllClauses>()

    override x.RelativeTestDataPath = "features/quickFixes/addDiscriminatedUnionAllClauses"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple in context 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple single existing case 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Partially complete union match``() = x.DoNamedTest()
