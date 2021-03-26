namespace rec JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Generic
open System.Linq
open System.Collections.Concurrent
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

module ProjectFcsModuleReader =
    let isDll (project: IProject) (targetFrameworkId: TargetFrameworkId) =
        let projectProperties = project.ProjectProperties
        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> cfg.OutputType = ProjectOutputType.LIBRARY
        | _ -> false

    module DummyValues =
        let subsystemVersion = 4, 0
        let useHighEntropyVA = false
        let hashalg = None
        let locale = None
        let flags = 0
        let exportedTypes = mkILExportedTypes []
        let metadataVersion = String.Empty


type ProjectFcsModuleReader(psiModule: IPsiModule, cache: FcsModuleReaderCommonCache) =
    let symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, false, true)

    let mutable moduleDef: ILModuleDef option = None

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, ILTypeDef>()

    member x.Timestamp = timestamp
    member x.PsiModule = psiModule

    interface ILModuleReader with
        member this.ILModuleDef =
            match moduleDef with
            | Some moduleDef -> moduleDef
            | None ->

            use readLockCookie = ReadLockCookie.Create()

            let project = psiModule.ContainingProjectModule :?> IProject
            let moduleName = project.Name
            let assemblyName = project.GetOutputAssemblyName(psiModule.TargetFrameworkId)
            let isDll = ProjectFcsModuleReader.isDll project psiModule.TargetFrameworkId

            let typeDefs =
                let result = List<ILPreTypeDef>()

                let rec addTypes (ns: INamespace) =
                    for typeElement in ns.GetNestedTypeElements(symbolScope) do
                        let clrTypeName = typeElement.GetClrName().GetPersistent() // todo: intern
                        result.Add(PreTypeDef(clrTypeName, this))
                    for nestedNs in ns.GetNestedNamespaces(symbolScope) do
                        addTypes nestedNs

                addTypes symbolScope.GlobalNamespace

                let preTypeDefs = result.ToArray()
                mkILTypeDefsComputed (fun _ -> preTypeDefs)

            // todo: add internals visible to test
            let flags = 0 // todo
            let exportedTypes = mkILExportedTypes []

            let newModuleDef =
                mkILSimpleModule
                    assemblyName moduleName isDll
                    ProjectFcsModuleReader.DummyValues.subsystemVersion
                    ProjectFcsModuleReader.DummyValues.useHighEntropyVA
                    typeDefs
                    None None flags exportedTypes
                    ProjectFcsModuleReader.DummyValues.metadataVersion

            moduleDef <- Some newModuleDef
            newModuleDef


        member this.Dispose() = ()
        member this.ILAssemblyRefs = []


module PreTypeDef =
    let typeParameterCountStrings = [| "`0"; "`1"; "`2"; "`3"; "`4"; "`5"; "`6"; "`7" |]
    let typeParameterCountStringsCount = typeParameterCountStrings.Length

    let mkTypeName (name: string) (paramsCount: int) =
        if paramsCount = 0 then name else
        let paramsCountString =
            if paramsCount >= typeParameterCountStringsCount then paramsCount.ToString() else
            typeParameterCountStrings.[paramsCount]
        name + paramsCountString

    let mkNameFromTypeNameAndParamsNumber (nameAndParametersCount: TypeNameAndTypeParameterNumber) =
        mkTypeName nameAndParametersCount.TypeName nameAndParametersCount.TypeParametersNumber

    let mkNameFromClrTypeName (clrTypeName: IClrTypeName) =
        mkTypeName clrTypeName.ShortName clrTypeName.TypeParametersCount


type PreTypeDef(clrTypeName: IClrTypeName, reader: ProjectFcsModuleReader) =
    member x.Name =
        let typeName = clrTypeName.TypeNames.Last() // todo: use clrTypeName.ShortName ? (check type params)
        PreTypeDef.mkNameFromTypeNameAndParamsNumber typeName

    interface ILPreTypeDef with
        member x.Name = x.Name

        member x.Namespace =
            if not (clrTypeName.TypeNames.IsSingle()) then [] else
            clrTypeName.NamespaceNames |> List.ofSeq

        member x.GetTypeDef() =
            ILTypeDef(clrTypeName.ShortName, enum 0, ILTypeDefLayout.Auto, [], [], None, emptyILMethods,
                emptyILTypeDefs, emptyILFields, emptyILMethodImpls, emptyILEvents, emptyILProperties,
                emptyILSecurityDecls, emptyILCustomAttrs)
