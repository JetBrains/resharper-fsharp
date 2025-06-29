namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddMissingInnerPatternsFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingInnerPatternsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingMatchClauses/inner"

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

    [<Test>] member x.``Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Record 02``() = x.DoNamedTest()

    [<Test>] member x.``Tuple - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Union 02 - Escaped name``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Discard``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 04 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 05 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 11 - Struct``() = x.DoNamedTest()
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
    [<Test>] member x.``Union 19``() = x.DoNamedTest()


[<FSharpTest>]
type AddMissingPatternsFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingPatternsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingMatchClauses/simplified"

    [<Test; NotAvailable>] member x.``Not available - Bar 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Single line 01``() = x.DoNamedTest()

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Comment 02 - Eof``() = x.DoNamedTest()
    [<Test>] member x.``Comment 03``() = x.DoNamedTest()
    [<Test>] member x.``Comment 04 - Space``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Comment 05``() = x.DoNamedTest()

    [<Test>] member x.``Empty line 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty line 02``() = x.DoNamedTest() // todo: formatter: add empty line

    [<Test>] member x.``Indent 01``() = x.DoNamedTest()
    [<Test>] member x.``Indent 02``() = x.DoNamedTest()
    [<Test>] member x.``Indent 03``() = x.DoNamedTest()

    [<Test>] member x.``As 01``() = x.DoNamedTest()
    [<Test>] member x.``As 02``() = x.DoNamedTest()
    [<Test>] member x.``As 03``() = x.DoNamedTest()
    [<Test>] member x.``As 04``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 03``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 04``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 05``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Active pattern 06``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 07``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 08``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Active pattern 09``() = x.DoNamedTest()
    [<Test>] member x.``Active pattern 10``() = x.DoNamedTest()

    [<Test>] member x.``Array 01``() = x.DoNamedTest()
    [<Test>] member x.``Array 02``() = x.DoNamedTest()
    [<Test>] member x.``Array 03``() = x.DoNamedTest()

    [<Test>] member x.``Bool 01``() = x.DoNamedTest()

    [<Test>] member x.``Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function 02``() = x.DoNamedTest()

    [<Test>] member x.``List 01``() = x.DoNamedTest()
    [<Test>] member x.``List 02``() = x.DoNamedTest()
    [<Test>] member x.``List 03``() = x.DoNamedTest()
    [<Test>] member x.``List 04``() = x.DoNamedTest()
    [<Test>] member x.``List 05``() = x.DoNamedTest()
    [<Test>] member x.``List 06``() = x.DoNamedTest()
    [<Test>] member x.``List 07``() = x.DoNamedTest()
    [<Test>] member x.``List 08``() = x.DoNamedTest()
    [<Test>] member x.``List 09``() = x.DoNamedTest()
    [<Test>] member x.``List 10``() = x.DoNamedTest()
    [<Test>] member x.``List 11``() = x.DoNamedTest()
    [<Test>] member x.``List 12``() = x.DoNamedTest()
    [<Test>] member x.``List 13``() = x.DoNamedTest()

    [<Test>] member x.``Null 01``() = x.DoNamedTest()
    [<Test>] member x.``Null 02``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 04``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 05``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 06``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 07``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 08``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 09``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 10``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 11``() = x.DoNamedTest()

    [<Test>] member x.``Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Union 02``() = x.DoNamedTest()
    [<Test>] member x.``Union 03``() = x.DoNamedTest()
    [<Test>] member x.``Union 04``() = x.DoNamedTest()
    [<Test>] member x.``Union 05``() = x.DoNamedTest()
    [<Test>] member x.``Union 06``() = x.DoNamedTest()
    [<Test>] member x.``Union 07``() = x.DoNamedTest()
    [<Test>] member x.``Union 08``() = x.DoNamedTest()
    [<Test>] member x.``Union 09``() = x.DoNamedTest()
    [<Test>] member x.``Union 10``() = x.DoNamedTest()
    [<Test>] member x.``Union 11``() = x.DoNamedTest()
    [<Test>] member x.``Union 12``() = x.DoNamedTest()
    [<Test>] member x.``Union 13``() = x.DoNamedTest()
    [<Test>] member x.``Union 14``() = x.DoNamedTest()
    [<Test>] member x.``Union 15``() = x.DoNamedTest()
    [<Test>] member x.``Union 16``() = x.DoNamedTest()
    [<Test>] member x.``Union 17``() = x.DoNamedTest()
    [<Test>] member x.``Union 18``() = x.DoNamedTest()
    [<Test>] member x.``Union 19``() = x.DoNamedTest()
    [<Test>] member x.``Union 20``() = x.DoNamedTest()
    [<Test>] member x.``Union 21``() = x.DoNamedTest()
    [<Test>] member x.``Union 22``() = x.DoNamedTest()
    [<Test>] member x.``Union 23``() = x.DoNamedTest()
