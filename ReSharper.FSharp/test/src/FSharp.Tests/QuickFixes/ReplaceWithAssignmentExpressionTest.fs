namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type ReplaceWithAssignmentExpressionTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithAssignmentExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAssignmentExpression"

    [<Test>] member x.``ReferenceExpr 1 - Variable``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 2 - Record field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 3 - Mutable field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 4 - Mutable member``() = x.DoNamedTest()
    [<Test>] member x.``IndexerExpr``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``ReferenceExpr 1 - Not mutable field, not available``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``ReferenceExpr 2 - Not mutable member, not available``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``ReferenceExpr 3 - Function arg, not available``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``ReferenceExpr 4 - Pattern matching, not available``() = x.DoNamedTest()

    [<TestReferences("../../../assemblies/FSharpRecord.dll")>]
    [<Test; NotAvailable>] member x.``ReferenceExpr 5 - Compiled record, not available``() = x.DoNamedTest()
