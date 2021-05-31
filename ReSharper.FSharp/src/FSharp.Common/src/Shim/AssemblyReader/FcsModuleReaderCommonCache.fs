namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open FSharp.Compiler.AbstractIL.IL
open JetBrains.Application.changes
open JetBrains.Lifetimes
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.Util.DataStructures

[<SolutionComponent>]
type FcsModuleReaderCommonCache(lifetime: Lifetime, changeManager: ChangeManager) =
    inherit AssemblyReaderShimChangeListenerBase(lifetime, changeManager)

    let assemblyRefs = ConcurrentDictionary<AssemblyNameInfo, ILScopeRef>()

    member val Cultures = DataIntern<string option>()
    member val PublicKeys = DataIntern<PublicKey option>()
    member val LiteralValues = DataIntern<ILFieldInit option>()
