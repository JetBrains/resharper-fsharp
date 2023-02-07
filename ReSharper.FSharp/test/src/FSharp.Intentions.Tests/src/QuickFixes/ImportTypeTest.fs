namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type ImportTypeTest() =
    inherit FSharpQuickFixTestBase<FSharpImportTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/importType"

    [<Test>] member x.``Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Type 02 - Empty line``() = x.DoNamedTest()
    [<Test>] member x.``Type extension 01``() = x.DoNamedTest()

    [<Test>] member x.``Generic List 01``() = x.DoNamedTest()
    [<Test>] member x.``Generic List 02``() = x.DoNamedTest()
    [<Test>] member x.``Generic List 03``() = x.DoNamedTest()
    [<Test>] member x.``Generic List 04``() = x.DoNamedTest()

    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()

    [<Test>] member x.``Type arguments - Count 01``() = x.DoNamedTest()
    [<Test>] member x.``Module name - Escaped 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace - Module 02``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace - Module 03``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace - Module 04``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace - Module 05``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner namespace 02 - Sibling``() = x.DoNamedTest()

    [<Test>] member x.``Nested Module - Attribute 01``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Nested Module - Attribute 02 - Prefer nested``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpOptions>, "TopLevelOpenCompletion", "false")>]
    [<Test>] member x.``Nested Module - Attribute 03 - Nested modules``() = x.DoNamedTest()

    [<Test>] member x.``Nested Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Nested Module 02``() = x.DoNamedTest()

    [<Test>] member x.``Open group 01 - Type``() = x.DoNamedTest()

    [<Test>] member x.``Qualifiers - Expr - Imported 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Expr - Imported 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Expr 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Reference name - Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Reference name - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifiers - Type extension 01``() = x.DoNamedTest()

    [<Test>] member x.``Qualifiers - Namespace 01``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available 01 - Open``() = x.DoNamedTest()
