namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type ReplaceWithRegularStringFixTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithRegularStringFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithRegularStringFix"

    [<Test>] member x.``01 - String``() = x.DoNamedTest()
    [<Test>] member x.``02 - Verbatim string 01``() = x.DoNamedTest()
    [<Test>] member x.``03 - Verbatim string 02``() = x.DoNamedTest()
    [<Test>] member x.``04 - Triple quoted string``() = x.DoNamedTest()
