namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveSubsequentTest() =
    inherit FSharpQuickFixTestBase<RemoveSubsequentFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeSubsequent"

    [<Test>] member x.``Remove expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Remove expr 02 - Not the first``() = x.DoNamedTest()
    [<Test>] member x.``Remove expr 03 - Semicolon``() = x.DoNamedTest()

    [<Test>] member x.``Replace seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 02 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 03 - Comment on new line``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 04 - Semicolon``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 05 - Semicolon and space``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 06 - Semicolon and new line``() = x.DoNamedTest()
    [<Test>] member x.``Replace seq 07 - Comment and semicolon``() = x.DoNamedTest()
