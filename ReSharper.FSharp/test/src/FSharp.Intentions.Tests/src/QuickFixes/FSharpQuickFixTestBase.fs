﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open System
open System.IO
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type NoHighlightingFoundAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NoHighlightingsFoundError)

type NotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NotAvailable)

type ActionNotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.ActionNotAvailable)

type DumpPsiTreeAttribute() =
    inherit Attribute()

[<AbstractClass; FSharpTest>]
type FSharpQuickFixTestBase<'T when 'T :> IQuickFix>() =
    inherit QuickFixTestBase<'T>()

    override x.OnQuickFixNotAvailable(_, _) = Assert.Fail(ErrorText.NotAvailable)

    override x.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile) =
        let occurrenceName = BaseTestWithTextControl.GetSetting(textControl, FSharpTestPopup.OccurrenceName)
        FSharpTestPopup.setOccurrence occurrenceName true x.Solution x.TestLifetime

        base.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile)

    override this.DumpTextControl(textControl) =
        if this.GetAttributes<DumpPsiTreeAttribute>() |> Seq.isEmpty then
            base.DumpTextControl(textControl) else

        let dumpTextControl = base.DumpTextControl(textControl)
        let solution = this.Solution

        Action<TextWriter>(fun writer ->
            dumpTextControl.Invoke(writer)
            writer.WriteLine("---------------------------------------------------------\n")

            textControl.TryGetSourceFiles(solution)
            |> Seq.tryExactlyOne
            |> Option.map _.GetPrimaryPsiFile()
            |> Option.iter (fun psiFile -> DebugUtil.DumpPsi(writer, psiFile))
        )

    override this.CheckAllFiles = true

    member this.DoNamedTestWithSignature() =
        let testName = this.TestMethodName
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        this.DoTestSolution(testName + fsiExt, testName + fsExt)

    member this.DoNamedTestWithTwoFiles() =
        let testName = this.TestMethodName
        let firstName = testName + " - 1"
        let secondName = testName + " - 2"
        let ext = FSharpProjectFileType.FsExtension
        this.DoTestSolution(firstName + ext, secondName + ext)

    member this.DoNamedTestWithSignatureAndSecondFile() =
        let testName = this.TestMethodName
        let firstName = testName + " - 1"
        let secondName = testName + " - 2"
        let fsExt = FSharpProjectFileType.FsExtension
        let fsiExt = FSharpSignatureProjectFileType.FsiExtension
        this.DoTestSolution(firstName + fsiExt, firstName + fsExt, secondName + fsExt)
