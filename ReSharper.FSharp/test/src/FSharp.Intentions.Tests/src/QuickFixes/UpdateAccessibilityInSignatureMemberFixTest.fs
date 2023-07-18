﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateAccessibilityInSignatureMemberFixTest() =
    inherit FSharpQuickFixTestBase<UpdateAccessibilityInSignatureMemberFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateAccessibilityInSignatureMemberFix"

    [<Test>] member x.``Member - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Member - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Member - 03`` () = x.DoNamedTestWithSignature()

    [<Test>] member x.``AutoProperty - 01`` () = x.DoNamedTestWithSignature()
