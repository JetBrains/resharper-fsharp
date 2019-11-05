namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Feature.Services.Tests.FeatureServices.SelectEmbracingConstruct
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type FSharpExtendSelectionTest() =
    inherit SelectEmbracingConstructTestBase()

    override x.RelativeTestDataPath = "features/service/extendSelection"

    [<Test>] member x.``Module qualifier 01 - Name``() = x.DoNamedTest()
    [<Test>] member x.``Module qualifier 02 - Qualifier``() = x.DoNamedTest()
    [<Test>] member x.``Module qualifier 03 - Multiple qualifiers``() = x.DoNamedTest()

    [<Test>] member x.``Match clause - When 01 - Pat``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - When 02 - When``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - When 03 - Expr``() = x.DoNamedTest()

    [<Test>] member x.``Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Keyword 01``() = x.DoNamedTest()
