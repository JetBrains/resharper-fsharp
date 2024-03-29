namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type ReplaceWithInterpolatedStringTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithInterpolatedStringFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithInterpolatedString"

    [<Test; ExecuteScopedActionInFile>] member x.``String 01 - Single specifier``() = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``String 02 - Many specifiers``() = x.DoNamedTest()
    [<Test>] member x.``String 03 - Many specifiers with text``() = x.DoNamedTest()
    [<Test>] member x.``String 04 - Escape braces``() = x.DoNamedTest()
    [<Test>] member x.``String 05 - failwithf``() = x.DoNamedTest()
    [<Test>] member x.``String 06 - Default format - Start``() = x.DoNamedTest()
    [<Test>] member x.``String 07 - Default format - Middle``() = x.DoNamedTest()
    [<Test>] member x.``String 08 - Default format - End``() = x.DoNamedTest()
    [<Test>] member x.``String 09 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``String 10 - Escape chars``() = x.DoNamedTest()
    [<Test>] member x.``String 11 - Remove outer parens``() = x.DoNamedTest()
    [<Test>] member x.``String 12 - sprintf piped``() = x.DoNamedTest()
    [<Test>] member x.``String 13 - Format string parens``() = x.DoNamedTest()
    [<Test>] member x.``String 14 - Parentheses required``() = x.DoNamedTest()

    [<Test>] member x.``Triple Quoted String 01 - Many specifiers with text``() = x.DoNamedTest()
    [<Test>] member x.``Triple Quoted String 02 - Applied string literal``() = x.DoNamedTest()
    [<Test>] member x.``Triple Quoted String 03 - Applied byte array``() = x.DoNamedTest()
    [<Test>] member x.``Triple Quoted String 04 - Applied verbatim string``() = x.DoNamedTest()

    [<Test>] member x.``Verbatim String 01 - Many specifiers with text``() = x.DoNamedTest()

    [<Test>] member x.``Tuple - Struct 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Struct 02 - Nested parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Nested parens``() = x.DoNamedTest()
