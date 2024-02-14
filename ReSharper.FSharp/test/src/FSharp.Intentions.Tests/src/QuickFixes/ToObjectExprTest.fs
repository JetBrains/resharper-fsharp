namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToObjectExprTest() =
    inherit FSharpQuickFixTestBase<ToObjectExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toObjExpr"

    [<Test>] member x.``Class 01`` () = x.DoNamedTest()
    [<Test>] member x.``Class 02`` () = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available 01 - No arg``() = x.DoNamedTest()
