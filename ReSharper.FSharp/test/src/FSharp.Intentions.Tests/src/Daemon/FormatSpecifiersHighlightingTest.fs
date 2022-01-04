namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Daemon.Impl
open NUnit.Framework

type FormatSpecifiersHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/formatSpecifiersHighlighting"

    override x.HighlightingPredicate(highlighting, _, _) = highlighting :? FormatStringItemHighlighting

    [<Test>] member x.``Bindings``() = x.DoNamedTest()
    [<Test>] member x.``Try with finally``() = x.DoNamedTest()
    [<Test>] member x.``Record and union members``() = x.DoNamedTest()
    [<Test>] member x.``Escaped strings``() = x.DoNamedTest()
    [<Test>] member x.``Triple quoted strings``() = x.DoNamedTest()
    [<Test>] member x.``Multi line strings``() = x.DoNamedTest()
    [<Test>] member x.``Malformed formatters``() = x.DoNamedTest()
    [<Test>] member x.``kprintf bprintf fprintf``() = x.DoNamedTest()
    [<Test>] member x.``Plane strings``() = x.DoNamedTest()
    [<Test>] member x.``Extensions``() = x.DoNamedTest()
