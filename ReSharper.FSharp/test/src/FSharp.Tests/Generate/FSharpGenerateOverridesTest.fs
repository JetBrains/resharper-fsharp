namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Generate

open JetBrains.ReSharper.FeaturesTestFramework.Generate
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpGenerateOverridesTest() =
    inherit GenerateTestBase()

    override x.RelativeTestDataPath = "features/generate/overrides"

    [<Test>] member x.``Anchor - Ctor - Primary 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Inherit 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Inherit 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Let 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Let 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Let 04``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 04``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 05``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 06``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Member 07``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - No repr 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - No repr 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr 04``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 04``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 05``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 06``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 07``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Repr - Member 08``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Type 04 - Start``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 02``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 03``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 04``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 05``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 06``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 07``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 08``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 09``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 10``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 11``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 12``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 13``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 14``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Whitespace 15``() = x.DoNamedTest()

    [<Test>] member x.``Anchor - Union Case 01``() = x.DoNamedTest()
    [<Test>] member x.``Anchor - Union Case 02 - Modifier``() = x.DoNamedTest()

    [<Test>] member x.``Member - Event - Cli 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 02 - Setter``() = x.DoNamedTest()
    [<Test>] member x.``Member - Property 03 - Setter only``() = x.DoNamedTest()

    [<Test>] member x.``Input elements - Overriden 01``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Empty - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Class 02 - Same line``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Class 03 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Empty - Struct 01``() = x.DoNamedTest()

    [<Test>] member x.``Repr - Union - No bar - Multiple 01``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Union - No bar - Multiple 02``() = x.DoNamedTest()
    [<Test>] member x.``Repr - Union - No bar - Single 01``() = x.DoNamedTest()
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
