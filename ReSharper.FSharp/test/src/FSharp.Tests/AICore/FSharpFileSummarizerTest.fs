namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.AICore

open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.AICore
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open JetBrains.Rider.Backend.Features.AICore.Summarization
open NUnit.Framework

[<FSharpTest>]
type FSharpFileSummarizerTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "ai/summarizer"

    [<Test>] member x.``Implementation file 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 02 - Top level module``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 03 - Unresolved type``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 04 - Type parameters``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 05 - Type parameters``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 06 - Inherit``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Implementation file 07 - Syntax error``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Signature file 01``() = x.DoNamedTestWithFsi()

    override x.DoTest(_: Lifetime, project: IProject) =
        let psiServices = x.Solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()
        let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
        let sourceFile = projectFile.ToSourceFiles().Single()

        x.ExecuteWithGold(fun writer ->
            let summarizer: IRiderFileSummarizer = FSharpFileSummarizer()
            let summary = summarizer.GetSummary(sourceFile, SummarizationFlavor.Junie)
            writer.Write(summary)) |> ignore

    member x.DoNamedTestWithFs() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + FSharpProjectFileType.FsExtension)

    member x.DoNamedTestWithFsi() =
        let testName = x.TestMethodName
        x.DoTestSolution(testName + FSharpSignatureProjectFileType.FsiExtension)
