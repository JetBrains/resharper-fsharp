namespace rec JetBrains.ReSharper.Plugins.FSharp.Tests.Features.TypingAssist

open System.IO
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.TypingAssist
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.TypingAssist
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.CachingLexers
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl
open NUnit.Framework

[<FSharpTest>]
[<TestSettingsKey(typeof<FSharpFormatSettingsKey>)>]
type FSharpTypingAssistTest() =
    inherit TypingAssistTestBase()

    override x.RelativeTestDataPath = "features/service/typingAssist"

    [<Test>] member x.``Enter 00 - File beginning``() = x.DoNamedTest()
    [<Test>] member x.``Enter 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 02 - Dumb indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 03 - Dumb indent, trim spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 04 - Dumb indent, empty line``() = x.DoNamedTest()

    [<Test>] member x.``Enter 05 - Indent after =``() = x.DoNamedTest()
    [<Test>] member x.``Enter 06 - Indent after = and spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 07 - Indent after = and spaces, comments``() = x.DoNamedTest()
    [<Test>] member x.``Enter 08 - Indent after = and line with spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 09 - Indent after = and line with comments``() = x.DoNamedTest()
    [<Test>] member x.``Enter 10 - Indent after = and line with source``() = x.DoNamedTest()

    [<Test>] member x.``Enter 11 - Left paren``() = x.DoNamedTest()
    [<Test>] member x.``Enter 12 - Left paren and eol space``() = x.DoNamedTest()
    [<Test>] member x.``Enter 13 - Left paren and space before``() = x.DoNamedTest()
    [<Test>] member x.``Enter 14 - List, first element``() = x.DoNamedTest()
    [<Test>] member x.``Enter 15 - List, last element``() = x.DoNamedTest()

    [<Test>] member x.``Enter 16 - After list``() = x.DoNamedTest()
    [<Test>] member x.``Enter 17 - After multiple continued lines``() = x.DoNamedTest()
    [<Test>] member x.``Enter 18 - After single continued line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 19 - After pair starting at line start``() = x.DoNamedTest()

    [<Test>] member x.``Enter 20 - Nested indent after =``() = x.DoNamedTest()
    [<Test>] member x.``Enter 21 - Nested indent after = and comments``() = x.DoNamedTest()

    [<Test>] member x.``Enter 22 - Indent after = 2``() = x.DoNamedTest()
    [<Test>] member x.``Enter 23 - After new line ctor and =``() = x.DoNamedTest()
    [<Test>] member x.``Enter 24 - Add indent after continued line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 25 - Add indent after continued line before block``() = x.DoNamedTest()

    [<Test>] member x.``Enter 26 - Empty line, add indent from below``() = x.DoNamedTest()
    [<Test>] member x.``Enter 27 - Empty line, dump indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 28 - No indent after else and new line``() = x.DoNamedTest()

    [<Test>] member x.``Enter 29 - No indent before source``() = x.DoNamedTest()
    [<Test>] member x.``Enter 30 - No indent before source 2``() = x.DoNamedTest()
    [<Test>] member x.``Enter 31 - Inside empty ctor``() = x.DoNamedTest()
    [<Test>] member x.``Enter 32 - Nested binding``() = x.DoNamedTest()
    [<Test>] member x.``Enter 33 - After then on line with multiple parens in row``() = x.DoNamedTest()
    [<Test>] member x.``Enter 34 - After line with multiple parens in row``() = x.DoNamedTest()
    [<Test>] member x.``Enter 35 - Nested binding and indent``() = x.DoNamedTest()

    [<Test>] member x.``Enter 36 - Indent after =, trim before source``() = x.DoNamedTest()
    [<Test>] member x.``Enter 37 - After first list element, trim before source``() = x.DoNamedTest()

    [<Test>] member x.``Enter 38 - Empty list``() = x.DoNamedTest()
    [<Test>] member x.``Enter 39 - Empty list with spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 40 - Empty list continuing line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 41 - Empty array continuing line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 42 - Before first list element and new line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 43 - Before first list element``() = x.DoNamedTest()
    [<Test>] member x.``Enter 44 - Before first list element and spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 45 - Before first list element in multiline list``() = x.DoNamedTest()
    [<Test>] member x.``Enter 46 - Before first list element in multiline list``() = x.DoNamedTest()
    [<Test>] member x.``Enter 47 - Before first list element in multiline list``() = x.DoNamedTest()

    [<Test>] member x.``Enter 48 - After =``() = x.DoNamedTest()
    [<Test>] member x.``Enter 49 - After yield!``() = x.DoNamedTest()
    [<Test>] member x.``Enter 50 - After line with attribute``() = x.DoNamedTest()
    [<Test>] member x.``Enter 51 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Enter 52 - Nested indents and parens``() = x.DoNamedTest()
    [<Test>] member x.``Enter 53 - After when``() = x.DoNamedTest()
    [<Test>] member x.``Enter 54 - After mismatched {``() = x.DoNamedTest()
    [<Test>] member x.``Enter 55 - Should not continue line``() = x.DoNamedTest()
    [<Test>] member x.``Enter 56 - After then before source``() = x.DoNamedTest()
    [<Test>] member x.``Enter 57 - After + before source``() = x.DoNamedTest()
    [<Test>] member x.``Enter 58 - Object expression``() = x.DoNamedTest()
    [<Test>] member x.``Enter 59 - After when, add space before rarrow``() = x.DoNamedTest()
    [<Test>] member x.``Enter 59 - After new``() = x.DoNamedTest()

    [<Test>] member x.``Enter after arrow 01``() = x.DoNamedTest()
    
    [<Test>] member x.``Enter after error 01 - If``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 02 - If``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 03 - If``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 04 - While``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 05 - multiline if``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 06 - match``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 07 - multiline if with parens``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 08 - multiline if before then``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 09 - After then``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 10 - After while``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 11 - After while and comments``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 12 - After then and elif``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 13 - After for in do``() = x.DoNamedTest()
    [<Test>] member x.``Enter after error 14 - After for do``() = x.DoNamedTest()
    
    [<Test>] member x.``Enter in comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Enter in comment 02``() = x.DoNamedTest()
    [<Test>] member x.``Enter in comment 03``() = x.DoNamedTest()

    [<Test>] member x.``Enter in string 01 - Inside empty triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Enter in string 02 - Inside triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Enter in string 03 - Inside triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Enter in string 04 - Inside multiline triple-quoted string``() = x.DoNamedTest()

    [<Test>] member x.``Enter before dot 01``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 02``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 03 - After parens``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 04 - After ctor``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 05 - Chained methods``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 06 - Chained members``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 07 - Indexer``() = x.DoNamedTest()
    [<Test>] member x.``Enter before dot 08 - Multiline indexer``() = x.DoNamedTest()

    [<Test>] member x.``Enter in app 01``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 02``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 03``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 04 - After last arg``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 05 - After last arg and comment``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 06 - Inside method invoke``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 07 - Joined args in parens``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 08 - Infix app``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 09 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 10 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 11 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 12 - Before pipe``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 13 - Before pipe``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 14 - After infix op``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 15 - After first token on line``() = x.DoNamedTest()
    [<Test>] member x.``Enter in app 16 - After last infix op token on line``() = x.DoNamedTest()

    [<Test>] member x.``Backspace 01 - Inside empty triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 02 - Inside multiline triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 03 - Inside multiline triple-quoted string 2``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 04 - Inside triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 05 - Inside triple-quoted string 2``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 06 - Inside multiline triple-quoted string``() = x.DoNamedTest()
    [<Test>] member x.``Backspace 07 - Inside multiline triple-quoted string``() = x.DoNamedTest()
    
    [<Test>] member x.``Space 01 - Inside empty list``() = x.DoNamedTest()
    [<Test>] member x.``Space 02 - Inside empty array``() = x.DoNamedTest()
    [<Test>] member x.``Space 03 - Inside empty quotation, typed``() = x.DoNamedTest()
    [<Test>] member x.``Space 04 - Inside empty quotation, untyped``() = x.DoNamedTest()
    [<Test>] member x.``Space 05 - Inside empty quotation, no assist``() = x.DoNamedTest()
    [<Test>] member x.``Space 06 - Inside operator, no assist``() = x.DoNamedTest()
    [<Test>] member x.``Space 07 - Inside empty array, no assist``() = x.DoNamedTest()
    [<Test>] member x.``Space 08 - Inside empty braces``() = x.DoNamedTest()

    [<Test>] member x.``Quotes - Adding third quote``() = x.DoNamedTest()

    [<Test>] member x.``Quotes - Pair quotes 01 - Before code``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 02 - Insert pair``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 03 - Insert pair inside string``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 04 - Wrong quote type``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 05 - In parens``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 06 - Insert single escaped``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 07 - Insert single escaped``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 08 - Before list``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 09 - Insert single in multiline``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 10 - Insert single in multiline``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 11 - At eof``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Pair quotes 12 - At eof, unfinished triple quote``() = x.DoNamedTest()

    [<Test>] member x.``Quotes - Skip end 01 - Char``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 02 - String``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 03 - Verbatim``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 05 - Triple-quote``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 06 - Triple-quote``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 07 - Triple-quote``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 08 - ByteArray``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 09 - No skip``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 10 - No skip``() = x.DoNamedTest()
    [<Test>] member x.``Quotes - Skip end 11 - No skip``() = x.DoNamedTest()

    [<Test>] member x.``Brackets - Left 01 - Add right``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Left 02 - No add right before other``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Left 03 - Add right before other and space``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Left 04 - No add angle``() = x.DoNamedTest()

    [<Test>] member x.``Brackets - Skip right 01``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Skip right 02 - After code``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Skip right 03 - After code and space``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Skip right 04 - Angle``() = x.DoNamedTest()

    [<Test>] member x.``Brackets - Insert right 01``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Insert right 02 - After code and brace``() = x.DoNamedTest()

    [<Test>] member x.``Brackets - Backspace 01 - Erase both``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Backspace 02 - Erase single, unbalanced left``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Backspace 03 - Erase single, unbalanced right``() = x.DoNamedTest()

    [<Test>] member x.``Brackets - Attributes 01 - Type left angle``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Attributes 02 - Type left angle before spaces``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Attributes 03 - Type right angle``() = x.DoNamedTest()
      
    [<Test>] member x.``Brackets - Arrays 01 - Type left bar``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 02 - Type left bar before space``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 03 - Type left bar in multiline list``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 04 - Type right bar``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 05 - Type right bar in multiline list``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 06 - Type right bar in multiline list``() = x.DoNamedTest()
    [<Test>] member x.``Brackets - Arrays 07 - Type bar in multiline list wrong formatting``() = x.DoNamedTest()

    [<Test>] member x.``At 01 - Make quotation after left angle``() = x.DoNamedTest()
    [<Test>] member x.``At 02 - Inside empty typed quotations``() = x.DoNamedTest()
    [<Test>] member x.``At 03 - Inside empty typed quotations and spaces 01``() = x.DoNamedTest()
    [<Test>] member x.``At 04 - Inside empty typed quotations and spaces 02``() = x.DoNamedTest()
    [<Test>] member x.``At 05 - Inside empty typed quotations and spaces 03``() = x.DoNamedTest()
    [<Test>] member x.``At 06 - Inside empty typed quotations and spaces 04``() = x.DoNamedTest()
    [<Test>] member x.``At 07 - Inside empty typed quotations and spaces 05``() = x.DoNamedTest()
    [<Test>] member x.``At 08 - Inside empty typed quotations and spaces 06``() = x.DoNamedTest()
    [<Test>] member x.``At 09 - Inside empty typed quotations and spaces 07``() = x.DoNamedTest()
    [<Test>] member x.``At 10 - Inside empty typed quotations and spaces 08``() = x.DoNamedTest()

[<FSharpTest>]
type LineIndentsTest() =
    inherit LineIndentsTestBase()

    [<Test>] member x.``Indents``() = x.DoNamedTest()

    override x.DoLineTest(writer, textControl, line) =
        writer.WriteLine(getLineIndent x.CachingLexerService textControl line)

[<FSharpTest>]
type NestedIndentsTest() =
    inherit LineIndentsTestBase()

    [<Test>] member x.``Nested indents``() = x.DoNamedTest()

    override x.DoLineTest(writer, textControl, line) =
        writer.WriteLine(tryGetNestedIndentBelowLine x.CachingLexerService textControl line)

[<AbstractClass>]
type LineIndentsTestBase() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/service/typingAssist"

    member x.CachingLexerService =
        x.Solution.GetComponent<CachingLexerService>()

    abstract DoLineTest: TextWriter * ITextControl * Line -> unit 

    override x.DoTest(project: IProject) =
        use textControl = x.OpenTextControl(project)
        let linesCount = int (textControl.Document.GetLineCount())
        x.ExecuteWithGold(fun writer ->
            for i in 1 .. linesCount do
                x.DoLineTest(writer, textControl, docLine (i - 1))) |> ignore
