namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type GenerateMissingRecordFieldsTest() =
    inherit FSharpQuickFixTestBase<GenerateMissingRecordFieldsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingRecordFields"

    [<Test>] member x.``Empty 01``() = x.DoNamedTest()
    [<Test>] member x.``Empty 02 - Space``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Empty 03 - New line``() = x.DoNamedTest()

    [<Test>] member x.``Single line 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line 02 - Semi``() = x.DoNamedTest()
    [<Test>] member x.``Single line 03 - Spaces``() = x.DoNamedTest()
    [<Test>] member x.``Single line 04 - Add two fields``() = x.DoNamedTest()
    [<Test>] member x.``Single line 05 - Name with spaces``() = x.DoNamedTest()
    [<Test>] member x.``Single line 06 - Convert to multiline``() = x.DoNamedTest()
    [<Test>] member x.``Single line 07 - Ordered beginning``() = x.DoNamedTest()
    [<Test>] member x.``Single line 08 - Ordered middle``() = x.DoNamedTest()
    [<Test>] member x.``Single line 09 - Unordered``() = x.DoNamedTest()
    [<Test>] member x.``Single line 10 - Ordered comment 01``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 03 - Ordered beginning``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 04 - Ordered middle``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 05 - Unordered``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 06 - Ordered comment 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 07 - Ordered comment 02``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 08 - Ordered comment 03``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 09 - Only middle``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 10``() = x.DoNamedTest()

    [<Test>] member x.``Empty function``() = x.DoNamedTest()

    [<Test>] member x.``Unresolved field 01``() = x.DoNamedTest()
    [<Test>] member x.``Unresolved field 02``() = x.DoNamedTest()

    // The quickfix should apply if the empty record is the final statement of a function binding, as that's what the
    // annotated return type pertains to.
    [<Test; NotAvailable>] member x.``Empty function statement no-op``() = x.DoNamedTest()
