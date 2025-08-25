namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceReturnTypeTest() =
    inherit FSharpQuickFixTestBase<ReplaceReturnTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceReturnType"

    [<Test>] member x.``Let 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Let 02``() = x.DoNamedTest()

    [<Test>] member x.``Method 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Auto 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Prop - Member 01``() = x.DoNamedTestWithSignature()
    [<Test; Explicit>] member x.``Prop - Member 02``() = x.DoNamedTest()

    [<Test>] member x.``Array 01``() = x.DoNamedTest()
    [<Test>] member x.``Constraint Mismatch``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Infix application``() = x.DoNamedTest()
    [<Test>] member x.Sequential() = x.DoNamedTest()

    [<Test>] member x.``Match clause 01``() = x.DoNamedTest()
    [<Test>] member x.``Match clause 02``() = x.DoNamedTest()
    [<Test>] member x.``Match clause 03``() = x.DoNamedTest()
    [<Test>] member x.``Match clause 04``() = x.DoNamedTest()

    [<Test>] member x.``Return type with attribute``() = x.DoNamedTest()
    [<Test>] member x.TryWith() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.MatchLambda() = x.DoNamedTest()
    [<Test>] member x.LetOrUse() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse - If``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse - Else``() = x.DoNamedTest()
    [<Test>] member x.``Elif - Then``() = x.DoNamedTest()
    [<Test>] member x.``Elif - Else``() = x.DoNamedTest()
    [<Test>] member x.``FunctionType 01``() = x.DoNamedTest()
    [<Test>] member x.``FunctionType 02``() = x.DoNamedTest()
    [<Test>] member x.``FunctionType 03``() = x.DoNamedTest()
    [<Test>] member x.``Paren around return type``() = x.DoNamedTest()

    [<Test>] member x.``Type - No annotation 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - No annotation 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - No annotation 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - No annotation 04``() = x.DoNamedTest()
    [<Test>] member x.``Type - No annotation 05``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Type - No annotation - Lambda return 01``() = x.DoNamedTest()
    [<Test>] member x.``Type - No annotation - Lambda return 02``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Type - No annotation - Lambda return 03``() = x.DoNamedTest()

    [<Test>] member x.``Type - Tuple 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Type - Tuple 02``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Type - Tuple 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Tuple 04``() = x.DoNamedTest()
    [<Test>] member x.``Type - Tuple 05``() = x.DoNamedTest()
    [<Test>] member x.``Type - Tuple 06``() = x.DoNamedTest()

    [<Test>] member x.``Type - Partial 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Type - Partial 02``() = x.DoNamedTest()
    [<Test>] member x.``Type - Partial 03``() = x.DoNamedTest()
    [<Test>] member x.``Type - Partial 04``() = x.DoNamedTest()
    [<Test>] member x.``Type - Partial 05``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``No highlighting 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``No highlighting 02``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``No highlighting 03``() = x.DoNamedTest()

    [<Test; Explicit "Fix FCS error">] member x.``Not available - Pattern - Tuple 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 02``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 03``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 04``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 05``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 06``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceReturnTypeAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceReturnType"

    [<Test>] member x.``Availability - Ref pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Ref pat 02 - Operator``() = x.DoNamedTest()


[<FSharpTest>]
type ChangeElementTypeTest() =
    inherit FSharpQuickFixTestBase<ChangeTypeFromElementReferenceFix>()

    override x.RelativeTestDataPath = "features/quickFixes/changeType/reference"

    [<Test>] member x.``Field - Record 01``() = x.DoNamedTestWithSignature()
    [<Test>] member x.``Field - Record 02``() = x.DoNamedTest()
    [<Test; Explicit "Fix wrong FCS diagnostic data">] member x.``Field - Record 03``() = x.DoNamedTest()
    [<Test; Explicit "Fix wrong FCS diagnostic data">] member x.``Field - Record 04``() = x.DoNamedTest()
    [<Test>] member x.``Field - Struct 01``() = x.DoNamedTest()

    [<Test>] member x.``Prop - Auto 01``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Auto 02``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Auto 03``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Member 01``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Member 02``() = x.DoNamedTest()
    [<Test>] member x.``Prop - Member 03``() = x.DoNamedTest()


[<FSharpTest>]
type ChangeElementTypeFromSetTest() =
    inherit FSharpQuickFixTestBase<ChangeTypeFromSetExprFix>()

    override x.RelativeTestDataPath = "features/quickFixes/changeType/set"

    [<Test>] member x.``Field - Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Record 02``() = x.DoNamedTest()
    [<Test>] member x.``Field - Struct 01``() = x.DoNamedTest()


[<FSharpTest>]
type ChangeElementTypeFromFieldBindingTest() =
    inherit FSharpQuickFixTestBase<ChangeTypeFromRecordFieldBindingFix>()

    override x.RelativeTestDataPath = "features/quickFixes/changeType/init"

    [<Test>] member x.``Record 01``() = x.DoNamedTest()
    [<Test>] member x.``Record 02``() = x.DoNamedTest()
    [<Test>] member x.``Record 03``() = x.DoNamedTest()

[<FSharpTest>]
type ChangeParamTypeFromArgTest() =
    inherit FSharpQuickFixTestBase<ChangeParameterTypeFromArgumentFix>()

    override x.RelativeTestDataPath = "features/quickFixes/changeType/arg"

    [<Test>] member x.``Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Function 03``() = x.DoNamedTest()
    [<Test>] member x.``Function 04``() = x.DoNamedTest()

    [<Test>] member x.``Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Method 02``() = x.DoNamedTest()
    [<Test>] member x.``Method 03``() = x.DoNamedTest()
    [<Test>] member x.``Method 04``() = x.DoNamedTest()
