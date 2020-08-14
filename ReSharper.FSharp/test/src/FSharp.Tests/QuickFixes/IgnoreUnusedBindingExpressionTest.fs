namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open NUnit.Framework

[<FSharpTest>]
type IgnoreUnusedBindingExpressionTest() =
    inherit FSharpQuickFixTestBase<IgnoreUnusedBindingExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/ignoreUnusedBindingExpression"

    override x.DoTest(lifetime, project) =
        base.DoTest(lifetime, project)

        for projectFile in project.GetAllProjectFiles() do
            let file = projectFile.GetPrimaryPsiFile()
            x.ExecuteWithSpecifiedGold(
                x.GetTestDataFilePath2(x.TestName + ".dump.gold"),
                fun writer -> DebugUtil.DumpPsi(writer, file)) |> ignore

    [<Test>] member x.``Single line 01 - No parens``() = x.DoNamedTest()
    [<Test>] member x.``Single line 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Single line 03 - If expr``() = x.DoNamedTest()
    [<Test>] member x.``Single line 04 - Match expr``() = x.DoNamedTest()
    [<Test>] member x.``Single line 05 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Single line 06 - Lazy``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Single line 07 - Comment``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Single line 08 - Ignore``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Single line 09 - Do expr``() = x.DoNamedTest()
    [<Test>] member x.``Single line 10 - Assert expr``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Single line 11 - Function``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01 - Seq expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02 - Nested let``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 03 - If expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 04 - Match expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 05 - Match expr - Deindented last clause``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 06 - If expr - Deindented else expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 07 - Paren expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 08 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 09 - Lazy``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 10 - Match lambda expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 11 - Lambda expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 12 - Try with expr``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 13 - Try finally expr``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Multiline 14 - Unit prefix app``() = x.DoNamedTest()
    [<Test;>] member x.``Multiline 15 - Seq expr with let expr``() = x.DoNamedTest()
