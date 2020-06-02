namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypeProviders

open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("../../../assemblies/TestTypeProviders/net45/TestTypeProviders.dll")>]
type ProvidedEventsResolveTest() =
    inherit TypeProvidersHighlightingTestBase()
    
    override x.RelativeTestDataPath = "features/typeProviders/providedEvents"

    [<Test>] member x.``Simple event``() = x.DoNamedTest()
    [<Test>] member x.``Simple static event``() = x.DoNamedTest()