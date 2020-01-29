namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest>]
type FSharpMetadataReaderTest() =
    inherit BaseTestWithSingleProject()

    override x.RelativeTestDataPath = "common/metadataReader"
    
    member x.DoTest(moduleName: string) =
        x.WithSingleProject([], fun lifetime solution (project: IProject) ->
            let modules = project.GetSolution().PsiModules().GetModules()
            match modules |> Seq.tryFind (fun m -> m.Name = moduleName) with
            | None -> failwith "Could not get module"
            | Some psiModule -> FSharpMetadataReader.ReadMetadata(psiModule) )

    [<Test; TestPackages("FSharp.Core")>]
    member x.FSharpCore() = x.DoTest("FSharp.Core")

    [<Test; TestReferences("TypeInGlobalNamespace.dll")>]
    member x.``Type in global namespace``() = x.DoTest("TypeInGlobalNamespace")
