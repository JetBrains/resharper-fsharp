namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypeProviders

open JetBrains.ReSharper.TestFramework
open NUnit.Framework

//TODO: tests for default raw value
[<TestReferences("../../../assemblies/TestTypeProviders/net45/TestTypeProviders.dll")>]
type ProvidedFieldsResolveTest() =
    inherit TypeProvidersHighlightingTestBase()

    override x.RelativeTestDataPath = "features/typeProviders/providedFields"

    [<Test>] member x.``String literal field``() = x.DoNamedTest()
