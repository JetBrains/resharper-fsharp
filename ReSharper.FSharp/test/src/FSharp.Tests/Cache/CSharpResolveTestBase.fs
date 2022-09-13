namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open System.IO
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.TestFramework

[<TestPackages(FSharpCorePackage); AbstractClass>]
type CSharpResolveTestBase(fileExtension) =
    inherit TestWithTwoProjectsBase(CSharpProjectFileType.CS_EXTENSION, fileExtension)
    let highlightingManager = HighlightingSettingsManager.Instance

    override x.RelativeTestDataPath = "cache/csharpResolve"

    override x.DoTest(project: IProject, _: IProject) =
        x.Solution.GetPsiServices().Files.CommitAllDocuments()
        x.ExecuteWithGold(fun writer ->
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()
            let psiFile = sourceFile.GetPrimaryPsiFile()

            let daemon = TestHighlightingDumper(sourceFile, writer, null, Func<_,_,_,_>(x.ShouldHighlight))
            daemon.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT)
            daemon.Dump()

            let referenceProcessor = RecursiveReferenceProcessor(fun r -> x.ProcessReference(r, writer))
            psiFile.ProcessThisAndDescendants(referenceProcessor)) |> ignore

    member x.ShouldHighlight highlighting sourceFile settings =
        let severity = highlightingManager.GetSeverity(highlighting, sourceFile, x.Solution, settings)
        severity = Severity.ERROR

    member x.ProcessReference(reference: IReference, writer: TextWriter) =
        match reference.Resolve().DeclaredElement with
        | :? IFSharpTypeMember as typeMember -> writer.WriteLine(typeMember.XMLDocId)
        | _ -> ()
