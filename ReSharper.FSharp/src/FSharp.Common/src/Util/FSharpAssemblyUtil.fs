[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil

open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<CompiledName("InterfaceDataVersionAttrTypeName")>]
let interfaceDataVersionAttrTypeName = clrTypeName "Microsoft.FSharp.Core.FSharpInterfaceDataVersionAttribute"

let isFSharpAssemblyKey = Key("IsFSharpAssembly")

[<Extension; CompiledName("IsFSharpAssembly")>]
let isFSharpAssembly (psiModule: IPsiModule) =
    match psiModule.ContainingProjectModule with
    | :? IProject -> false
    | _ ->

    match psiModule.GetData(isFSharpAssemblyKey) with
    | null ->
        use cookie = ReadLockCookie.Create()
        let attrs = psiModule.GetPsiServices().Symbols.GetModuleAttributes(psiModule)
        let isFSharpAssembly = attrs.HasAttributeInstance(interfaceDataVersionAttrTypeName, false)

        psiModule.PutData(isFSharpAssemblyKey, if isFSharpAssembly then BooleanBoxes.True else BooleanBoxes.False)
        isFSharpAssembly

    | value -> value == BooleanBoxes.True

[<Extension; CompiledName("IsFromFSharpAssembly")>]
let isFromFSharpAssembly (declaredElement: IClrDeclaredElement) =
    isFSharpAssembly declaredElement.Module


let [<Literal>] FSharpCore = "FSharp.Core"

let isFSharpCore (assemblyName: AssemblyNameInfo) =
    isNotNull assemblyName && AssemblyNameInfo.SimpleNameComparer.Equals(FSharpCore, assemblyName.Name)
