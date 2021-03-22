module JetBrains.ReSharper.Plugins.FSharp.Tests.Features.FindUsages

open JetBrains.ReSharper.FeaturesTestFramework.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpOccurenceKindTest() =
    inherit OccurrenceKindTestBase()

    override x.RelativeTestDataPath = "features/findUsages/occurenceKinds"

    [<Test>] member x.``Import 01``() = x.DoNamedTest()

    [<Test>] member x.``New instance 01``() = x.DoNamedTest()
    [<Test>] member x.``New instance 02``() = x.DoNamedTest()
    [<Test>] member x.``New instance 03 - New``() = x.DoNamedTest()
    [<Test>] member x.``New instance 04 - Attribute``() = x.DoNamedTest()

    [<Test>] member x.``Unions 01``() = x.DoNamedTest()
    [<Test>] member x.``Unions 02 - Single empty case``() = x.DoNamedTest()

    [<Test>] member x.``Exception - Singleton 01``() = x.DoNamedTest()
    [<Test>] member x.``Exception - Singleton 02 - Type specification``() = x.DoNamedTest()
    [<Test>] member x.``Exception - Singleton 03 - Members``() = x.DoNamedTest()
    [<Test>] member x.``Exception - Fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Exception - Fields 02 - Type specification``() = x.DoNamedTest()
    [<Test>] member x.``Exception - Fields 03 - Members``() = x.DoNamedTest()

    [<Test>] member x.``Base Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Base Type 02``() = x.DoNamedTest()
    [<Test>] member x.``Base Type 03 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Base Type 04 - Inherit arg``() = x.DoNamedTest()

    [<Test>] member x.``Base Type - Object expressions 01 - Class``() = x.DoNamedTest()
    [<Test>] member x.``Base Type - Object expressions 02 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Base Type - Object expressions 03 - Secondary interfaces``() = x.DoNamedTest()

    [<Test>] member x.``Type Argument 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 02 - Pattern``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 03 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 04 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 05 - Return type``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 06 - ML``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 07 - Tuple abbreviation``() = x.DoNamedTest()

    [<Test>] member x.``Type Cast 01 - Upcast``() = x.DoNamedTest()
    [<Test>] member x.``Type Cast 02 - Downcast``() = x.DoNamedTest()

    [<Test>] member x.``Type Test 01 - Expr``() = x.DoNamedTest()
    [<Test>] member x.``Type Test 02 - Pattern``() = x.DoNamedTest()

    [<Test>] member x.``Type abbreviation 01``() = x.DoNamedTest()

    [<Test>] member x.``Read 01``() = x.DoNamedTest()
    [<Test>] member x.``Read 02 - Anon record field``() = x.DoNamedTest()

    [<Test>] member x.``Write - Expr 01 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``Write - Expr 02 - Paren``() = x.DoNamedTest()
    [<Test>] member x.``Write - Expr 03 - Indexer``() = x.DoNamedTest()
    [<Test>] member x.``Write - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Write - Record 02 - Qualified name``() = x.DoNamedTest()
    [<Test>] member x.``Write - Record 03 - Copy and update``() = x.DoNamedTest()

    [<Test>] member x.``Type Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Extension 02 - Qualified``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Record 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Exception 02 - Fields``() = x.DoNamedTest()

    [<Test>] member x.``Members - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Members - Override 01``() = x.DoNamedTest()

    [<Test>] member x.``Field decl - Anon record 01``() = x.DoNamedTest()
