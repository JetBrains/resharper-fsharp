namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypeProviders

open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("../../../assemblies/TestTypeProviders/net45/TestTypeProviders.dll")>]
type ProvidedPropertiesResolveTest() =
    inherit TypeProvidersHighlightingTestBase()
    
    override x.RelativeTestDataPath = "features/typeProviders/providedProperties"

    [<Test>] member x.``Readonly string property``() = x.DoNamedTest()
    [<Test>] member x.``Readonly internal type property``() = x.DoNamedTest()
    [<Test>] member x.``Static readonly string property``() = x.DoNamedTest()
    [<Test>] member x.``Try set readonly property``() = x.DoNamedTest()
    [<Test>] member x.``String property with setter``() = x.DoNamedTest()
    [<Test>] member x.``String property with indexer``() = x.DoNamedTest()
    [<Test>] member x.``Set mutable property``() = x.DoNamedTest()