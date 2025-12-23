namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithAssignmentExpressionTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithAssignmentExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAssignmentExpression"

    [<Test>] member x.``ReferenceExpr 1 - Variable``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 2 - Record field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 3 - Mutable field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 4 - Mutable member``() = x.DoNamedTest()
    [<Test>] member x.``IndexerExpr``() = x.DoNamedTest()
    [<Test>] member x.``Unit type expected error``() = x.DoNamedTest()

    [<Test>] member x.``ReferenceExpr 1 - Not mutable field, not available``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 2 - Not mutable member, not available``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 3 - Function arg, not available``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 4 - Pattern matching, not available``() = x.DoNamedTest()

    [<TestReferenceProjectOutput("FSharpRecord")>]
    [<Test>] member x.``ReferenceExpr 5 - Compiled record, not available``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceWithAssignmentExpressionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAssignmentExpression"

    [<Test>] member x.``Availability 01 - Unit type expected warning``() = x.DoNamedTest()
    [<Test>] member x.``Availability 02 - Unit type expected error``() = x.DoNamedTest()
