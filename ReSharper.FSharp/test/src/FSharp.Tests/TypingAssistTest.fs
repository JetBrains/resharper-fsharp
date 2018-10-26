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

    [<Test>] member x.``Enter 01 - No indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 02 - Dumb indent``() = x.DoNamedTest()
    [<Test>] member x.``Enter 03 - Dumb indent, trim spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 04 - Dumb indent, trim empty line``() = x.DoNamedTest()

    [<Test>] member x.``Enter 05 - Indent after =``() = x.DoNamedTest()
    [<Test>] member x.``Enter 06 - Indent after = and spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 07 - Indent after = and spaces, comments``() = x.DoNamedTest()
    [<Test>] member x.``Enter 08 - Indent after = and line with spaces``() = x.DoNamedTest()
    [<Test>] member x.``Enter 09 - Indent after = and line with comments``() = x.DoNamedTest()
    [<Test>] member x.``Enter 10 - Indent after = and line with source``() = x.DoNamedTest()

    [<Test>] member x.``Enter 11 - Left paren``() = x.DoNamedTest()
    [<Test>] member x.``Enter 12 - Left paren and eol space``() = x.DoNamedTest()
    [<Test>] member x.``Enter 13 - Left paren and space before``() = x.DoNamedTest()
    [<Test>] member x.``Enter 14 - List``() = x.DoNamedTest()

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
        writer.WriteLine(tryGetNestedIndentBelow x.CachingLexerService textControl line)
        
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
