namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddMissingMatchClausesFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingMatchClausesFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingMatchClauses"

    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()

    [<Test>] member x.``And 01``() = x.DoNamedTest()

    [<Test>] member x.``Bool 01``() = x.DoNamedTest()
    [<Test>] member x.``Bool 02 - Wrong type``() = x.DoNamedTest()
    [<Test>] member x.``Bool 03 - Named literal``() = x.DoNamedTest()

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Enum 02 - Named literal``() = x.DoNamedTest()
    [<Test>] member x.``Enum 03 - Duplicate field``() = x.DoNamedTest()
    [<Test>] member x.``Enum 04 - Unnamed``() = x.DoNamedTest()
    [<Test>] member x.``Enum 05 - Rqa module``() = x.DoNamedTest()

    [<Test>] member x.``List 01``() = x.DoNamedTest()
    [<Test>] member x.``List 02``() = x.DoNamedTest()
    [<Test>] member x.``List 03``() = x.DoNamedTest()
    [<Test>] member x.``List 04``() = x.DoNamedTest()

    [<Test>] member x.``Or 01``() = x.DoNamedTest()

    [<Test>] member x.``Tuple - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Union 02 - Escaped name``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Discard``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 05 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 06``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 07``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 08``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 09``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 10``() = x.DoNamedTest()

    [<Test>] member x.``Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Union 02``() = x.DoNamedTest()
    [<Test>] member x.``Union 03``() = x.DoNamedTest()
    [<Test>] member x.``Union 04``() = x.DoNamedTest()
    [<Test>] member x.``Union 05 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Union 06 - Discard fields``() = x.DoNamedTest()
    [<Test>] member x.``Union 07 - Option``() = x.DoNamedTest()
    [<Test>] member x.``Union 08 - Nested generic``() = x.DoNamedTest()
    [<Test>] member x.``Union 09 - Escaped name``() = x.DoNamedTest()
    [<Test>] member x.``Union 10 - Option``() = x.DoNamedTest()
    [<Test>] member x.``Union 11 - Generic``() = x.DoNamedTest()
    [<Test>] member x.``Union 12 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Union 13 - Other namespace``() = x.DoNamedTest()
    [<Test>] member x.``Union 14 - Param in parens``() = x.DoNamedTest()
    [<Test>] member x.``Union 15 - When``() = x.DoNamedTest()
    [<Test>] member x.``Union 16``() = x.DoNamedTest()
    [<Test>] member x.``Union 17``() = x.DoNamedTest()
    [<Test>] member x.``Union 18``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Union 19``() = x.DoNamedTest()
