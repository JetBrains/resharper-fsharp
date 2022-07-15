﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest; FSharpExperimentalFeature(ExperimentalFeature.RedundantParenAnalysis)>]
type RemoveRedundantParenExprTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenExprFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/expr"

    [<Test>] member x.``App - Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Multiline 02 - Binary``() = x.DoNamedTest()
    [<Test>] member x.``App - Multiline 03 - Deindent``() = x.DoNamedTest()
    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Spaces``() = x.DoNamedTest()

    [<Test; ExecuteScopedActionInFile>] member x.``Scoped 01``() = x.DoNamedTest()
    [<Test; ExecuteScopedActionInFile>] member x.``Scoped 02 - Nested``() = x.DoNamedTest()

[<FSharpTest>]
type RemoveRedundantPatParenTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/pat"

    [<Test>] member x.``No space 01 - Before``() = x.DoNamedTest()
    [<Test>] member x.``No space 02 - After``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveRedundantTypeUsageParenTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantParenTypeUsageFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantParens/typeUsage"

    [<Test>] member x.``Type argument list 01``() = x.DoNamedTest()
