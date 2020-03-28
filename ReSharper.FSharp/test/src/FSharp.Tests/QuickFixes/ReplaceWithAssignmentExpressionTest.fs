namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithAssignmentExpressionTestTest() =
    inherit QuickFixTestBase<ReplaceWithAssignmentExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithAssignmentExpression"

    [<Test>] member x.``ReferenceExpr - Variable``() = x.DoNamedTest()
    [<Test>] member x.``IndexerExpr``() = x.DoNamedTest()