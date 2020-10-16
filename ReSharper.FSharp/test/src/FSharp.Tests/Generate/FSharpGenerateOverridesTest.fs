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
    [<Test; Explicit>] member x.``Anchor - Type 03``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Anchor - Type 04 - Start``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Empty - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Class 02 - Same line``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Struct 01``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Union 02``() = x.DoNamedTest()
