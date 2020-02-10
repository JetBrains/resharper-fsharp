namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type FSharpCodeFormatterTest() =
    inherit CodeFormatterWithExplicitSettingsTestBase<FSharpLanguage>()
    
    override x.RelativeTestDataPath = "features/service/codeFormatter"

    [<Test>] member x.``Match expr alignment 01 - Correct alignment``() = x.DoNamedTest()
    [<Test>] member x.``Match expr alignment 02 - Clause before match``() = x.DoNamedTest()
    [<Test>] member x.``Match expr alignment 03 - Clause after match``() = x.DoNamedTest()
    [<Test>] member x.``Match expr alignment 04 - Multiple clauses``() = x.DoNamedTest()
    [<Test>] member x.``Match expr alignment 05 - New line``() = x.DoNamedTest()
    
    [<Test>] member x.``Match lambda expr alignment 01 - Correct alignment``() = x.DoNamedTest()
    [<Test>] member x.``Match lambda expr alignment 02 - Clause before function``() = x.DoNamedTest()

    [<Test>] member x.``Sequential expr alignment 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Sequential expr alignment 02 - Multiline array init``() = x.DoNamedTest()
    
    [<Test>] member x.``Nested module decl indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Nested module decl indent 02 - With Let member``() = x.DoNamedTest()
    [<Test>] member x.``Nested module decl indent 03 - Multiple members``() = x.DoNamedTest()
    
    [<Test>] member x.``Top level binding indent 01 - Single member``() = x.DoNamedTest()
    [<Test>] member x.``Top level binding indent 02 - Multiple members``() = x.DoNamedTest()
    
    [<Test>] member x.``Local binding indent 01 - Single member``() = x.DoNamedTest()
    [<Test>] member x.``Local binding indent 02 - Multiple members``() = x.DoNamedTest()
    
    [<Test>] member x.``For expr indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``For expr indent 02 - Multiline Do expr``() = x.DoNamedTest()
    
    [<Test>] member x.``ForEach expr indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``ForEach expr indent 02 - Multiline Do expr``() = x.DoNamedTest()
    
    [<Test>] member x.``While expr indent 01 - Simple Do expr``() = x.DoNamedTest()
    [<Test>] member x.``While expr indent 02 - Multiline Do expr``() = x.DoNamedTest()
    
    [<Test>] member x.``Try expr indent - TryWith 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Try expr indent - TryWith 02 - Multiline Try expr``() = x.DoNamedTest()
    [<Test>] member x.``Try expr indent - TryFinally 01 - Multiline Try expr``() = x.DoNamedTest()
    
    [<Test>] member x.``Do expr indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Do expr indent 02 - Multiline``() = x.DoNamedTest()
    
    [<Test>] member x.``Comp expr indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Comp expr indent 02 - Multiline``() = x.DoNamedTest()
    
    [<Test>] member x.``Lazy expr indent 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Lazy expr indent 02 - Multiline``() = x.DoNamedTest()
