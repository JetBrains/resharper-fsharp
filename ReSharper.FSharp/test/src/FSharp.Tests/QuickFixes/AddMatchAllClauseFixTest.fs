namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages("FSharp.Core")>]
type AddMatchAllClauseFixTest() =
    inherit QuickFixTestBase<AddMatchAllClauseFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMatchAllClause"

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Indent``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03 - Multiple clauses``() = x.DoNamedTest()
    [<Test>] member x.``Simple 04 - Generate single line``() = x.DoNamedTest()
