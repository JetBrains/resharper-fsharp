namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateRecordFieldTypeInSignatureFixTest() =
    inherit FSharpQuickFixTestBase<UpdateRecordFieldTypeInSignatureFix>()
    override x.RelativeTestDataPath = "features/quickFixes/updateRecordFieldTypeInSignatureFix"
    override this.CheckAllFiles = true

    member x.DoNamedTestWithSignature() =
        let testName = x.TestMethodName
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        x.DoTestSolution(testName + fsiExt, testName + fsExt)

    [<Test>] member x.``Wrong Record Field Type - 01`` () = x.DoNamedTestWithSignature()
