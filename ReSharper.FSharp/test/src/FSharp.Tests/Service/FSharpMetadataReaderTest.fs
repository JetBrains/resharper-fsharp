namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; AbstractClass>]
type FSharpReferencedAssemblyTestBase() =
    inherit BaseTestWithSingleProject()

    abstract DoTest: assemblyModule: IPsiModule -> unit

    member x.DoTest(moduleName: string) =
        x.WithSingleProject([], fun lifetime solution (project: IProject) ->
            let modules = project.GetSolution().PsiModules().GetModules()
            match modules |> Seq.tryFind (fun m -> m.Name = moduleName) with
            | None -> failwith "Could not get module"
            | Some psiModule -> x.DoTest(psiModule) )


type FSharpMetadataReaderTest() =
    inherit FSharpReferencedAssemblyTestBase()

    override x.RelativeTestDataPath = "common/metadataReader"

    override x.DoTest(assemblyModule: IPsiModule) =
        FSharpMetadataReader.ReadMetadata(assemblyModule)

    [<Test; TestPackages(FSharpCorePackage)>]
    member x.FSharpCore() = x.DoTest("FSharp.Core")

    [<Test; TestReferences("TypeInGlobalNamespace.dll")>]
    member x.``Type in global namespace``() = x.DoTest("TypeInGlobalNamespace")
