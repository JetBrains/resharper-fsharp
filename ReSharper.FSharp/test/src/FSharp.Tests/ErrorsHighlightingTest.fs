namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ErrorsHighlightingTest() =
    inherit HighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/errorsHighlighting"

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _

    [<Test>] member x.``Empty file``() = x.DoNamedTest()
    [<Test>] member x.``No errors 01``() = x.DoNamedTest()

    [<Test>] member x.``Syntax errors 01``() = x.DoNamedTest()
    [<Test>] member x.``Syntax errors 02``() = x.DoNamedTest()

    [<Test>] member x.``Type check errors 01 - type mismatch``() = x.DoNamedTest()
    [<Test>] member x.``Type check errors 02 - nested error``() = x.DoNamedTest()
