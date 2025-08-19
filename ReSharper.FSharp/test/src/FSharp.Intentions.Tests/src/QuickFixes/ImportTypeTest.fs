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

    [<NotAvailable>]
    [<Test>] member x.``Accessibility 01``() = x.DoNamedTest()
    [<Test>] member x.``Accessibility 02``() = x.DoNamedTest()
    [<Test>] member x.``Accessibility 03``() = x.DoNamedTest()
    [<Test>] member x.``Accessibility 04``() = x.DoNamedTestWithSignatureAndSecondFile()
    [<NotAvailable>]
    [<Test>] member x.``Accessibility 05``() = x.DoNamedTestWithSignatureAndSecondFile()
    [<NotAvailable>]
    [<Test>] member x.``Accessibility 06``() = x.DoNamedTest()
    [<Test>] member x.``Accessibility 07``() = x.DoNamedTest()
    [<Test>] member x.``Accessibility 08``() = x.DoNamedTestWithTwoFiles()
    [<NotAvailable>]
    [<Test>] member x.``Accessibility 09``() = x.DoNamedTestWithTwoFiles()


[<FSharpTest>]
type ImportExtensionMemberTest() =
    inherit FSharpQuickFixTestBase<FSharpImportExtensionMemberFix>()

    override x.RelativeTestDataPath = "features/quickFixes/import/extension"

    member x.DoNamedTestFsCs() =
        x.DoTestSolution(FSharpTestUtil.referenceCSharpProject x)

    [<Test>] member x.``Expr - Dot lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Dot lambda 02``() = x.DoNamedTest()

    [<Test>] member x.``Extension - CSharp - Array 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp - Array 02``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 01``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 02``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 03``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 04``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 05``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 06``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 07``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 08``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 09``() = x.DoNamedTestFsCs()
    [<Test>] member x.``Extension - CSharp 10``() = x.DoNamedTestFsCs()
    
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 01``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 02``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 03``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 04``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 05``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 06``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 07``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 08``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 09``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("FSharpExtensions")>] member x.``FSharp - Compiled 10``() = x.DoNamedTest()

    [<Test>] member x.``FSharp - Source 01``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 02``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 03``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 04``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 05``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 06``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 07``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 08``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 09``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 10``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 11``() = x.DoNamedTest()
    [<Test>] member x.``FSharp - Source 12``() = x.DoNamedTest()

    [<Test>] member x.``FSharp - Nested module 01``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not Available - Type 01``() = x.DoNamedTest()


[<FSharpTest>]
type FSharpImportModuleMemberTest() =
    inherit FSharpQuickFixTestBase<FSharpImportModuleMemberFix>()

    override x.RelativeTestDataPath = "features/quickFixes/import/moduleMember"

    [<Test>] member x.``Expr - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Active pattern 04``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Expr - Active pattern 05``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Literal 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Literal 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Literal 05``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Literal 06``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Expr - Literal 07``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Union 02 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Union 03 - AutoOpen Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Union 04 - Rqa Rqa``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Expr - Union 05 - Rqa AutoOpen Rqa``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Expr - Union 06``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Value 02``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Expr - Value 03``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Function 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Function 04``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Expr - Function 05``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Active pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Active pattern 02``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Pattern - Active pattern 03``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Literal 02``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Literal 03``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Literal 04``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Literal 05``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Literal 06``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Pattern - Literal 07``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union 02 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union 03 - AutoOpen Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Union 04 - Rqa Rqa``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Pattern - Union 05 - Rqa AutoOpen Rqa``() = x.DoNamedTest()
    [<Test; TestReferenceProjectOutput("ModuleMembers")>] member x.``Pattern - Union 06``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Not available - Internal 01 - Literal``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Not available - Internal 02 - Value``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Not available - Internal 03 - Function``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available - Unreachable 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Unreachable 02``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Unreachable 03``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Unreachable 04``() = x.DoNamedTest()


[<FSharpTest>]
type FSharpImporStaticMemberTest() =
    inherit FSharpQuickFixTestBase<FSharpImportStaticMemberFromQualifierTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/import/staticMember"

    [<Test>] member x.``Debugger 01``() = x.DoNamedTest()
    [<Test>] member x.``Debugger 02``() = x.DoNamedTest()
    [<Test>] member x.``String 01``() = x.DoNamedTest()
