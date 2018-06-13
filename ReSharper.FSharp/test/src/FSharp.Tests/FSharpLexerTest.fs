namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Lexing

open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestFileExtension(FSharpProjectFileType.FsExtension)>]
type FSharpLexerTest() =
    inherit LexerTestBase()

    override x.RelativeTestDataPath = "lexing"

    [<Test>] member x.``Empty file 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty file 02 - New lines``() = x.DoNamedTest()

    [<Test>] member x.``Literals 01 - simple numbers``() = x.DoNamedTest()
    [<Test>] member x.``Literals 02 - numbers with suffices``() = x.DoNamedTest()

    [<Test>] member x.``Strings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Strings 02 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Strings 03 - Verbatim``() = x.DoNamedTest()
    [<Test; Ignore>] member x.``Strings 04 - Bytearray``() = x.DoNamedTest()

    [<Test>] member x.``Multiline strings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 02 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 03 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 04 - Empty line 1``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 04 - Empty line 2``() = x.DoNamedTest()

    [<Test>] member x.``Unfinished strings 01``() = x.DoNamedTest()
    [<Test>] member x.``Unfinished strings 02``() = x.DoNamedTest()

    [<Test>] member x.``Comments 01 - End of line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 02 - Multiple end of line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 03 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Comments 04 - Multiline with empty line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 05 - String inside 01, simple``() = x.DoNamedTest()
    [<Test>] member x.``Comments 06 - String inside 02, triple quote``() = x.DoNamedTest()
    [<Test>] member x.``Comments 07 - String inside 03, verbatim``() = x.DoNamedTest()

    [<Test>] member x.Braces() = x.DoNamedTest()
    [<Test>] member x.Keywords() = x.DoNamedTest()
    [<Test>] member x.Punctuation() = x.DoNamedTest()

    [<Test>] member x.``Hash directives 01 - Dead code``() = x.DoNamedTest()
    [<Test>] member x.``Hash directives 02 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``Hash directives 03 - Include``() = x.DoNamedTest()
    [<Test>] member x.``Hash directives 04 - Light``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - Simple arifmetic``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Logic``() = x.DoNamedTest()
    [<Test>] member x.``Operators 03 - Pipes``() = x.DoNamedTest()
    [<Test; Ignore>] member x.``Operators 04 - Comparison``() = x.DoNamedTest()
    [<Test; Ignore>] member x.``Operators 05 - Custom``() = x.DoNamedTest()
