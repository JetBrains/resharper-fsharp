[<Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil

open JetBrains.Application.Threading
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open JetBrains.Util.dataStructures

[<CompiledName("InterfaceDataVersionAttrTypeName")>]
let interfaceDataVersionAttrTypeName = clrTypeName "Microsoft.FSharp.Core.FSharpInterfaceDataVersionAttribute"

[<CompiledName("InterfaceDataVersionAttrConcatTypeName")>]
let interfaceDataVersionAttrConcatTypeName =
    StringDotConcat("Microsoft.FSharp.Core", "FSharpInterfaceDataVersionAttribute");

let isFSharpAssemblyKey = Key("IsFSharpAssembly")


[<CompiledName("GetFSharpCoreSigdataPath")>]
let getFSharpCoreSigdataPath (assembly: IMetadataAssembly) =
    match assembly.Location with
    | null -> VirtualFileSystemPath.GetEmptyPathFor(assembly.Location.Context)
    | path ->

    match path.AssemblyPhysicalPath with
    | null -> VirtualFileSystemPath.GetEmptyPathFor(assembly.Location.Context)
    | path -> path.ChangeExtension("sigdata")


/// Shouldn't be used during an assembly load, as the assembly attributes aren't populated yet.
[<Extension; CompiledName("IsFSharpAssembly")>]
let isFSharpAssembly (psiModule: IPsiModule) =
    match psiModule.ContainingProjectModule with
    | :? IProject -> false
    | _ ->

    let value =
        psiModule.GetOrCreateDataUnderLock(isFSharpAssemblyKey, fun _ ->
            let psiServices = psiModule.GetPsiServices()

            psiServices.Locks.AssertReadAccessAllowed()
            let attrs = psiServices.Symbols.GetModuleAttributes(psiModule)
            let isFSharpAssembly = attrs.HasAttributeInstance(interfaceDataVersionAttrTypeName, false)
            if isFSharpAssembly then BooleanBoxes.True else BooleanBoxes.False
        )

    value == BooleanBoxes.True

[<Extension; CompiledName("IsFromFSharpAssembly")>]
let isFromFSharpAssembly (declaredElement: IClrDeclaredElement) =
    isFSharpAssembly declaredElement.Module


let [<Literal>] FSharpCore = "FSharp.Core"

[<Extension; CompiledName("IsFSharpCore")>]
let isFSharpCore (assemblyName: AssemblyNameInfo) =
    isNotNull assemblyName && AssemblyNameInfo.SimpleNameComparer.Equals(FSharpCore, assemblyName.Name)


let [<Literal>] SignatureInfoResourceName = "FSharpSignatureInfo."
let [<Literal>] SignatureInfoResourceNameOld = "FSharpSignatureData."
let [<Literal>] CompressedSignatureInfoResourceName = "FSharpSignatureCompressedData."

[<Extension; CompiledName("IsFSharpMetadataResource")>]
let isFSharpMetadataResource (manifestResource: IMetadataManifestResource) (compilationUnitName: outref<string>) =
    let name = manifestResource.Name
    if startsWith SignatureInfoResourceName name then
        compilationUnitName <- name.Substring(SignatureInfoResourceName.Length)
        true
    elif startsWith SignatureInfoResourceNameOld name then
        compilationUnitName <- name.Substring(SignatureInfoResourceNameOld.Length)
        true
    elif startsWith CompressedSignatureInfoResourceName name then
        compilationUnitName <- name.Substring(CompressedSignatureInfoResourceName.Length)
        true
    else false
