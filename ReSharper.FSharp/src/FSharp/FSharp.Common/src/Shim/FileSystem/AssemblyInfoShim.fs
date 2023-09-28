namespace JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem

open JetBrains.Application.Infra
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Util

[<SolutionComponent>]
type AssemblyInfoShim(lifetime: Lifetime, fsSourceCache: FSharpSourceCache,
        assemblyExistsService: AssemblyExistsService, toolset: ISolutionToolset) =
    inherit DelegatingFileSystemShim(lifetime)

    // The shim is injected to get the expected shim shadowing chain, it's expected to be unused.
    do fsSourceCache |> ignore

    let isSupported (path: VirtualFileSystemPath) =
        if not path.IsAbsolute then false else

        let extension = path.ExtensionNoDot
        extension = "dll" || extension = "exe"

    override x.GetLastWriteTime(path) =
        if isSupported path then assemblyExistsService.GetFileSystemData(path).LastWriteTimeUtc
        else base.GetLastWriteTime(path)

    override x.ExistsFile(path) =
        if isSupported path then assemblyExistsService.GetFileSystemData(path).FileExists else
        base.ExistsFile(path)

    override x.IsStableFile(path) =
        match toolset.GetBuildTool() with
        | null -> base.IsStableFile(path)
        | buildTool -> buildTool.Directory.IsPrefixOf(path)
