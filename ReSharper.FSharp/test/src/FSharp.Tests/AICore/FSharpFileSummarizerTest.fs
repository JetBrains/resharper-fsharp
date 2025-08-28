namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.AICore

open JetBrains.Lifetimes
open JetBrains.ProjectModel
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

    [<Test>] member x.``Simple``() = x.DoNamedTest()

    override x.DoTest(_: Lifetime, project: IProject) =
        let psiServices = x.Solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()
        let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
        let sourceFile = projectFile.ToSourceFiles().Single()

        x.ExecuteWithGold(fun writer ->
            let summarizer: IRiderFileSummarizer = FSharpFileSummarizer()
            let summary = summarizer.GetSummary(sourceFile, SummarizationFlavor.Junie)
            writer.Write(summary)) |> ignore
