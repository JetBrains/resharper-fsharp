namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
[<TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpCodeFormatterTest() =
    inherit CodeFormatterWithExplicitSettingsTestBase<FSharpLanguage>()

    override x.RelativeTestDataPath = "features/service/codeFormatter"

    override x.DoNamedTest() =
        use cookie = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        base.DoNamedTest()

    [<Test>] member x.``Type decl - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type decl - Enum 02 - Access modifier``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Top binding indent 01 - No indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Top binding indent 02 - Correct indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Top binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Local binding indent 01 - No indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Local binding indent 02 - Correct indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Local binding indent 03 - Big indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Let module decl binding indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test; Explicit("Merge formatter branch")>]
    member x.``Let expr binding indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``Nested module decl name indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Named module decl name indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``Nested module indent 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 02 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Nested module indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``For expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``ForEach expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``While expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Do expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Assert expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Assert expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Lazy expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Comp expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Comp expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Comp expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``Set expr indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 02 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Set expr indent 03 - Big indent``() = x.DoNamedTest()

    [<Test>] member x.``TryWith expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``TryFinally expr indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``IfThenElse expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 02``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse expr indent 03 - Elif``() = x.DoNamedTest()

    [<Test>] member x.``Match expr indent 01 - Expr at new line``() = x.DoNamedTest()
    [<Test>] member x.``Match expr indent 02 - With at new line``() = x.DoNamedTest()

    [<Test>] member x.``MatchClause expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 02 - TryWith``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 03 - TryWith - Clause on the same line``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 04 - Unindented last clause``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 05 - Wrong indent in last clause``() = x.DoNamedTest()
    [<Test>] member x.``MatchClause expr indent 06 - When``() = x.DoNamedTest()

    [<Test>] member x.``Lambda expr indent 01 - Without offset``() = x.DoNamedTest()
    [<Test>] member x.``Lambda expr indent 02 - With offset``() = x.DoNamedTest()

    [<Test>] member x.``PrefixApp expr indent 01``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent 02``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent - Comp expr 01``() = x.DoNamedTest()
    [<Test>] member x.``PrefixApp expr indent - Comp expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Enum declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Union declaration indent 02 - Modifier``() = x.DoNamedTest()
    [<Test>] member x.``Type abbreviation declaration indent 01 - Correct indent``() = x.DoNamedTest()
    [<Test>] member x.``Module abbreviation declaration indent 01 - Correct indent``() = x.DoNamedTest()

    [<Test>] member x.``Match clauses alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 01 - No separators``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 02 - Separators``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Binary expr alignment 02 - Pipe operator``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 02 - Semicolons``() = x.DoNamedTest()
    [<Test>] member x.``Record declaration alignment 03 - Mutable``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Record expr alignment 02 - Copy``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 01``() = x.DoNamedTest()
    [<Test>] member x.``Anon record expr alignment 02 - Copy``() = x.DoNamedTest()

    [<Test>] member x.``Type members 01``() = x.DoNamedTest()
    [<Test>] member x.``Type members 02 - Interface``() = x.DoNamedTest()
