namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantParenExprTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenExprFix>()

    override x.DoTest(lifetime, project) =
        use cookie = FSharpRegistryUtil.EnableRedundantParenAnalysisCookie.Create()
        base.DoTest(lifetime, project)
    
    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/expr"

    [<Test>] member x.``App - Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Multiline 02 - Binary``() = x.DoNamedTest()
    [<Test>] member x.``App - Multiline 03 - Deindent``() = x.DoNamedTest()
    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Spaces``() = x.DoNamedTest()
