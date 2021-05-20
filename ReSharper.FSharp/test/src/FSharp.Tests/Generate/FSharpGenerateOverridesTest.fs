namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Generate

open JetBrains.ReSharper.FeaturesTestFramework.Generate
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpGenerateOverridesTest() =
    inherit GenerateTestBase()

    override x.RelativeTestDataPath = "features/generate/overrides"

    [<Test>] member x.``Anchor - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Repr 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Repr 02``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Repr 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Repr - Member 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Repr - Member 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 04 - Start``() = x.DoNamedTest()

    [<Test>] member x.``Anchor - Union Case 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Union Case 02 - Modifier``() = x.DoNamedTest()

    [<Test>] member x.``Member - Event - Cli 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 01``() = x.DoNamedTest()

    [<Test>] member x.``Input elements - Overriden 01``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Empty - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Class 02 - Same line``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Class 03 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Struct 01``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Union 02``() = x.DoNamedTest()

    [<Test>] member x.``Super - Substitution 01``() = x.DoNamedTest()
    [<Test>] member x.``Super - Substitution 02``() = x.DoNamedTest()
    [<Test>] member x.``Super - Substitution 03 - Abbreviations``() = x.DoNamedTest()
    [<Test>] member x.``Super - Substitution 04 - Type parameter``() = x.DoNamedTest()

    [<Test>] member x.``Super 01``() = x.DoNamedTest()
    [<Test>] member x.``Super 02``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Static class 01``() = x.DoNamedTest()

    [<FSharpSignatureTest>]
    [<Test>] member x.``Not available - Signature 01``() = x.DoNamedTest()
