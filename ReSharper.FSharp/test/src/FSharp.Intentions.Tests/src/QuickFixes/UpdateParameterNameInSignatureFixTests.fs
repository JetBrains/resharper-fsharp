namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateParameterNameInSignatureFixTests() =
    inherit FSharpQuickFixTestBase<UpdateParameterNameInSignatureFix>()
    override x.RelativeTestDataPath = "features/quickFixes/updateParameterNameInSignatureFixTests"

    [<Test>] member x.``First parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Middle parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Last parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``First in tupled parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Middle in tupled parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Last in tupled parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Attribute parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Optional parameter`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``First in tuple in member definition`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Last in last tuple in member definition`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Primary constructor`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Secondary constructor`` () = x.DoNamedTestWithSignature()
