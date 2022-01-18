namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open NUnit.Framework

type SyntaxHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/syntaxHighlighting"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? ReSharperSyntaxHighlighting

    [<Test>] member x.``Inactive preprocessor branch 01``() = x.DoNamedTest()
