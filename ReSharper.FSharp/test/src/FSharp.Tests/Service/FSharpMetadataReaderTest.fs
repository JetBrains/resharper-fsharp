namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.Diagnostics
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.TestFramework
open JetBrains.Util
open NUnit.Framework

[<FSharpTest; AbstractClass>]
type FSharpReferencedAssemblyTestBase() =
    inherit BaseTestWithSingleProject()

    abstract DoTest: assemblyModule: IAssemblyPsiModule -> unit

    member x.DoTest(moduleName: string) =
        x.WithSingleProject([], fun lifetime solution (project: IProject) ->
            let modules = project.GetSolution().PsiModules().GetModules()
            let assemblyPsiModule =
                modules
                |> Seq.tryPick (fun psiModule ->
                    match psiModule with
                    | :? IAssemblyPsiModule as assemblyPsiModule when psiModule.Name = moduleName ->
                        Some(assemblyPsiModule)
                    | _ -> None)
                |> Option.toObj

            x.DoTest(assemblyPsiModule.NotNull()))


type FSharpMetadataReaderTest() =
    inherit FSharpReferencedAssemblyTestBase()

    override x.RelativeTestDataPath = "common/metadataReader"

    override x.DoTest(assemblyPsiModule: IAssemblyPsiModule) =
        use metadataLoader = new MetadataLoader()

        let path = assemblyPsiModule.NotNull().Assembly.Location.NotNull()
        let metadataAssembly = metadataLoader.LoadFrom(path, JetFunc<_>.False).NotNull()

        FSharpMetadataReader.ReadMetadata(assemblyPsiModule, metadataAssembly) |> ignore

    [<Test>]
    member x.FSharpCore() = x.DoTest("FSharp.Core")

    [<Test; TestPackages(PackageReferences.FsPickler)>]
    member x.FsPickler() = x.DoTest("FSharp.Core")

    [<Test; TestReferences("TypeInGlobalNamespace.dll")>]
    member x.``Type in global namespace``() = x.DoTest("TypeInGlobalNamespace")
