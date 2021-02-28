namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantPatParenTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/pat"

    [<Test>] member x.``No space 01 - Before``() = x.DoNamedTest()
    [<Test>] member x.``No space 02 - After``() = x.DoNamedTest()


[<FSharpTest; TestPackages(FSharpCorePackage)>]
type RemoveRedundantTypeUsageParenTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenTypeUsageFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/typeUsage"

    [<Test>] member x.``Type argument list 01``() = x.DoNamedTest()
