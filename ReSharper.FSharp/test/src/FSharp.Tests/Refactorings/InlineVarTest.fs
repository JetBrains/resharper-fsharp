﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Refactorings.Test.Common
open NUnit.Framework

[<FSharpTest>]
type InlineVarTest() =
    inherit InlineVarTestBase()

    override x.RelativeTestDataPath = "features/refactorings/inlineVar"

    // todo: remove parens
    [<Test>] member x.``Expr - If - Binary 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - If - Binary 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If - Match 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If - Match 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If - Then 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - If - Then 02 - No else``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda - Binary 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda - Type cast 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match - If 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref - Operator 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref - Operator 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Ref - Operator 03 - Compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Type cast - Binary 01 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Type cast - Binary 02 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Type check - Binary 01 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Type check - Binary 02 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed - Binary 01 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed - Binary 02 - Ignore``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Typed - Tuple 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Qualifier - Ident 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Qualifier - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Qualifier - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Qualifier - Method 02``() = x.DoNamedTest()

    [<Test; Explicit("Use FSharp.Core 5 in tests")>] member x.``Interpolation - App 01``() = x.DoNamedTest()
    [<Test; Explicit("Use FSharp.Core 5 in tests")>] member x.``Interpolation - Record 01``() = x.DoNamedTest()
    [<Test; Explicit("Use FSharp.Core 5 in tests")>] member x.``Interpolation - Record 02 - Anon``() = x.DoNamedTest()
    [<Test; Explicit("Use FSharp.Core 5 in tests")>] member x.``Interpolation - Ref 01``() = x.DoNamedTest()
    [<Test; Explicit("Use FSharp.Core 5 in tests")>] member x.``Interpolation - Ref 02 - Qualifier``() = x.DoNamedTest()

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Parens``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Comment 01``() = x.DoNamedTest() // todo: formatter: fix comments
    [<Test; Explicit>] member x.``Comment 02 - Empty line``() = x.DoNamedTest() // todo: formatter: fix comments
    [<Test; Explicit>] member x.``Comment 03 - Empty lines``() = x.DoNamedTest() // todo: formatter: fix comments

    [<Test>] member x.``In keyword 01``() = x.DoNamedTest()
    [<Test>] member x.``In keyword 02 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Inline 01``() = x.DoNamedTest()
    [<Test; Explicit("Parsed wrongly: dotnet/fsharp#7741")>] member x.``Inline 02 - Multiline body``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()

    [<Test>] member x.``Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern 02``() = x.DoNamedTest()

    [<Test>] member x.``Qualifier - App - High 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - App 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - Ident 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - Ident 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier - If 01``() = x.DoNamedTest()

    [<Test>] member x.``Space 01``() = x.DoNamedTest()

    [<Test>] member x.``Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Top level 02 - In type``() = x.DoNamedTest()

    [<Test>] member x.``Not available - AddressOf 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set - Indexer 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set - Indexer 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set - Indexer 03 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set - Indexer 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Set 02 - Parens``() = x.DoNamedTest()
