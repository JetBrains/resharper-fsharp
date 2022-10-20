namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateRecordFieldsInSignatureFixTest() =
    inherit FSharpQuickFixTestBase<UpdateRecordFieldsInSignatureFix>()
    override x.RelativeTestDataPath = "features/quickFixes/updateRecordFieldsInSignatureFix"
    
    [<Test>] member x.``Single missing field`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Multiple missing fields`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Invalid order of fields - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Extra field in signature - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Generic argument - 01`` () = x.DoNamedTestWithSignature()
