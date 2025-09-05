namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddMissingSeqFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingSeqFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingSeqFix"

    [<Test>] member x.``FS0740 Adds missing seq before sequential`` () = x.DoNamedTest()
    [<Test>] member x.``FS0740 Adds parens when needed app`` () = x.DoNamedTest()
    [<Test>] member x.``FS0740 Adds parens when needed dot`` () = x.DoNamedTest()
    [<Test>] member x.``FS0740 Adds parens when needed multiline`` () = x.DoNamedTest()
    [<Test>] member x.``FS3873 Adds missing seq before start to finish`` () = x.DoNamedTest()
    [<Test>] member x.``FS3873 Adds parens when needed app`` () = x.DoNamedTest()
    [<Test>] member x.``FS3873 Adds parens when needed dot`` () = x.DoNamedTest()
    [<Test>] member x.``FS3873 Adds parens when needed multiline`` () = x.DoNamedTest()
