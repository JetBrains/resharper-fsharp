﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type RemoveRedundantAttributeTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantAttributeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRqaAttr"

    [<Test>] member x.``Attr - After 01``() = x.DoNamedTest()

    [<Test>] member x.``Attr - Before - 01``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Before - 02``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Before - 03``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Before - 04``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Before 05 - Semi on other line``() = x.DoNamedTest()

    [<Test>] member x.``Attr - Inside 01``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Inside 02``() = x.DoNamedTest()

    [<Test>] member x.``Attr - Invalid on this element 01``() = x.DoNamedTest()
    [<Test>] member x.``Attr - Invalid on this element 02``() = x.DoNamedTest()

    [<Test>] member x.``List 01``() = x.DoNamedTest()
    [<Test>] member x.``List 02 - Space after``() = x.DoNamedTest()
    [<Test>] member x.``List 03 - Indent``() = x.DoNamedTest()
    [<Test>] member x.``List 04``() = x.DoNamedTest()

    [<Test>] member x.``List - Comment 01``() = x.DoNamedTest()

    [<Test>] member x.``List - Inline 01``() = x.DoNamedTest()
    [<Test>] member x.``List - Inline 02 - In group``() = x.DoNamedTest()
    [<Test>] member x.``List - Inline - Other list 01``() = x.DoNamedTest()

    [<Test>] member x.``List - Other list - After 01``() = x.DoNamedTest()
    [<Test>] member x.``List - Other list - After 02 - Space``() = x.DoNamedTest()
    [<Test>] member x.``List - Other list - After 03``() = x.DoNamedTest()
    [<Test>] member x.``List - Other list - Before 01``() = x.DoNamedTest()

[<FSharpTest>]
type RemoveNeverMatchingRuleTest1() =
    inherit FSharpQuickFixTestBase<RemoveRedundantAttributeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantAttribute"

    [<Test>] member x.``Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Extension 02``() = x.DoNamedTest()
    [<Test>] member x.``Extension 03``() = x.DoNamedTest()
    [<Test>] member x.``Extension 04``() = x.DoNamedTest()
    [<Test>] member x.``Extension 05``() = x.DoNamedTest()
    [<Test>] member x.``Extension 06``() = x.DoNamedTest()
