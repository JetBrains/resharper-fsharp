namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open NUnit.Framework

type XmlDocSyntaxHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/syntaxHighlighting/xmlDoc"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? ReSharperSyntaxHighlighting

    [<Test>] member x.``XmlDoc 01 - Simple summary doc``() = x.DoNamedTest()
    [<Test>] member x.``XmlDoc 02``() = x.DoNamedTest()
