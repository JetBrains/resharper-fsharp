namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Features.RegExp.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("System")>]
type RegexpHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/regexp"
    override x.CompilerIdsLanguage = ClrRegexLanguage.Instance :> _

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? RegExpSyntaxHighlighting
        | :? RegExpSyntaxError
        | :? RegExpHighlightingBase -> true
        | _ -> false

    [<Test>] member x.``Strings 01``() = x.DoNamedTest()
    [<Test>] member x.``Options 01``() = x.DoNamedTest()
    [<Test>] member x.``Errors 01``() = x.DoNamedTest()

    [<Test; TestNet50; TestPackages(JetBrainsAnnotationsPackage)>] member x.``Detection 01``() = x.DoNamedTest()

    [<Test>] member x.``Detection 02 - Regex type provider``() = x.DoNamedTest()
