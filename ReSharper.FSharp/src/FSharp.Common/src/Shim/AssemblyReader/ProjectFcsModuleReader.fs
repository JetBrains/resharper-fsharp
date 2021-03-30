namespace rec JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Generic
open System.Linq
open System.Collections.Concurrent
open System.Reflection
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds

module ProjectFcsModuleReader =
    // todo: store in reader/cache, so it doesn't leak after solution close
    let cultures = DataIntern()
    let publicKeys = DataIntern()
//    let literalValues = DataIntern()

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

    let mkDummyTypeDef (name: string) =
        let attributes = enum 0
        let layout = ILTypeDefLayout.Auto
        let implements = []
        let genericParams = []
        let extends = None
        let nestedTypes = emptyILTypeDefs

        ILTypeDef(
             name, attributes, layout, implements, genericParams, extends, emptyILMethods, nestedTypes,
             emptyILFields, emptyILMethodImpls, emptyILEvents, emptyILProperties, emptyILSecurityDecls,
             emptyILCustomAttrs)

    let mkTypeAccessRights (typeElement: ITypeElement): TypeAttributes =
        let accessRightsOwner = typeElement.As<IAccessRightsOwner>()
        if isNull accessRightsOwner then enum 0 else

        let accessRights = accessRightsOwner.GetAccessRights()
        if isNull (typeElement.GetContainingType()) then
            match accessRights with
            | AccessRights.PUBLIC -> TypeAttributes.Public
            | _ -> enum 0
        else
            match accessRights with
            | AccessRights.PUBLIC -> TypeAttributes.NestedPublic
            | AccessRights.INTERNAL -> TypeAttributes.NestedAssembly
            | AccessRights.PROTECTED -> TypeAttributes.NestedFamily
            | AccessRights.PROTECTED_OR_INTERNAL -> TypeAttributes.NestedFamORAssem
            | AccessRights.PROTECTED_AND_INTERNAL -> TypeAttributes.NestedFamANDAssem
            | AccessRights.PRIVATE -> TypeAttributes.NestedPrivate
            | _ -> TypeAttributes.NestedAssembly

    let mkTypeAttributes (typeElement: ITypeElement): TypeAttributes =
        // These attributes are ignored by FCS when reading types: BeforeFieldInit.
        // todo: ansi, sequential

        let kind =
            match typeElement with
            | :? IClass as c ->
                (if c.IsAbstract then TypeAttributes.Abstract else enum 0) |||
                (if c.IsSealed then TypeAttributes.Sealed else enum 0)

            | :? IInterface -> TypeAttributes.Interface

            | :? IEnum
            | :? IStruct
            | :? IDelegate -> TypeAttributes.Sealed

            | _ -> enum 0

        let accessRights = mkTypeAccessRights typeElement

        kind ||| accessRights

    let createAssemblyScopeRef (assemblyName: AssemblyNameInfo): ILAssemblyRef =
        let name = assemblyName.Name
        let hash = None // todo: is assembly hash used in FCS?
        let retargetable = assemblyName.IsRetargetable

        let publicKey =
            match assemblyName.GetPublicKeyToken2().GetArrayOrNull() with
            | null ->
                match assemblyName.GetPublicKey() with
                | null -> None
                | key -> publicKeys.Intern(Some(PublicKey.PublicKey(key)))
            | bytes -> publicKeys.Intern(Some(PublicKey.PublicKeyToken(bytes)))

        let version =
            match assemblyName.Version with
            | null -> None
            | v -> Some(ILVersionInfo(uint16 v.Major, uint16 v.Minor, uint16 v.Revision, uint16 v.Build))

        let locale =
            match assemblyName.Culture with
            | null | "neutral" -> None
            | culture -> cultures.Intern(Some(culture))

        ILAssemblyRef.Create(name, hash, publicKey, retargetable, version, locale)
    
    let getAssemblyScope (assemblyName: AssemblyNameInfo): ILScopeRef =
//        let mutable scopeRef = Unchecked.defaultof<_>
//        match assemblyRefs.TryGetValue(assemblyName, &scopeRef) with
//        | true -> scopeRef
//        | _ ->

        let assemblyRef = ILScopeRef.Assembly (createAssemblyScopeRef assemblyName)
//        assemblyRefs.[assemblyName] <- assemblyRef
        assemblyRef
    
    let mkILScopeRef (fromModule: IPsiModule) (targetModule: IPsiModule): ILScopeRef =
        if fromModule == targetModule then ILScopeRef.Local else
        let assemblyName =
            match targetModule.ContainingProjectModule with
            | :? IAssembly as assembly -> assembly.AssemblyName
            | :? IProject as project -> project.GetOutputAssemblyNameInfo(targetModule.TargetFrameworkId)
            | _ -> failwithf $"mkIlScopeRef: {fromModule} -> {targetModule}"
        getAssemblyScope assemblyName

    let mkTypeRef (fromModule: IPsiModule) (typeElement: ITypeElement) =
        let clrTypeName = typeElement.GetClrName()
        let targetModule = typeElement.Module

//        let typeRefCache =
//            if fromModule == targetModule then localTypeRefs else
//            getAssemblyTypeRefCache targetModule

//        let mutable typeRef = Unchecked.defaultof<_>
//        match typeRefCache.TryGetValue(clrTypeName, &typeRef) with
//        | true -> typeRef
//        | _ ->

        let scopeRef = mkILScopeRef fromModule targetModule

        let typeRef =
//            if fromModule != targetModule && localTypeRefs.TryGetValue(clrTypeName, &typeRef) then
//                ILTypeRef.Create(scopeRef, typeRef.Enclosing, typeRef.Name) else

            let containingType = typeElement.GetContainingType()

            let enclosingTypes =
                match containingType with
                | null -> []
                | _ ->

                let enclosingTypeNames =
                    containingType.GetClrName().TypeNames
                    |> List.ofSeq
                    |> List.map mkNameFromTypeNameAndParamsNumber

                // The namespace is later split back by FCS during module import.
                // todo: rewrite this in FCS: add extension point, provide split namespaces
                let ns = clrTypeName.GetNamespaceName()
                if ns.IsEmpty() then enclosingTypeNames else

                match enclosingTypeNames with
                | hd :: tl -> String.Concat(ns, ".", hd) :: tl
                | [] -> failwithf $"mkTypeRef: {clrTypeName}"

            let name =
                match containingType with
                | null -> clrTypeName.FullName
                | _ -> mkNameFromClrTypeName clrTypeName

            ILTypeRef.Create(scopeRef, enclosingTypes, name)

//        typeRefCache.[clrTypeName.GetPersistent()] <- typeRef
        typeRef

    
    let mkType (fromModule: IPsiModule) (t: IType): ILType =
        if t.IsVoid() then ILType.Void else

        match t with
        | :? IDeclaredType as declaredType ->
            match declaredType.Resolve() with
            | :? EmptyResolveResult ->
                // todo: store unresolved type short name to invalidate the type def when that type appears
                // todo: add per-module singletons for predefines types
                mkType fromModule (fromModule.GetPredefinedType().Object)

            | resolveResult ->

            match resolveResult.DeclaredElement with
            | :? ITypeParameter ->
                mkType fromModule (fromModule.GetPredefinedType().Object) // todo

            | :? ITypeElement as typeElement ->
                let typeArgs =
                    let substitution = resolveResult.Substitution
                    let domain = substitution.Domain
                    if domain.IsEmpty() then [] else

                    domain
                    |> List.ofSeq
                    |> List.map (fun typeParameter -> mkType fromModule substitution.[typeParameter])

                let typeRef = mkTypeRef fromModule typeElement
                let typeSpec = ILTypeSpec.Create(typeRef, typeArgs)

                match typeElement with
                | :? IEnum
                | :? IStruct -> ILType.Value(typeSpec)
                | _ -> ILType.Boxed(typeSpec)

            | _ -> failwithf $"mkType: resolved element: {t}"

        | :? IArrayType as arrayType ->
            let elementType = mkType fromModule arrayType.ElementType
            let shape = ILArrayShape.FromRank(arrayType.Rank) // todo: check ranks
            ILType.Array(shape, elementType)

        | :? IPointerType as pointerType ->
            let elementType = mkType fromModule pointerType.ElementType
            ILType.Ptr(elementType)

        | _ -> failwithf $"mkType: type: {t}"
    
    let extends (psiModule: IPsiModule) (typeElement: ITypeElement): ILType option =
        // todo: intern
        match typeElement with
        | :? IClass as c ->
            match c.GetBaseClassType() with
            | null -> Some(mkType psiModule (psiModule.GetPredefinedType().Object))
            | baseType -> Some(mkType psiModule baseType)

        | :? IEnum -> Some(mkType psiModule (psiModule.GetPredefinedType().Enum))
        | :? IStruct -> Some(mkType psiModule (psiModule.GetPredefinedType().ValueType))
        | :? IDelegate -> Some(mkType psiModule (psiModule.GetPredefinedType().MulticastDelegate))

        | _ -> None

    let mkEnumInstanceValue (psiModule: IPsiModule) (enum: IEnum): ILFieldDef =
        let name = "value__"
        let fieldType =
            let enumType =
                let enumType = enum.GetUnderlyingType()
                if not enumType.IsUnknown then enumType else
                psiModule.GetPredefinedType().Int :> _
            mkType psiModule enumType
        let attributes = FieldAttributes.Public ||| FieldAttributes.SpecialName ||| FieldAttributes.RTSpecialName
        ILFieldDef(name, fieldType, attributes, None, None, None, None, emptyILCustomAttrs)

type ProjectFcsModuleReader(psiModule: IPsiModule, _cache: FcsModuleReaderCommonCache) =
    // todo: is it safe to keep symbolScope?
    let symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, false, true)

    let locker = JetFastSemiReenterableRWLock()

    let mutable moduleDef: ILModuleDef option = None

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, ILTypeDef>()

    member this.Timestamp = timestamp
    member this.PsiModule = psiModule

    member this.CreateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()
        use cookie = ReadLockCookie.Create()
        use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

        match symbolScope.GetTypeElementByCLRName(clrTypeName) with
        | null ->
            // The type doesn't exist in the module anymore.
            // The project has likely changed and FCS will invalidate cache for this module.
            ProjectFcsModuleReader.mkDummyTypeDef clrTypeName.ShortName

        // For multiple types with the same name we'll get some random/first one here.
        // todo: add a test case
        | typeElement ->
            let typeAttributes = ProjectFcsModuleReader.mkTypeAttributes typeElement
            let extends = ProjectFcsModuleReader.extends psiModule typeElement

            let fields =
                match typeElement with
                | :? IEnum as enum -> mkILFields [ ProjectFcsModuleReader.mkEnumInstanceValue psiModule enum ]
                | _ -> emptyILFields

            let typeDef = 
                ILTypeDef(clrTypeName.ShortName, typeAttributes, ILTypeDefLayout.Auto, [], [], extends, emptyILMethods,
                    emptyILTypeDefs, fields, emptyILMethodImpls, emptyILEvents, emptyILProperties,
                    emptyILSecurityDecls, emptyILCustomAttrs)

            typeDefs.[clrTypeName] <- typeDef
            typeDef

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


type PreTypeDef(clrTypeName: IClrTypeName, reader: ProjectFcsModuleReader) =
    member x.Name =
        let typeName = clrTypeName.TypeNames.Last() // todo: use clrTypeName.ShortName ? (check type params)
        ProjectFcsModuleReader.mkNameFromTypeNameAndParamsNumber typeName

    interface ILPreTypeDef with
        member x.Name = x.Name

        member x.Namespace =
            if not (clrTypeName.TypeNames.IsSingle()) then [] else
            clrTypeName.NamespaceNames |> List.ofSeq

        member x.GetTypeDef() =
            reader.CreateTypeDef(clrTypeName)
