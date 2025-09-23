namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.AI

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

    [<Test>] member x.``Namespaces 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Modules 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Modules 02 - Top-level``() = x.DoNamedTestWithFs()

    [<Test>] member x.``Let bindings 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Let bindings 02 - Signatures``() = x.DoNamedTestWithFsi()

    [<Test>] member x.``Types 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Inherit 01``() = x.DoNamedTestWithFs()

    [<Test>] member x.``Members 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Members 02 - Interface impls``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Members 03 - Signatures``() = x.DoNamedTestWithFsi()

    [<Test>] member x.``Nested scopes 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Unresolved type 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Type parameters 01``() = x.DoNamedTestWithFs()
    [<Test>] member x.``Syntax error 01``() = x.DoNamedTestWithFs()

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
