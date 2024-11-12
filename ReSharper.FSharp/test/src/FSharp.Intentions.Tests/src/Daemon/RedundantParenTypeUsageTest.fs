﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open NUnit.Framework

[<FSharpTest>]
type RedundantParenTypeUsageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantParens/typeUsage"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantParenTypeUsageWarning

    [<Test>] member x.``Array 01``() = x.DoNamedTest()
    [<Test>] member x.``Array 02 - Patterns``() = x.DoNamedTest()
    [<Test>] member x.``Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function 02 - IsInstPat``() = x.DoNamedTest()
    [<Test>] member x.``Function 03 - Abstract member``() = x.DoNamedTest()

    [<FSharpSignatureTest>]
    [<Test>] member x.``Function 04 - Signature``() = x.DoNamedTest()

    [<Test>] member x.``Function 05 - Case field``() = x.DoNamedTest()
    [<Test>] member x.``Function 06 - Val field``() = x.DoNamedTest()

    [<Test>] member x.``Nullable 01``() = x.DoNamedTest()

    [<Test>] member x.``Parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Paren 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Case field``() = x.DoNamedTest()

    [<FSharpSignatureTest>]
    [<Test>] member x.``Tuple 03 - Signature``() = x.DoNamedTest() // todo: return attribute

    [<Test>] member x.``Tuple 04 - Patterns``() = x.DoNamedTest()

    [<FSharpSignatureTest>]
    [<Test>] member x.``Tuple 05 - Named parameter sig``() = x.DoNamedTest()

    [<Test>] member x.``As pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Trait call 01``() = x.DoNamedTest()
