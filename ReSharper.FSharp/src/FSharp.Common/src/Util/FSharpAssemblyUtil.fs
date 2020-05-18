[<Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil

open System.Collections.Generic
open JetBrains.Diagnostics
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Impl.Reflection2
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


type FSharpSignatureDataResource =
    { CompilationUnitName: string
      MetadataResource: IManifestResourceDisposition }

let [<Literal>] signatureInfoResourceName = "FSharpSignatureInfo."
let [<Literal>] signatureInfoResourceNameOld = "FSharpSignatureData."

let internal isSignatureDataResource (manifestResource: IMetadataManifestResource) (compilationUnitName: outref<string>) =
    let name = manifestResource.Name
    if startsWith signatureInfoResourceName name then
        compilationUnitName <- name.Substring(signatureInfoResourceName.Length)
        true
    elif startsWith signatureInfoResourceNameOld name then
        compilationUnitName <- name.Substring(signatureInfoResourceNameOld.Length)
        true
    else false

[<CompiledName("GetFSharpMetadataResources")>]
let getFSharpMetadataResources (psiModule: IPsiModule) =
    match psiModule.As<IAssemblyPsiModule>() with
    | null -> null
    | assemblyPsiModule ->

    let path = assemblyPsiModule.Assembly.Location
    if isNull path then null else

    Assertion.Assert(isFSharpAssembly psiModule, "isFSharpAssembly psiModule")

    use metadataLoader = new MetadataLoader()
    let metadataAssembly = metadataLoader.TryLoadFrom(path, JetFunc<_>.False)
    if isNull metadataAssembly then null else

    let resources = List()
    for manifestResource in metadataAssembly.GetManifestResources() do
        let mutable compilationUnitName = Unchecked.defaultof<_>
        if isSignatureDataResource manifestResource &compilationUnitName then
            let disposition = manifestResource.GetDisposition()
            if isNotNull disposition then
                resources.Add({ CompilationUnitName = compilationUnitName; MetadataResource = disposition })

    // todo: external metadata in FSharp.Core

    resources.AsReadOnly()


let getNestedTypes (declaredElement: IClrDeclaredElement) (symbolScope: ISymbolScope): ITypeElement seq =
    match declaredElement with
    | :? INamespace as ns -> ns.GetNestedTypeElements(symbolScope) :> _
    | :? ITypeElement as typeElement -> typeElement.NestedTypes :> _
    | _ -> Seq.empty

let rec getNestedAutoImportedModules (declaredElement: IClrDeclaredElement) (symbolScope: ISymbolScope) = seq {
    for typeElement in getNestedTypes declaredElement symbolScope do
        for attribute in getAutoOpenAttributes typeElement do
            if attribute.PositionParameterCount = 0 then
                yield typeElement
                yield! getNestedAutoImportedModules typeElement symbolScope }

let getAutoOpenModules (psiAssemblyFileLoader: IPsiAssemblyFileLoader) (assembly: IPsiAssembly) =
    let result = List()

    psiAssemblyFileLoader.GetOrLoadAssembly(assembly, true, fun psiAssembly assemblyFile metadataAssembly ->
        let attributesSet = assemblyFile.CreateAssemblyAttributes()
        let attributes = getAutoOpenAttributes attributesSet

        if attributes.IsEmpty() then () else

        let psiServices = psiAssembly.PsiModule.GetPsiServices()
        let symbolScope = psiServices.Symbols.GetSymbolScope(psiAssembly.PsiModule, false, true)

        for attribute in attributes do
            let moduleString = attribute.PositionParameter(0).ConstantValue.Value.As<string>()
            if moduleString.IsNullOrEmpty() then () else

            for declaredElement in symbolScope.GetElementsByQualifiedName(moduleString) do
                result.Add(declaredElement)
                result.AddRange(getNestedAutoImportedModules declaredElement symbolScope)) |> ignore

    result
