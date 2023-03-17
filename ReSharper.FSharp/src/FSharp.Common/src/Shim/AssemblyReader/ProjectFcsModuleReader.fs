namespace rec JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Generic
open System.Linq
open System.Collections.Concurrent
open System.Reflection
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Application
open JetBrains.Diagnostics
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Impl.Special
open JetBrains.ReSharper.Psi.Impl.Types
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds

module ProjectFcsModuleReader =
    module DummyValues =
        let subsystemVersion = 4, 0
        let useHighEntropyVA = false
        let metadataVersion = String.Empty

    let typeParameterCountStrings = [| "`0"; "`1"; "`2"; "`3"; "`4"; "`5"; "`6"; "`7" |]
    let typeParameterCountStringsCount = typeParameterCountStrings.Length

    let mkTypeName (name: string) (paramsCount: int) =
        if paramsCount = 0 then name else

        let paramsCountString =
            if paramsCount >= typeParameterCountStringsCount then paramsCount.ToString() else
            typeParameterCountStrings[paramsCount]

        name + paramsCountString

    let mkNameFromTypeNameAndParamsNumber (nameAndParametersCount: TypeNameAndTypeParameterNumber) =
        mkTypeName nameAndParametersCount.TypeName nameAndParametersCount.TypeParametersNumber

    let mkNameFromClrTypeName (clrTypeName: IClrTypeName) =
        mkTypeName clrTypeName.ShortName clrTypeName.TypeParametersCount


type FcsTypeDefMembers =
    { mutable Methods: ILMethodDef[]
      mutable Fields: ILFieldDef list
      mutable Events: ILEventDef list
      mutable Properties: ILPropertyDef list
      mutable NestedTypes: ILPreTypeDef[] }

    static member Create() =
        { Fields = Unchecked.defaultof<_>
          Methods = Unchecked.defaultof<_>
          Events = Unchecked.defaultof<_>
          Properties = Unchecked.defaultof<_>
          NestedTypes = Unchecked.defaultof<_> }

type FcsTypeDef =
    { TypeDef: ILTypeDef
      mutable Members: FcsTypeDefMembers }


type ProjectFcsModuleReader(psiModule: IPsiModule, cache: FcsModuleReaderCommonCache, path,
        shim: IFcsAssemblyReaderShim) =
    let locks = psiModule.GetPsiServices().Locks
    let symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, false, true)

    let locker = JetFastSemiReenterableRWLock()

    let mutable isDirty = false
    let mutable upToDateChecked = null

    let mutable moduleDef: ILModuleDef option = None
    let mutable realModuleReader: ILModuleReader option = None

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, FcsTypeDef>() // todo: use non-concurrent, add locks
    let clrNamesByShortNames = CompactOneToSetMap<string, IClrTypeName>()

    let readData f =
        FSharpAsyncUtil.UsingReadLockInsideFcs(locks, fun _ ->
            use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

            if not (psiModule.GetPsiServices().CachesState.IsIdle.Value) then
                shim.MarkTypesDirty(psiModule)

            f ()
        )

    let isDll (project: IProject) (targetFrameworkId: TargetFrameworkId) =
        let projectProperties = project.ProjectProperties
        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> cfg.OutputType = ProjectOutputType.LIBRARY
        | _ -> false

    let mkDummyModuleDef () : ILModuleDef =
        // Should only be used as a recovery when the module is already invalid (e.g. the project is unloaded, etc)
        assert not (psiModule.IsValid())

        let name = psiModule.Name
        let typeDefs = mkILTypeDefs []
        let flags = 0
        let exportedTypes = mkILExportedTypes []

        mkILSimpleModule
            name name true
            ProjectFcsModuleReader.DummyValues.subsystemVersion
            ProjectFcsModuleReader.DummyValues.useHighEntropyVA
            typeDefs
            None None flags exportedTypes
            ProjectFcsModuleReader.DummyValues.metadataVersion

    let mkDummyTypeDef (name: string) =
        let attributes = enum 0
        let layout = ILTypeDefLayout.Auto
        let implements = []
        let genericParams = []
        let extends = None
        let nestedTypes = emptyILTypeDefs

        ILTypeDef(name, attributes, layout, implements, genericParams, extends, emptyILMethods, nestedTypes,
             emptyILFields, emptyILMethodImpls, emptyILEvents, emptyILProperties, false, emptyILSecurityDecls,
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
                | key -> cache.PublicKeys.Intern(Some(PublicKey.PublicKey(key)))
            | bytes -> cache.PublicKeys.Intern(Some(PublicKey.PublicKeyToken(bytes)))

        let version =
            match assemblyName.Version with
            | null -> None
            | v -> Some(ILVersionInfo(uint16 v.Major, uint16 v.Minor, uint16 v.Revision, uint16 v.Build))

        let locale =
            match assemblyName.Culture with
            | null | "neutral" -> None
            | culture -> cache.Cultures.Intern(Some(culture))

        ILAssemblyRef.Create(name, hash, publicKey, retargetable, version, locale)

    let getAssemblyScopeRef (assemblyName: AssemblyNameInfo): ILScopeRef =
        let mutable scopeRef = Unchecked.defaultof<_>
        match cache.AssemblyRefs.TryGetValue(assemblyName, &scopeRef) with
        | true -> scopeRef
        | _ ->

        let assemblyRef = ILScopeRef.Assembly(createAssemblyScopeRef assemblyName)
        cache.AssemblyRefs[assemblyName] <- assemblyRef
        assemblyRef

    let mkILScopeRef (targetModule: IPsiModule): ILScopeRef =
        if psiModule == targetModule then ILScopeRef.Local else

        let assemblyName =
            match targetModule.ContainingProjectModule with
            | :? IAssembly as assembly -> assembly.AssemblyName
            | :? IProject as project -> project.GetOutputAssemblyNameInfo(targetModule.TargetFrameworkId)
            | _ -> failwithf $"mkIlScopeRef: {psiModule} -> {targetModule}"

        getAssemblyScopeRef assemblyName


    let internTypeRef (typeRefCache: IDictionary<_, _>) scopeRef (clrTypeName: IClrTypeName) enclosing name =
        let typeRef = ILTypeRef.Create(scopeRef, enclosing, name)
        typeRefCache[clrTypeName.GetPersistent()] <- typeRef
        typeRef

    let internTypeRefAtScope typeRefCache scopeRef clrTypeName (typeRef: ILTypeRef) =
        internTypeRef typeRefCache scopeRef clrTypeName typeRef.Enclosing typeRef.Name

    let createTypeRef typeRefCache scopeRef (typeElement: ITypeElement) (clrTypeName: IClrTypeName) =
        let containingType = typeElement.GetContainingType()

        let enclosingTypes =
            match containingType with
            | null -> []
            | _ ->

            let enclosingTypeNames =
                [ for name in containingType.GetClrName().TypeNames do
                    ProjectFcsModuleReader.mkNameFromTypeNameAndParamsNumber name ]

            // The namespace is later split back by FCS during module import.
            // todo: rewrite this in FCS: add extension point, provide split namespaces
            let ns = clrTypeName.GetNamespaceName()
            if ns.IsEmpty() then enclosingTypeNames else

            match enclosingTypeNames with
            | hd :: tl -> $"{ns}.{hd}" :: tl
            | [] -> failwithf $"mkTypeRef: {clrTypeName}"

        let name =
            match containingType with
            | null -> clrTypeName.FullName
            | _ -> ProjectFcsModuleReader.mkNameFromClrTypeName clrTypeName

        internTypeRef typeRefCache scopeRef clrTypeName enclosingTypes name

    let mkTypeRef (typeElement: ITypeElement): ILTypeRef =
        let clrTypeName = typeElement.GetClrName()
        let targetModule = typeElement.Module
        let isLocalRef = psiModule == targetModule

        let typeRefCache =
            if isLocalRef then cache.LocalTypeRefs else cache.GetOrCreateAssemblyTypeRefCache(targetModule)

        let mutable typeRef = Unchecked.defaultof<_>
        if typeRefCache.TryGetValue(clrTypeName, &typeRef) then typeRef else

        if isLocalRef && cache.TryGetAssemblyTypeRef(psiModule, clrTypeName, &typeRef) then
            internTypeRefAtScope typeRefCache ILScopeRef.Local clrTypeName typeRef else

        let scopeRef = mkILScopeRef targetModule

        if not isLocalRef && cache.LocalTypeRefs.TryGetValue(clrTypeName, &typeRef) then
            internTypeRefAtScope typeRefCache scopeRef clrTypeName typeRef else

        createTypeRef typeRefCache scopeRef typeElement clrTypeName

    // todo: per-typedef cache
    let getGlobalIndex (typeParameter: ITypeParameter) =
        let mutable index = typeParameter.Index
        let mutable parent = typeParameter.Owner.GetContainingType()
        while isNotNull parent do
            index <- index + parent.TypeParameters.Count
            parent <- parent.GetContainingType()
        index

    let rec mkType (t: IType): ILType =
        if t.IsVoid() then ILType.Void else

        if not t.IsResolved then
            // todo: use candidates?
            mkUnresolvedType psiModule else

        match t with
        | :? IDeclaredType as declaredType ->
            match declaredType.Resolve() with
            | :? EmptyResolveResult ->
                match declaredType with
                | :? ISimplifiedIdTypeInfo -> () // todo: record unresolved names
                | _ -> ()

                // todo: add per-module singletons for predefines types
                mkType (psiModule.GetPredefinedType().Object)

            | resolveResult ->

            match resolveResult.DeclaredElement with
            | :? ITypeParameter as typeParameter ->
                match typeParameter.Owner with
                | null -> mkType (psiModule.GetPredefinedType().Object)
                | _ ->

                let index = getGlobalIndex typeParameter
                ILType.TypeVar (uint16 index)

            | :? ITypeElement as typeElement ->
                let typeArgs =
                    let substitution = resolveResult.Substitution
                    let domain = substitution.Domain
                    if domain.IsEmpty() then [] else

                    domain
                    |> List.ofSeq
                    |> List.sortBy getGlobalIndex
                    |> List.map (fun typeParameter -> mkType substitution[typeParameter])

                let typeRef = mkTypeRef typeElement
                let typeSpec = ILTypeSpec.Create(typeRef, typeArgs)

                match typeElement with
                | :? IEnum
                | :? IStruct -> ILType.Value(typeSpec)
                | _ -> ILType.Boxed(typeSpec)

            | _ -> failwithf $"mkType: resolved element: {t}"

        | :? IArrayType as arrayType ->
            let elementType = mkType arrayType.ElementType
            let shape = ILArrayShape.FromRank(arrayType.Rank) // todo: check ranks
            ILType.Array(shape, elementType)

        | :? IPointerType as pointerType ->
            let elementType = mkType pointerType.ElementType
            ILType.Ptr(elementType)

        | _ -> failwithf $"mkType: type: {t}"

    and mkUnresolvedType (psiModule: IPsiModule) =
        let objType = psiModule.GetPredefinedType().Object
        if objType.IsResolved then
            mkType objType
        else
            // todo: make a typeRef to System.Object in primary assembly
            ILType.Void

    let staticCallingConv = Callconv(ILThisConvention.Static, ILArgConvention.Default)
    let instanceCallingConv = Callconv(ILThisConvention.Instance, ILArgConvention.Default)

    let mkCallingConv (func: IFunction): ILCallingConv =
        if func.IsStatic then staticCallingConv else instanceCallingConv

    let mkCallingThisConv (func: IModifiersOwner): ILThisConvention =
        if func.IsStatic then ILThisConvention.Static else ILThisConvention.Instance

    let rec mkExplicitImplTypeName (declaredType: IDeclaredType) =
        let typeElement = declaredType.GetTypeElement()
        if isNull typeElement then "" else

        let clrTypeName = typeElement.GetClrName()

        let typeNames = 
            clrTypeName.TypeNames
            |> Seq.map (fun n -> n.TypeName)
            |> String.concat "."

        let name = 
            match clrTypeName.GetNamespaceName() with
            | "" -> typeNames
            | ns -> $"{ns}.{typeNames}"

        let typeParameters = typeElement.TypeParameters
        if typeParameters.Count = 0 then name else

        let substitution = declaredType.GetSubstitution()
        let typeArgs = 
            typeParameters
            |> Seq.map (fun tp ->
                let declaredType = substitution[tp].As<IDeclaredType>()
                mkExplicitImplTypeName declaredType
            )
            |> String.concat ","

        $"{name}<{typeArgs}>"

    let mkMethodRef (method: IFunction): ILMethodRef =
        let typeRef =
            let typeElement =
                match method.GetContainingType() with
                | null -> psiModule.GetPredefinedType().Object.GetTypeElement()
                | typeElement -> typeElement

            mkTypeRef typeElement

        let callingConv = mkCallingConv method
        let name = method.ShortName

        let typeParamsCount =
            match method with
            | :? IMethod as method -> method.TypeParameters.Count
            | _ -> 0

        // todo: use method def when available? it'll save things like types calc and other things
        let paramTypes =
            [ for parameter in method.Parameters do
                mkType parameter.Type ]

        let returnType = mkType method.ReturnType

        ILMethodRef.Create(typeRef, callingConv, name, typeParamsCount, paramTypes, returnType)

    let mkTypeDefExtends (typeElement: ITypeElement): ILType option =
        // todo: intern
        match typeElement with
        | :? IClass as c ->
            match c.GetBaseClassType() with
            | null -> Some(mkType (psiModule.GetPredefinedType().Object))
            | baseType -> Some(mkType baseType)

        | :? IEnum -> Some(mkType (psiModule.GetPredefinedType().Enum))
        | :? IStruct -> Some(mkType (psiModule.GetPredefinedType().ValueType))
        | :? IDelegate -> Some(mkType (psiModule.GetPredefinedType().MulticastDelegate))

        | _ -> None

    let mkTypeDefImplements (typeElement: ITypeElement) =
        [ for declaredType in typeElement.GetSuperTypesWithoutCircularDependent() do
            if declaredType.GetTypeElement() :? IInterface then
                mkType declaredType ]

    let mkCompilerGeneratedAttribute (attrTypeName: IClrTypeName) (args: ILAttribElem list): ILAttribute option =
        let attrType = TypeFactory.CreateTypeByCLRName(attrTypeName, NullableAnnotation.Unknown, psiModule)

        match attrType.GetTypeElement() with
        | null -> None
        | typeElement ->

        let ctor = typeElement.Constructors.First(fun ctor -> args.IsEmpty = ctor.IsParameterless)
        let methodSpec = ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, [])
        Some(ILAttribute.Decoded(methodSpec, args, []))

    let mkCompilerGeneratedAttributeNoArgs (attrTypeName: IClrTypeName): ILAttribute option =
        mkCompilerGeneratedAttribute attrTypeName []

    let paramArrayAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.PARAM_ARRAY_ATTRIBUTE_CLASS

    let extensionAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.EXTENSION_ATTRIBUTE_CLASS

    let isReadonlyAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.IS_READ_ONLY_ATTRIBUTE_FQN

    let internalsVisibleToAttribute arg =
        let args = [ ILAttribElem.String(Some(arg)) ]
        mkCompilerGeneratedAttribute PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS args

    // todo: typeof, arrays
    let attributeValueTypes =
        [| PredefinedType.BOOLEAN_FQN, fun (c: ConstantValue) -> ILAttribElem.Bool c.BoolValue
           PredefinedType.CHAR_FQN,    fun (c: ConstantValue) -> ILAttribElem.Char c.CharValue
           PredefinedType.SBYTE_FQN,   fun (c: ConstantValue) -> ILAttribElem.SByte c.SbyteValue
           PredefinedType.BYTE_FQN,    fun (c: ConstantValue) -> ILAttribElem.Byte c.ByteValue
           PredefinedType.SHORT_FQN,   fun (c: ConstantValue) -> ILAttribElem.Int16 c.ShortValue
           PredefinedType.USHORT_FQN,  fun (c: ConstantValue) -> ILAttribElem.UInt16 c.UshortValue
           PredefinedType.INT_FQN,     fun (c: ConstantValue) -> ILAttribElem.Int32 c.IntValue
           PredefinedType.UINT_FQN,    fun (c: ConstantValue) -> ILAttribElem.UInt32 c.UintValue
           PredefinedType.LONG_FQN,    fun (c: ConstantValue) -> ILAttribElem.Int64 c.LongValue
           PredefinedType.ULONG_FQN,   fun (c: ConstantValue) -> ILAttribElem.UInt64 c.UlongValue
           PredefinedType.FLOAT_FQN,   fun (c: ConstantValue) -> ILAttribElem.Single c.FloatValue
           PredefinedType.DOUBLE_FQN,  fun (c: ConstantValue) -> ILAttribElem.Double c.DoubleValue |]
        |> dict

    let mkCustomAttribute (attrInstance: IAttributeInstance) =
        let ctor = attrInstance.Constructor

        let attrType = TypeFactory.CreateType(ctor.ContainingType)
        let methodSpec = ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, [])

        let mkAttribElement (attrValue: AttributeValue) =
            let constantValue = attrValue.ConstantValue

            // todo: use default value for type from parameter/property?
            if constantValue.IsBadValue() then ILAttribElem.Null else

            if constantValue.IsString() then ILAttribElem.String(Some constantValue.StringValue) else

            let valueType = 
                if constantValue.IsEnum() then
                    constantValue.Type.GetEnumUnderlying()
                else
                    constantValue.Type

            let declaredType = valueType.As<IDeclaredType>()

            let mutable literalType = Unchecked.defaultof<_>
            match attributeValueTypes.TryGetValue(declaredType.GetClrName(), &literalType) with
            | true -> cache.AttributeValues.Intern(literalType constantValue)
            | _ -> ILAttribElem.Null

        let positionalArgs = 
            attrInstance.PositionParameters()
            |> List.ofSeq
            |> List.map mkAttribElement

        let namedArgs =
            attrInstance.NamedParameters()
            |> List.ofSeq
            |> List.filter (fun (Pair(_, attributeValue)) -> attributeValue.IsConstant)
            |> List.map (fun (Pair(name, attributeValue)) ->
                let attribElement = mkAttribElement attributeValue
                let valueType = mkType attributeValue.ConstantValue.Type
                name, valueType, true, attribElement)

        ILAttribute.Decoded(methodSpec, positionalArgs, namedArgs)

    let mkCustomAttributes (attributesSet: IAttributesSet) =
        attributesSet.GetAttributeInstances(AttributesSource.Self)
        |> List.ofSeq
        |> List.filter (fun attributeInstance -> isNotNull attributeInstance.Constructor)
        |> List.map mkCustomAttribute

    let mkGenericVariance (variance: TypeParameterVariance): ILGenericVariance =
        match variance with
        | TypeParameterVariance.IN -> ILGenericVariance.ContraVariant
        | TypeParameterVariance.OUT -> ILGenericVariance.CoVariant
        | _ -> ILGenericVariance.NonVariant

    // todo: test with same name parameter

    let mkGenericParameterDef (typeParameter: ITypeParameter): ILGenericParameterDef =
        let typeConstraints =
            [ for typeConstraint in typeParameter.TypeConstraints do
                mkType typeConstraint ]

        let attributes = storeILCustomAttrs emptyILCustomAttrs // todo

        { Name = typeParameter.ShortName
          Constraints = typeConstraints
          Variance = mkGenericVariance typeParameter.Variance
          HasReferenceTypeConstraint = typeParameter.IsReferenceType
          HasNotNullableValueTypeConstraint = typeParameter.IsValueType
          HasDefaultConstructorConstraint = typeParameter.HasDefaultConstructor
          CustomAttrsStored = attributes
          MetadataIndex = NoMetadataIdx }

    let mkGenericParamDefs (typeElement: ITypeElement) =
        let typeParameters = typeElement.GetAllTypeParameters().ResultingList()
        [ for i in typeParameters.Count - 1 .. -1 .. 0 do
            mkGenericParameterDef typeParameters[i] ]

    let mkTypeDefCustomAttrs (typeElement: ITypeElement) =
        let hasExtensions =
            let typeElement = typeElement.As<TypeElement>()
            if isNull typeElement then false else

            typeElement.EnumerateParts()
            |> Seq.exists (fun part -> not (Array.isEmpty part.ExtensionMethodInfos))

        let customAttributes = mkCustomAttributes typeElement
        [ yield! customAttributes
          if hasExtensions then
              match extensionAttribute () with
              | Some attribute -> attribute
              | _ -> () ]
        |> mkILCustomAttrs

    let mkEnumInstanceValue (enum: IEnum): ILFieldDef =
        let name = "value__"
        let fieldType =
            let enumType =
                let enumType = enum.GetUnderlyingType()
                if not enumType.IsUnknown then enumType else
                psiModule.GetPredefinedType().Int :> _
            mkType enumType
        let attributes = FieldAttributes.Public ||| FieldAttributes.SpecialName ||| FieldAttributes.RTSpecialName
        ILFieldDef(name, fieldType, attributes, None, None, None, None, emptyILCustomAttrs)

    let mkFieldAttributes (field: IField): FieldAttributes =
        let accessRights =
            match field.GetAccessRights() with
            | AccessRights.PUBLIC -> FieldAttributes.Public
            | AccessRights.INTERNAL -> FieldAttributes.Assembly
            | AccessRights.PRIVATE -> FieldAttributes.Private
            | AccessRights.PROTECTED -> FieldAttributes.Family
            | AccessRights.PROTECTED_OR_INTERNAL -> FieldAttributes.FamORAssem
            | AccessRights.PROTECTED_AND_INTERNAL -> FieldAttributes.FamANDAssem
            | _ -> enum 0

        accessRights |||
        (if field.IsStatic then FieldAttributes.Static else enum 0) |||
        (if field.IsReadonly then FieldAttributes.InitOnly else enum 0) |||
        (if field.IsConstant || field.IsEnumMember then FieldAttributes.Literal else enum 0)

    let literalTypes =
        [| PredefinedType.BOOLEAN_FQN, fun (c: ConstantValue) -> ILFieldInit.Bool c.BoolValue
           PredefinedType.CHAR_FQN,    fun (c: ConstantValue) -> ILFieldInit.Char (uint16 c.CharValue) // todo: can use UshortValue?
           PredefinedType.SBYTE_FQN,   fun (c: ConstantValue) -> ILFieldInit.Int8 c.SbyteValue
           PredefinedType.BYTE_FQN,    fun (c: ConstantValue) -> ILFieldInit.UInt8 c.ByteValue
           PredefinedType.SHORT_FQN,   fun (c: ConstantValue) -> ILFieldInit.Int16 c.ShortValue
           PredefinedType.USHORT_FQN,  fun (c: ConstantValue) -> ILFieldInit.UInt16 c.UshortValue
           PredefinedType.INT_FQN,     fun (c: ConstantValue) -> ILFieldInit.Int32 c.IntValue
           PredefinedType.UINT_FQN,    fun (c: ConstantValue) -> ILFieldInit.UInt32 c.UintValue
           PredefinedType.LONG_FQN,    fun (c: ConstantValue) -> ILFieldInit.Int64 c.LongValue
           PredefinedType.ULONG_FQN,   fun (c: ConstantValue) -> ILFieldInit.UInt64 c.UlongValue
           PredefinedType.FLOAT_FQN,   fun (c: ConstantValue) -> ILFieldInit.Single c.FloatValue
           PredefinedType.DOUBLE_FQN,  fun (c: ConstantValue) -> ILFieldInit.Double c.DoubleValue |]
        |> dict

    let nullLiteralValue = Some(ILFieldInit.Null)

    // todo: cache

    let getLiteralValue (value: ConstantValue) (valueType: IType): ILFieldInit option =
        if value.IsBadValue() then None else
        if value.IsNull() then nullLiteralValue else

        // A separate case to prevent interning string literals.
        if value.IsString() then Some(ILFieldInit.String(value.StringValue)) else

        match valueType with
        | :? IDeclaredType as declaredType ->
            let mutable literalType = Unchecked.defaultof<_>
            match literalTypes.TryGetValue(declaredType.GetClrName(), &literalType) with
            | true -> cache.LiteralValues.Intern(Some(literalType value))
            | _ -> None
        | _ -> None

    let mkFieldLiteralValue (field: IField) =
        let valueType =
            if not field.IsEnumMember then field.Type else

            match field.GetContainingType() with
            | :? IEnum as enum -> enum.GetUnderlyingType()
            | _ -> null

        let value = field.ConstantValue
        getLiteralValue value valueType

    // todo: unfinished field test (e.g. missing `;`)

    let mkFieldDef (field: IField): ILFieldDef =
        let name = field.ShortName
        let attributes = mkFieldAttributes field
        let fieldType = mkType field.Type
        let data = None // todo: check FCS
        let offset = None
        let literalValue = mkFieldLiteralValue field
        let marshal = None
        let customAttrs = mkCustomAttributes field |> mkILCustomAttrs

        ILFieldDef(name, fieldType, attributes, data, literalValue, offset, marshal, customAttrs)

    // todo: different attrs in class vs interface?
    let methodAbstractAttrs = MethodAttributes.Abstract ||| MethodAttributes.NewSlot ||| MethodAttributes.Virtual

    let mkMethodAttributes (method: IFunction): MethodAttributes =
        let accessRights =
            match method.GetAccessRights() with
            | AccessRights.PUBLIC -> MethodAttributes.Public
            | AccessRights.INTERNAL -> MethodAttributes.Assembly
            | AccessRights.PRIVATE -> MethodAttributes.Private
            | AccessRights.PROTECTED -> MethodAttributes.Family
            | AccessRights.PROTECTED_OR_INTERNAL -> MethodAttributes.FamORAssem
            | AccessRights.PROTECTED_AND_INTERNAL -> MethodAttributes.FamANDAssem
            | _ -> enum 0

        accessRights |||
        MethodAttributes.HideBySig |||
        (if method.IsStatic then MethodAttributes.Static else enum 0) |||
        (if method.IsSealed then MethodAttributes.Final else enum 0) |||
        (if method.IsAbstract then methodAbstractAttrs else enum 0) |||
        (if method.IsVirtual then MethodAttributes.Virtual else enum 0) ||| // todo: test
        (if not (method.GetHiddenMembers().IsEmpty()) then MethodAttributes.NewSlot else enum 0) ||| // todo: test
        (if method :? IConstructor || method :? IAccessor then MethodAttributes.SpecialName else enum 0)

    let mkParamDefaultValue (param: IParameter) =
        let defaultValue = param.GetDefaultValue()
        if defaultValue.IsBadValue then None else
        getLiteralValue defaultValue.ConstantValue defaultValue.DefaultTypeValue

    let mkParam (param: IParameter): ILParameter =
        let name = param.ShortName
        let paramType = mkType param.Type
        let defaultValue = mkParamDefaultValue param

        let isRef =
            match param.Kind with
            | ParameterKind.INPUT
            | ParameterKind.OUTPUT
            | ParameterKind.REFERENCE -> true
            | _ -> false

        let paramType =
            if isRef then ILType.Byref paramType else paramType

        let customAttributes = mkCustomAttributes param
        let attrs =
            [ yield! customAttributes

              if param.IsParameterArray then
                match paramArrayAttribute () with
                | Some attribute -> attribute
                | _ -> ()
            
              if param.Kind = ParameterKind.INPUT then
                 match isReadonlyAttribute () with
                 | Some attribute -> attribute
                 | _ -> () ]

        { Name = Some(name) // todo: intern?
          Type = paramType
          Default = defaultValue
          Marshal = None
          IsIn = param.Kind = ParameterKind.INPUT
          IsOut = param.Kind = ParameterKind.OUTPUT
          IsOptional = param.IsOptional
          CustomAttrsStored = attrs |> mkILCustomAttrs |> storeILCustomAttrs
          MetadataIndex = NoMetadataIdx }

    let mkParams (method: IFunction): ILParameter list =
        [ for parameter in method.Parameters do
            mkParam parameter ]

    let voidReturn = mkILReturn ILType.Void
    let methodBodyUnavailable = lazy MethodBody.NotAvailable

    let mkMethodReturn (method: IFunction) =
        let returnType = method.ReturnType
        if returnType.IsVoid() then voidReturn else

        mkType returnType |> mkILReturn

    let mkMethodDef (method: IFunction): ILMethodDef =
        let name = method.ShortName
        let methodAttrs = mkMethodAttributes method
        let callingConv = mkCallingConv method
        let parameters = mkParams method
        let ret = mkMethodReturn method

        let genericParams =
            match method with
            | :? IMethod as method ->
                [ for typeParameter in method.TypeParameters do
                    mkGenericParameterDef typeParameter ]
            | _ -> []

        let customAttrs =
            let isExtension = 
                match method with
                | :? IMethod as method -> method.IsExtensionMethod
                | _ -> false

            let customAttributes = mkCustomAttributes method
            [ yield! customAttributes
              if isExtension then
                  match extensionAttribute () with
                  | Some attribute -> attribute
                  | _ -> () ]
            |> mkILCustomAttrs 

        let implAttributes = MethodImplAttributes.Managed
        let body = methodBodyUnavailable
        let securityDecls = emptyILSecurityDecls
        let isEntryPoint = false

        ILMethodDef(name, methodAttrs, implAttributes, callingConv, parameters, ret, body, isEntryPoint, genericParams,
             securityDecls, customAttrs)

    let mkEventDefType (event: IEvent) =
        let eventType = event.Type
        if eventType.IsUnknown then None else
        Some(mkType eventType)

    let mkEventAddMethod (event: IEvent) =
        let adder = event.Adder
        if isNotNull adder then adder else ImplicitAccessor(event, AccessorKind.ADDER) :> _
        |> mkMethodRef

    let mkEventRemoveMethod (event: IEvent) =
        let remover = event.Remover
        if isNotNull remover then remover else ImplicitAccessor(event, AccessorKind.REMOVER) :> _
        |> mkMethodRef

    let mkEventFireMethod (event: IEvent) =
        match event.Raiser with
        | null -> None
        | adder -> Some(mkMethodRef adder)

    let mkEventDef (event: IEvent): ILEventDef =
        let eventType = mkEventDefType event
        let name = event.ShortName
        let attributes = enum 0 // Not used by FCS.
        let addMethod = mkEventAddMethod event
        let removeMethod = mkEventRemoveMethod event
        let fireMethod = mkEventFireMethod event
        let otherMethods = []
        let customAttrs = mkCustomAttributes event |> mkILCustomAttrs

        ILEventDef(eventType, name, attributes, addMethod, removeMethod, fireMethod, otherMethods, customAttrs)

    let mkPropertyParams (property: IProperty) =
        [ for parameter in property.Parameters do
            mkType parameter.Type ]

    let mkPropertySetter (property: IProperty) =
        match property.Setter with
        | null -> None
        | setter -> Some(mkMethodRef setter)

    let mkPropertyGetter (property: IProperty) =
        match property.Getter with
        | null -> None
        | getter -> Some(mkMethodRef getter)

    let mkPropertyDef (property: IProperty): ILPropertyDef =
        let name = property.ShortName
        let attrs = enum 0 // todo
        let callConv = mkCallingThisConv property
        let propertyType = mkType property.Type
        let init = None // todo
        let args = mkPropertyParams property
        let setter = mkPropertySetter property
        let getter = mkPropertyGetter property
        let customAttrs = mkCustomAttributes property |> mkILCustomAttrs

        ILPropertyDef(name, attrs, setter, getter, callConv, propertyType, init, args, customAttrs)

    let usingTypeElement (typeName: IClrTypeName) defaultValue f =
        let mutable result = Unchecked.defaultof<_>
        readData (fun _ ->
            if not (psiModule.IsValid()) then
                result <- defaultValue else

            let typeElement = symbolScope.GetTypeElementByCLRName(typeName)
            if isNull typeElement then
                result <- defaultValue else

            result <- f typeElement
        )
        result

    let getOrCreateMembers (typeName: IClrTypeName) defaultValue (getMemberTable: FcsTypeDefMembers -> 'Table) createTable =
        use _ = locker.UsingWriteLock()

        let fcsTypeDef = typeDefs.TryGetValue(typeName)
        if isNull fcsTypeDef then
            // The type has been invalidated.
            // Fcs will check the module timestamp and request new data, so return dummy info.
            defaultValue else

        let members = fcsTypeDef.Members
        let table = if isNotNull members then getMemberTable members else Unchecked.defaultof<_>
        if isNotNull table then table else

        lock fcsTypeDef (fun _ ->
            if isNull fcsTypeDef.Members then
                fcsTypeDef.Members <- FcsTypeDefMembers.Create()

            let members = fcsTypeDef.Members
            let table = getMemberTable members
            if isNotNull table then table else

            usingTypeElement typeName defaultValue (createTable members))

    let isExplicitImpl (typeMember: ITypeMember) =
        let overridableMember = typeMember.As<IOverridableMember>()
        isNotNull overridableMember && overridableMember.IsExplicitImplementation

    let getSignature (parametersOwner: IParametersOwner) =
        parametersOwner.GetSignature(parametersOwner.IdSubstitution)

    let mkMethods (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let methods =
            let seenMethods = HashSet(CSharpInvocableSignatureComparer.Overload)
            [| for method in typeElement.GetMembers().OfType<IFunction>() do
                if not (isExplicitImpl method) && seenMethods.Add(getSignature method) then
                    yield mkMethodDef method |]

        table.Methods <- methods
        methods

    let mkFields (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let fields =
            match typeElement with
            | :? IEnum as e -> e.EnumMembers
            | _ -> typeElement.GetMembers().OfType<IField>()

        let fields =
            [ for field in fields do
                yield mkFieldDef field ]

        let fields = 
            match typeElement with
            | :? IEnum as enum -> mkEnumInstanceValue enum :: fields
            | _ -> fields

        table.Fields <- fields
        fields

    let mkProperties (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let properties =
            let seenProperties = HashSet(CSharpInvocableSignatureComparer.Overload)
            [ for property in typeElement.Properties do
                if not (isExplicitImpl property) && seenProperties.Add(getSignature property) then
                    yield mkPropertyDef property ]

        table.Properties <- properties
        properties

    let mkEvents (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let events =
            [ for event in typeElement.Events do
                yield mkEventDef event ]

        table.Events <- events
        events

    let mkNestedTypes reader (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let nestedTypes =
            [| for typeElement in typeElement.NestedTypes do
                PreTypeDef(typeElement, reader) :> ILPreTypeDef |]

        table.NestedTypes <- nestedTypes
        nestedTypes

    let mkTypeDefName (typeElement: ITypeElement) (clrTypeName: IClrTypeName) =
        match typeElement.GetContainingType() with
        | null -> clrTypeName.FullName
        | _ -> ProjectFcsModuleReader.mkNameFromClrTypeName clrTypeName


    let getOrCreateMethods (typeName: IClrTypeName) =
        getOrCreateMembers typeName EmptyArray.Instance (fun members -> members.Methods) mkMethods

    let getOrCreateFields (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Fields) mkFields

    let getOrCreateProperties (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Properties) mkProperties

    let getOrCreateEvents (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Events) mkEvents

    let getOrCreateNestedTypes reader (typeName: IClrTypeName) =
        getOrCreateMembers typeName [||] (fun members -> members.NestedTypes) (mkNestedTypes reader)


    let isUpToDateTypeParamDef (typeParameter: ITypeParameter) (genericParameterDef: ILGenericParameterDef) =
        let constraints = typeParameter.TypeConstraints |> Seq.map mkType |> List.ofSeq
        constraints = genericParameterDef.Constraints

    let isUpToDateTypeParamDefs (typeParameters: IList<ITypeParameter>) (paramDefs: ILGenericParameterDefs) =
        typeParameters.Count = paramDefs.Length &&
        Seq.forall2 isUpToDateTypeParamDef typeParameters paramDefs

    let isUpToDateCustomAttributes (actual: IAttributesSet) (attrs: ILAttributes) =
        let actual = mkCustomAttributes actual
        actual.AsArray() = attrs.AsArray()

    let isUpToDateTypeDefCustomAttributes (typeElement: ITypeElement) (typeDef: ILTypeDef) =
        let actual = mkTypeDefCustomAttrs typeElement 
        actual.AsArray() = typeDef.CustomAttrs.AsArray()

    let isUpToDateParameterDef (param: IParameter) (paramDef: ILParameter) =
        mkType param.Type = paramDef.Type &&

        let defaultValue = mkParamDefaultValue param
        defaultValue = paramDef.Default

    let isUpToDateReturn (method: IFunction) (methodDef: ILMethodDef) =
        let ret = mkMethodReturn method
        let methodDefReturn = methodDef.Return

        ret.Type = methodDefReturn.Type &&
        isUpToDateCustomAttributes method.ReturnTypeAttributes methodDefReturn.CustomAttrs

    let isUpToDateMethodDef (method: IFunction) (methodDef: ILMethodDef) =
        mkMethodAttributes method = methodDef.Attributes &&

        let parameters = method.Parameters
        parameters.Count = methodDef.Parameters.Length &&
        Seq.forall2 isUpToDateParameterDef parameters methodDef.Parameters &&

        isUpToDateReturn method methodDef &&

        isUpToDateCustomAttributes method methodDef.CustomAttrs &&
        
        let method = method.As<IMethod>()
        isNull method || isUpToDateTypeParamDefs method.TypeParameters methodDef.GenericParams

    let isUpToDateMethodsDefs (typeElement: ITypeElement) (methodDefs: ILMethodDef[]) =
        isNull methodDefs ||

        let methods = typeElement.GetMembers().OfType<IFunction>().AsArray()
        methods.Length = methodDefs.Length &&

        Array.forall2 isUpToDateMethodDef methods methodDefs

    let isUpToDateFieldDef (field: IField) (fieldDef: ILFieldDef) =
        mkType field.Type = fieldDef.FieldType &&
        mkFieldLiteralValue field = fieldDef.LiteralValue &&
        isUpToDateCustomAttributes field fieldDef.CustomAttrs

    let isUpToDateFieldDefs (typeElement: ITypeElement) (fieldDefs: ILFieldDef list) =
        isNull fieldDefs ||

        let fields = List.ofSeq typeElement.Fields
        fields.Length = fieldDefs.Length &&

        List.forall2 isUpToDateFieldDef fields fieldDefs

    let isUpToDateEventDef (event: IEvent) (eventDef: ILEventDef) =
        mkEventDefType event = eventDef.EventType &&
        mkEventAddMethod event = eventDef.AddMethod &&
        mkEventRemoveMethod event = eventDef.RemoveMethod &&
        mkEventFireMethod event = eventDef.FireMethod &&
        isUpToDateCustomAttributes event eventDef.CustomAttrs

    let isUpToDateEventDefs (typeElement: ITypeElement) (eventDefs: ILEventDef list) =
        isNull eventDefs ||

        let events = List.ofSeq typeElement.Events
        events.Length = eventDefs.Length &&

        List.forall2 isUpToDateEventDef events eventDefs

    let isUpToDatePropertyDef (property: IProperty) (propertyDef: ILPropertyDef) =
        mkPropertyParams property = propertyDef.Args &&
        mkPropertySetter property = propertyDef.SetMethod &&
        mkPropertyGetter property = propertyDef.GetMethod &&
        isUpToDateCustomAttributes property propertyDef.CustomAttrs

    let isUpToDatePropertyDefs (typeElement: ITypeElement) (propertyDefs: ILPropertyDef list) =
        isNull propertyDefs ||

        let properties = List.ofSeq typeElement.Properties
        properties.Length = properties.Length &&

        List.forall2 isUpToDatePropertyDef properties propertyDefs

    let rec isUpToDateTypeDef (typeElement: ITypeElement) (fcsTypeDef: FcsTypeDef) =
        let typeDef = fcsTypeDef.TypeDef

        let extends = mkTypeDefExtends typeElement
        extends = typeDef.Extends &&

        let implements = mkTypeDefImplements typeElement
        implements = typeDef.Implements && 

        isUpToDateTypeParamDefs typeElement.TypeParameters typeDef.GenericParams &&
        isUpToDateTypeDefCustomAttributes typeElement typeDef &&
        isUpToDateMembers typeElement fcsTypeDef.Members

    and isUpToDateNestedTypeDefs  (preTypeDefs: ILPreTypeDef[]) =
        isNull preTypeDefs ||

        preTypeDefs |> Array.forall (fun preTypeDef ->
            let preTypeDef = preTypeDef :?> PreTypeDef
            let clrTypeName = preTypeDef.ClrTypeName
            let typeDef = typeDefs.TryGetValue(clrTypeName)
            isNull typeDef ||

            let typeElement = symbolScope.GetTypeElementByCLRName(clrTypeName).NotNull("IsUpToDate: nested type")
            isUpToDateTypeDef typeElement typeDef)

    and isUpToDateMembers (typeElement: ITypeElement) (members: FcsTypeDefMembers) =
        isNull members ||

        isUpToDateNestedTypeDefs members.NestedTypes &&
        isUpToDateMethodsDefs typeElement members.Methods &&
        isUpToDateFieldDefs typeElement members.Fields &&
        isUpToDateEventDefs typeElement members.Events &&
        isUpToDatePropertyDefs typeElement members.Properties

    let isUpToDateTypeDef (clrTypeName: IClrTypeName) (fcsTypeDef: FcsTypeDef) =
        match symbolScope.GetTypeElementByCLRName(clrTypeName) with
        | null -> false
        | typeElement -> isUpToDateTypeDef typeElement fcsTypeDef

    // todo: check added/removed types
    /// Checks if any external change has lead to a metadata change,
    /// e.g. a super type resolves to a different thing.
    let isUpToDate () =
        use lock = locker.UsingWriteLock()

        if not isDirty || typeDefs.IsEmpty then true else

        if isNull upToDateChecked then
            upToDateChecked <- HashSet()

        use cookie = ReadLockCookie.Create()
        use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

        let mutable isUpToDate = true

        for KeyValue(clrTypeName, fcsTypeDef) in List.ofSeq typeDefs do
            Interruption.Current.CheckAndThrow()

            if upToDateChecked.Contains(clrTypeName) then () else

            if not (isUpToDateTypeDef clrTypeName fcsTypeDef) then
                typeDefs.TryRemove(clrTypeName) |> ignore
                isUpToDate <- false

            upToDateChecked.Add(clrTypeName) |> ignore

        isDirty <- false
        isUpToDate

    member this.CreateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()

        match typeDefs.TryGetValue(clrTypeName) with
        | NotNull typeDef -> typeDef.TypeDef
        | _ ->

        readData (fun _ ->
            if not (psiModule.IsValid()) then () else

            match symbolScope.GetTypeElementByCLRName(clrTypeName) with
            | null ->
                // The type doesn't exist in the module anymore.
                // The project has likely changed and FCS will invalidate cache for this module.
                ()

            // For multiple types with the same name we'll get some random/first one here.
            // todo: add a test case
            | typeElement ->
                let name = mkTypeDefName typeElement clrTypeName
                let typeAttributes = mkTypeAttributes typeElement
                let extends = mkTypeDefExtends typeElement
                let implements = mkTypeDefImplements typeElement
                let nestedTypes = mkILTypeDefsComputed (fun _ -> getOrCreateNestedTypes this clrTypeName)
                let genericParams = mkGenericParamDefs typeElement
                let methods = mkILMethodsComputed (fun _ -> getOrCreateMethods clrTypeName)
                let fields = mkILFieldsLazy (lazy getOrCreateFields clrTypeName)
                let properties = mkILPropertiesLazy (lazy getOrCreateProperties clrTypeName)
                let events = mkILEventsLazy (lazy getOrCreateEvents clrTypeName)
                let customAttrs = mkTypeDefCustomAttrs typeElement

                let typeDef =
                    ILTypeDef(name, typeAttributes, ILTypeDefLayout.Auto, implements, genericParams,
                        extends, methods, nestedTypes, fields, emptyILMethodImpls, events, properties, false,
                        emptyILSecurityDecls, customAttrs)

                let fcsTypeDef = 
                    { TypeDef = typeDef
                      Members = Unchecked.defaultof<_> }

                clrNamesByShortNames.Add(typeElement.ShortName, clrTypeName)
                typeDefs[clrTypeName] <- fcsTypeDef
            )

        match typeDefs.TryGetValue(clrTypeName) with
        | NotNull typeDef -> typeDef.TypeDef
        | _ -> mkDummyTypeDef clrTypeName.ShortName

    member this.InvalidateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()
        typeDefs.TryRemove(clrTypeName) |> ignore
        shim.Logger.Trace("Invalidate TypeDef: {0}: {1}", path, clrTypeName)

        // todo: invalidate timestamp on seen-by-FCS type changes only
        // todo: add test for adding/removing not-seen-by-FCS types
        moduleDef <- None
        timestamp <- DateTime.UtcNow
        shim.Logger.Trace("New timestamp: {0}: {1}", path, timestamp)

    interface IProjectFcsModuleReader with
        member this.ILModuleDef =
            use lock = locker.UsingWriteLock()

            match moduleDef with
            | Some(moduleDef) -> moduleDef
            | None ->

            readData (fun _ ->
                if not (psiModule.IsValid()) then () else

                let project = psiModule.ContainingProjectModule :?> IProject
                let moduleName = project.Name
                let assemblyName = project.GetOutputAssemblyName(psiModule.TargetFrameworkId)
                let isDll = isDll project psiModule.TargetFrameworkId

                let typeDefs =
                    // todo: make inner types computed on demand, needs an Fcs patch
                    let result = List<ILPreTypeDef>()

                    let rec addTypes (ns: INamespace) =
                        for typeElement in ns.GetNestedTypeElements(symbolScope) do
                            result.Add(PreTypeDef(typeElement, this))
                        for nestedNs in ns.GetNestedNamespaces(symbolScope) do
                            addTypes nestedNs

                    addTypes symbolScope.GlobalNamespace

                    let preTypeDefs = result.ToArray()
                    mkILTypeDefsComputed (fun _ -> preTypeDefs)

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

                let ivtAttributes =
                    let psiServices = psiModule.GetPsiServices()
                    let attributeInstances =
                        psiServices.Symbols
                            .GetModuleAttributes(psiModule)
                            .GetAttributeInstances(PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS, false)

                    [| for instance in attributeInstances do
                         match instance.PositionParameter(0).ConstantValue.AsString() with
                         | null -> ()
                         | s ->

                         match internalsVisibleToAttribute s with
                         | Some attribute -> attribute
                         | _ -> () |]

                let newModuleDef =
                    if ivtAttributes.IsEmpty() then newModuleDef else

                    let attrs = mkILCustomAttrsFromArray ivtAttributes |> storeILCustomAttrs
                    let manifest = { newModuleDef.Manifest.Value with CustomAttrsStored = attrs }
                    { newModuleDef with Manifest = Some(manifest) }

                moduleDef <- Some(newModuleDef)
            )

            match moduleDef with
            | None -> mkDummyModuleDef ()
            | Some value -> value

        member this.Dispose() =
            match realModuleReader with
            | Some(moduleReader) -> moduleReader.Dispose()
            | _ -> ()

        member this.ILAssemblyRefs = []

        member this.Timestamp =
            use lock = locker.UsingReadLock()
            timestamp

        member this.Path = path
        member this.PsiModule = psiModule

        member val RealModuleReader = None with get, set

        member this.InvalidateTypeDefs(shortName) =
            use _ = locker.UsingWriteLock()
            shim.Logger.Trace("Invalidate types by short name: {0}: {1} ", path, shortName)
            for clrTypeName in clrNamesByShortNames.GetValuesSafe(shortName) do
                this.InvalidateTypeDef(clrTypeName)
            isDirty <- true
            upToDateChecked <- null

        member this.UpdateTimestamp() =
            use _ = locker.UsingWriteLock()
            shim.Logger.Trace("Trying to update timestamp: {0}", path)
            if not (isUpToDate ()) then
                moduleDef <- None
                timestamp <- DateTime.UtcNow
                shim.Logger.Trace("New timestamp: {0}: {1}", path, timestamp)

        member this.InvalidateAllTypeDefs() =
            use _ = locker.UsingWriteLock()
            shim.Logger.Trace("Invalidate all type defs: {0}", path)
            typeDefs.Clear()
            moduleDef <- None
            timestamp <- DateTime.UtcNow
            shim.Logger.Trace("New timestamp: {0}: {1}", path, timestamp)

        member this.MarkDirty() =
            use _ = locker.UsingWriteLock()
            shim.Logger.Trace("Mark dirty: {0}", path)
            isDirty <- true
            upToDateChecked <- null


type PreTypeDef(clrTypeName: IClrTypeName, reader: ProjectFcsModuleReader) =
    new (typeElement: ITypeElement, reader: ProjectFcsModuleReader) =
        PreTypeDef(typeElement.GetClrName().GetPersistent(), reader) // todo: intern

    member this.ClrTypeName = clrTypeName

    interface ILPreTypeDef with
        member x.Name =
            let typeName = clrTypeName.TypeNames.Last() // todo: use clrTypeName.ShortName ? (check type params)
            ProjectFcsModuleReader.mkNameFromTypeNameAndParamsNumber typeName

        member x.Namespace =
            if not (clrTypeName.TypeNames.IsSingle()) then [] else
            clrTypeName.NamespaceNames |> List.ofSeq

        member x.GetTypeDef() =
            reader.CreateTypeDef(clrTypeName)
