namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Features.RegExp.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open JetBrains.ReSharper.Psi.RegExp.ClrRegex
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestReferences("System")>]
type RegexpHighlightingTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/regexp"
    override this.CompilerIdsLanguage = ClrRegexLanguage.Instance :> _

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? RegExpSyntaxHighlighting
        | :? RegExpSyntaxError
        | :? RegExpHighlightingBase -> true
        | _ -> false

    [<Test>] member x.``Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Options - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Options - Literal 01``() = x.DoNamedTest()
    [<Test>] member x.``Options 01 - Empty``() = x.DoNamedTest()
