namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithAssignmentExpressionTestTest() =
    inherit QuickFixTestBase<ReplaceWithAssignmentExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAssignmentExpression"

    [<Test>] member x.``ReferenceExpr 1 - Variable``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 2 - Record field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 3 - Mutable field``() = x.DoNamedTest()
    [<Test>] member x.``ReferenceExpr 4 - Mutable member``() = x.DoNamedTest()
    [<Test>] member x.``IndexerExpr``() = x.DoNamedTest()