namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open FSharp.Compiler.AbstractIL.IL
open JetBrains.Application.changes
open JetBrains.Lifetimes
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp

[<SolutionComponent>]
type FcsModuleReaderCommonCache(lifetime: Lifetime, changeManager: ChangeManager) =
    inherit AssemblyReaderShimChangeListenerBase(lifetime, changeManager)

    let assemblyRefs = ConcurrentDictionary<AssemblyNameInfo, ILScopeRef>()
