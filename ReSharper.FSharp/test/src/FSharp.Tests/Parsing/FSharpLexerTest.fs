namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Lexing

open JetBrains.ReSharper.Plugins.FSharp
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
    [<Test>] member x.``Literals 03 - digits``() = x.DoNamedTest()
    [<Test>] member x.``Literals 04 - escape characters``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Byte array 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Byte array 02 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Regular 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Regular 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Regular 03``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Regular 04``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Triple quote 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Triple quote 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Triple quote 03``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Verbatim 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Verbatim 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Verbatim 03``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Verbatim 04``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Interpolated - Verbatim 05``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Regular 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Regular 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Triple quote 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Triple quote 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Eof - Triple quote 03``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Interpolated - Braces - Escape 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Braces - Escape 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Braces - Escape 03``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Interpolated - Regular 01 - No interpolation``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Regular 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Regular 03 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Triple quote - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Triple quote 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Triple quote 02``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Verbatim 01``() = x.DoNamedTest()
    [<Test>] member x.``Strings - Interpolated - Verbatim 02``() = x.DoNamedTest()

    [<Test>] member x.``Strings - Triple quote 01``() = x.DoNamedTest()

    [<Test>] member x.``Strings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Strings 02 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Strings 03 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Strings 05 - Escape characters 1``() = x.DoNamedTest()
    [<Test>] member x.``Strings 05 - Escape characters 2``() = x.DoNamedTest()
    [<Test>] member x.``Strings 05 - Escape characters 3 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Strings 05 - Escape characters 4 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Strings 06 - Keyword``() = x.DoNamedTest()
    [<Test>] member x.``Strings 07 - Backslash``() = x.DoNamedTest()
    [<Test>] member x.``Strings 08 - Quotes``() = x.DoNamedTest()

    [<Test>] member x.``Multiline strings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 02 - Triple quoted``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 03 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 04 - Empty line 1``() = x.DoNamedTest()
    [<Test>] member x.``Multiline strings 04 - Empty line 2``() = x.DoNamedTest()

    [<Test>] member x.``Unfinished strings 01``() = x.DoNamedTest()
    [<Test>] member x.``Unfinished strings 02``() = x.DoNamedTest()

    [<Test>] member x.``Comment - Eof - Block - String 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Block - String 02``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Block 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Block 02``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Block 03``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Block 04``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Line 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment - Eof - Line 02``() = x.DoNamedTest()

    [<Test>] member x.``Comment - String 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment - String 02``() = x.DoNamedTest()
    [<Test>] member x.``Comment - String 03``() = x.DoNamedTest()
    [<Test>] member x.``Comment - String 04``() = x.DoNamedTest()
    [<Test>] member x.``Comment - String 05``() = x.DoNamedTest()
    [<Test>] member x.``Comment - String 06``() = x.DoNamedTest()

    [<Test>] member x.``Comments 01 - End of line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 02 - Multiple end of line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 03 - Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Comments 03 - Multiline 02``() = x.DoNamedTest()
    [<Test>] member x.``Comments 04 - Multiline with empty line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 05 - String inside 01, simple``() = x.DoNamedTest()
    [<Test>] member x.``Comments 06 - String inside 02, triple quote``() = x.DoNamedTest()
    [<Test>] member x.``Comments 07 - String inside 03, verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Comments 08 - Type application 1 - End of line``() = x.DoNamedTest()
    [<Test>] member x.``Comments 08 - Type application 2 - Block comment``() = x.DoNamedTest()

    [<Test>] member x.Braces() = x.DoNamedTest()
    [<Test>] member x.Keywords() = x.DoNamedTest()
    [<Test>] member x.Punctuation() = x.DoNamedTest()
    [<Test>] member x.Attribute() = x.DoNamedTest()
    [<Test>] member x.``Attribute inside generic``() = x.DoNamedTest()

    [<Test>] member x.``Symbolic keyword``() = x.DoNamedTest()
    [<Test>] member x.``Code quotation``() = x.DoNamedTest()
    [<Test>] member x.``Generic declaration``() = x.DoNamedTest()

    [<Test>] member x.``Type application 01 - Correct``() = x.DoNamedTest()
    [<Test>] member x.``Type application 02 - Incorrect``() = x.DoNamedTest()
    [<Test>] member x.``Type application 03 - Smashing GREATER_BAR_RBRACK``() = x.DoNamedTest()
    [<Test>] member x.``Type application 04 - Inside generic``() = x.DoNamedTest()
    [<Test>] member x.``Type application 05``() = x.DoNamedTest()

    [<Test>] member x.``Preprocessor 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Preprocessor 02 - Disjunction``() = x.DoNamedTest()
    [<Test>] member x.``Preprocessor 03 - Conjunction``() = x.DoNamedTest()
    [<Test>] member x.``Preprocessor 04 - Nesting block 1``() = x.DoNamedTest()
    [<Test>] member x.``Preprocessor 04 - Nesting block 2``() = x.DoNamedTest()

    [<Test>] member x.``Hash directives 01 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``Hash directives 02 - Include``() = x.DoNamedTest()
    [<Test>] member x.``Hash directives 03 - Light``() = x.DoNamedTest()

    [<Test>] member x.``Operators - Custom 3 - Multiplication decl``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - Simple arithmetic``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Logic``() = x.DoNamedTest()
    [<Test>] member x.``Operators 03 - Pipes``() = x.DoNamedTest()
    [<Test>] member x.``Operators 04 - Comparison``() = x.DoNamedTest()
    [<Test>] member x.``Operators 05 - Custom 1``() = x.DoNamedTest()
    [<Test>] member x.``Operators 05 - Custom 2``() = x.DoNamedTest()
    [<Test>] member x.``Operators 06 - Symbolic``() = x.DoNamedTest()
    [<Test>] member x.``Operators 07 - Integer range``() = x.DoNamedTest()
    [<Test>] member x.``Operators 08 - Bad operator``() = x.DoNamedTest()
    [<Test>] member x.``Operators 09 - Left arrow``() = x.DoNamedTest()
    [<Test>] member x.``Operators 10 - Circumflexes``() = x.DoNamedTest()
    [<Test>] member x.``Operators 11 - More circumflexes``() = x.DoNamedTest()

    [<Test>] member x.``Identifiers 01 - Backticked``() = x.DoNamedTest()
    [<Test>] member x.``Identifiers 02 - Unfinished backticked``() = x.DoNamedTest()

    [<Test>] member x.``Anonymous Records``() = x.DoNamedTest()
