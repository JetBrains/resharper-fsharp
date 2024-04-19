namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open System
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.Util
open NUnit.Framework

[<SolutionComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type TestModulePathProvider(shim: TestAssemblyReaderShim, moduleReferencesResolveStore: IModuleReferencesResolveStore) =
    inherit ModulePathProvider(moduleReferencesResolveStore)

    override this.GetModulePath(moduleReference) =
        match moduleReference.ResolveResult(moduleReferencesResolveStore) with
        | :? IProject as project when project == shim.ReferencedProject -> Some(shim.Path)
        | _ -> base.GetModulePath(moduleReference)

    interface IHideImplementation<ModulePathProvider>

[<AbstractClass; FSharpTest; FSharpExperimentalFeature(ExperimentalFeature.AssemblyReaderShim)>]
type AssemblyReaderTestBase(mainFileExtension: string, secondFileExtension: string) =
    inherit TestWithTwoProjectsBase(mainFileExtension, secondFileExtension)

    override this.RelativeTestDataPath = "common/assemblyReaderShim"

     // todo: test InternalsVisibleTo

    override this.DoTest(_: Lifetime, project: IProject) =
        let solution = this.Solution
        let manager = HighlightingSettingsManager.Instance

        solution.GetPsiServices().Files.CommitAllDocuments()

        this.ExecuteWithGold(fun writer ->
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()

            let daemon = {
                new TestHighlightingDumper(sourceFile, writer, fun highlighting sourceFile settingsStore ->
                  let severity = manager.GetSeverity(highlighting, sourceFile, solution, settingsStore)
                  severity = Severity.WARNING || severity = Severity.ERROR)
                with
                  override this.ShouldRunStage stage = stage :? TypeCheckErrorsStage
                }

            daemon.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT)
            daemon.Dump()) |> ignore

type AssemblyReaderCSharpTest() =
    inherit AssemblyReaderTestBase(FSharpProjectFileType.FsExtension, CSharpProjectFileType.CS_EXTENSION)

    override this.RelativeTestDataPath = "common/assemblyReaderShim"

    [<Test>] member x.``Attribute - Attribute usage 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute - Attribute usage 02``() = x.DoNamedTest()
    [<Test>] member x.``Attribute - Obsolete 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 03``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 04``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 05``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 06``() = x.DoNamedTest()

    [<Test>] member x.``Event - Same name 01``() = x.DoNamedTest()
    [<Test>] member x.``Event - Same name 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Event 01``() = x.DoNamedTest()
    [<Test>] member x.``Event 02``() = x.DoNamedTest()
    [<Test>] member x.``Event 03``() = x.DoNamedTest()

    [<Test>] member x.``Field - Const 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Const 02 - Wrong type``() = x.DoNamedTest()
    [<Test>] member x.``Field - Const 03 - Same name``() = x.DoNamedTest()
    [<Test>] member x.``Field - Same name 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Same name 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Field 02 - Inherit``() = x.DoNamedTest()

    [<Test>] member x.``Method - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Ctor 02 - Param array``() = x.DoNamedTest()
    [<Test>] member x.``Method - Ctor 03 - Optional param``() = x.DoNamedTest()
    [<Test>] member x.``Method - Duplicate 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Duplicate 02 - Visibility``() = x.DoNamedTest()
    [<Test>] member x.``Method - Explicit impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Explicit impl 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Method - Extension 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Extern 01``() = x.DoNamedTest()

    [<Test; Explicit "Can't reference attribute in net451">]
    member x.``Method - Param 01``() = x.DoNamedTest()

    [<Test>] member x.``Property - Implementation 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Duplicate 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Explicit impl 01``() = x.DoNamedTest()
    [<Test>] member x.``Property - Explicit impl 03 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Property 02 - Accessibility``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Class 02 - Nested``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Type def - Class 03 - Abstract``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Class 04 - Interface impl``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Delegate 01``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Enum 02``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Interface 02 - Super``() = x.DoNamedTest() // todo: members, type parameters

    [<Test>] member x.``Type def - Namespace 01``() = x.DoNamedTest()

    [<Test>] member x.``Type parameter 01``() = x.DoNamedTest()


type AssemblyReaderVbTest() =
    inherit AssemblyReaderTestBase(FSharpProjectFileType.FsExtension, VBProjectFileType.VB_EXTENSION)

    [<Test>] member x.``Property - Explicit impl 02 - Vb``() = x.DoNamedTest()
