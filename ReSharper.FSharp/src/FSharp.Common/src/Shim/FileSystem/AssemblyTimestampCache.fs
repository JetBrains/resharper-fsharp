namespace JetBrains.ReSharper.Plugins.FSharp.Common.Shim.FileSystem

open JetBrains.Application.Infra
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.Util

[<SolutionComponent>]
type AssemblyTimestampCache
        (lifetime: Lifetime, fsSourceCache: FSharpSourceCache, assemblyExistsService: AssemblyExistsService) =
    inherit DelegatingFileSystemShim(Lifetimes.Define(lifetime).Lifetime, fsSourceCache)

    let isSupported (path: FileSystemPath) =
        let extension = path.ExtensionNoDot
        extension = "dll" || extension = "exe"

    override x.GetLastWriteTime(path) =
        if isSupported path then assemblyExistsService.GetFileSystemData(path).LastWriteTimeUtc
        else base.GetLastWriteTime(path)
        
    override x.Exists(path) =
        if isSupported path then assemblyExistsService.GetFileSystemData(path).FileExists else
        base.Exists(path)
        
