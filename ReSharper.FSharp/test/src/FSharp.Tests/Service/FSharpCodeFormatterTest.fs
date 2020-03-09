namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpCodeFormatterTest() =
    inherit CodeFormatterWithExplicitSettingsTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "features/service/codeFormatter"

    override x.DoNamedTest() =
        use cookie = FSharpRegistryUtil.AllowFormatterCookie.Create()
        base.DoNamedTest()

    [<Test>] member x.``Top binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Top binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Local binding indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Nested module indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 03 - Big indent``() = x.DoNamedTest()
