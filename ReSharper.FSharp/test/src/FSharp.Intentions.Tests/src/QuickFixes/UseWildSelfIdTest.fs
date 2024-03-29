﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UseWildSelfIdTest() =
    inherit FSharpQuickFixTestBase<UseWildSelfIdFix>()

    override x.RelativeTestDataPath = "features/quickFixes/useWildSelfId"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Object expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Used name 01``() = x.DoNamedTest()

    [<Test; NoHighlightingFound>] member x.``Not available - Used name 01``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Not available - Used name 02``() = x.DoNamedTest()
