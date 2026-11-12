namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<AssertCorrectTreeStructure>]
type AddMissingNewFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingNewFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingNew"

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02 - Space``() = x.DoNamedTest()
    [<Test>] member x.``App 03 - Type args``() = x.DoNamedTest()
    [<Test>] member x.``App 04 - Qualified name``() = x.DoNamedTest()
    [<Test>] member x.``App 05 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``App 06 - Multiline, aligned``() = x.DoNamedTest()
    [<Test>] member x.``Arg 01 - Const``() = x.DoNamedTest()
    [<Test>] member x.``Arg 02 - Reference``() = x.DoNamedTest()
    [<Test>] member x.``Arg 03 - Qualified``() = x.DoNamedTest()
    [<Test>] member x.``Context 01 - Pipe``() = x.DoNamedTest()
    [<Test>] member x.``Context 02 - Upcast``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier 01``() = x.DoNamedTest()
    [<Test>] member x.``Qualifier 02 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Not available 01 - No arg``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Type args, no arg``() = x.DoNamedTest()
