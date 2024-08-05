namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open FSharp.Compiler.AbstractIL.IL
open JetBrains.Application.Parts
open JetBrains.Application.changes
open JetBrains.Lifetimes
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util.DataStructures

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FcsModuleReaderCommonCache(lifetime: Lifetime, changeManager: ChangeManager) =
    inherit AssemblyReaderShimChangeListenerBase(lifetime, changeManager)

    let assemblyTypeRefs = ConcurrentDictionary<IPsiModule, ConcurrentDictionary<IClrTypeName, ILTypeRef>>()

    member this.GetOrCreateAssemblyTypeRefCache(targetModule: IPsiModule) =
        let mutable cache = Unchecked.defaultof<_>
        match assemblyTypeRefs.TryGetValue(targetModule, &cache) with
        | true -> cache
        | _ ->

        let cache = ConcurrentDictionary()
        assemblyTypeRefs[targetModule] <- cache
        cache

    member this.TryGetAssemblyTypeRef(psiModule: IPsiModule, clrTypeName: IClrTypeName, typeRef: byref<_>) =
        match assemblyTypeRefs.TryGetValue(psiModule) with
        | true, assemblyTypeRefs -> assemblyTypeRefs.TryGetValue(clrTypeName, &typeRef)
        | _ -> false

    member val AssemblyRefs = ConcurrentDictionary<AssemblyNameInfo, ILScopeRef>()
    member val LocalTypeRefs = ConcurrentDictionary<IClrTypeName, ILTypeRef>()

    member val Cultures = DataIntern<string option>()
    member val PublicKeys = DataIntern<PublicKey option>()
    member val LiteralValues = DataIntern<ILFieldInit option>()
    member val AttributeValues = DataIntern<ILAttribElem>()
