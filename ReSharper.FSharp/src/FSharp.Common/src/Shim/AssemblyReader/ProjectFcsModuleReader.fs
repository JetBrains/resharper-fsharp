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
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
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
      mutable Properties: ILPropertyDef list }

    static member Create() =
        { Fields = Unchecked.defaultof<_>
          Methods = Unchecked.defaultof<_>
          Events = Unchecked.defaultof<_>
          Properties = Unchecked.defaultof<_> }

type FcsTypeDef =
    { TypeDef: ILTypeDef
      mutable Members: FcsTypeDefMembers }


type ProjectFcsModuleReader(psiModule: IPsiModule, cache: FcsModuleReaderCommonCache, shim: IFcsAssemblyReaderShim, path) =
    // todo: is it safe to keep symbolScope?
    let symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, false, true)

    let locker = JetFastSemiReenterableRWLock()

    let mutable moduleDef: ILModuleDef option = None
    let mutable realModuleReader: ILModuleReader option = None

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, FcsTypeDef>() // todo: use non-concurrent, add locks
    let clrNamesByShortNames = CompactOneToSetMap<string, IClrTypeName>()

    let typeUsedNames = OneToSetMap<IClrTypeName, string>()
    let usedShortNamesToUsingTypes = OneToSetMap<string, IClrTypeName>()

    // todo: record F#->F# chains
    // * base types are required
    // changes in inferred types/signatures due to changes in unrelated types seem OK to ignore,
    // since C# will show some error and will require fixing 
    let usedFSharpModulesToTypes = OneToSetMap<IPsiModule, IClrTypeName>() 

    let mutable currentTypeName: IClrTypeName = null
    let mutable currentTypeUnresolvedUsedNames: ISet<string> = null

    let recordUsedType (typeElement: ITypeElement) =
        if typeElement :? ITypeParameter then () else // todo: check this

        let typeElementModule = typeElement.Module
        if not (typeElementModule :? IProjectPsiModule) then () else

        if typeElement.PresentationLanguage.Is<FSharpLanguage>() then
            let fsProjectModule = typeElementModule
            usedFSharpModulesToTypes.Add(fsProjectModule, currentTypeName) |> ignore
        else
            let shortName = typeElement.ShortName
            usedShortNamesToUsingTypes.Add(shortName, currentTypeName) |> ignore
            typeUsedNames.Add(currentTypeName, shortName) |> ignore

    let isDll (project: IProject) (targetFrameworkId: TargetFrameworkId) =
        let projectProperties = project.ProjectProperties
        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> cfg.OutputType = ProjectOutputType.LIBRARY
        | _ -> false

    let mkDummyTypeDef (name: string) =
        let attributes = enum 0
        let layout = ILTypeDefLayout.Auto
        let implements = []
        let genericParams = []
        let extends = None
        let nestedTypes = emptyILTypeDefs

        ILTypeDef(name, attributes, layout, implements, genericParams, extends, emptyILMethods, nestedTypes,
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
            | hd :: tl -> String.Concat(ns, ".", hd) :: tl
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

        recordUsedType typeElement

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

        match t with
        | :? IDeclaredType as declaredType ->
            match declaredType.Resolve() with
            | :? EmptyResolveResult ->
                match declaredType with
                | :? ISimplifiedIdTypeInfo as simpleTypeInfo ->
                    let shortName = simpleTypeInfo.GetShortName()
                    if isNotNull shortName then
                        currentTypeUnresolvedUsedNames.Add(shortName) |> ignore
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

    let staticCallingConv = Callconv(ILThisConvention.Static, ILArgConvention.Default)
    let instanceCallingConv = Callconv(ILThisConvention.Instance, ILArgConvention.Default)

    let mkCallingConv (func: IFunction): ILCallingConv =
        if func.IsStatic then staticCallingConv else instanceCallingConv

    let mkCallingThisConv (func: IModifiersOwner): ILThisConvention =
        if func.IsStatic then ILThisConvention.Static else ILThisConvention.Instance


    let mkMethodRef (method: IFunction): ILMethodRef =
        let typeRef =
            let typeElement =
                match method.GetContainingType() with
                | null -> psiModule.GetPredefinedType().Object.GetTypeElement()
                | typeElement -> typeElement

            mkTypeRef typeElement

        let callingConv = mkCallingConv method
        let name = method.ShortName // todo: type parameter suffix?

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

    let extends (typeElement: ITypeElement): ILType option =
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

    let mkCompilerGeneratedAttribute (attrTypeName: IClrTypeName) (args: ILAttribElem list): ILAttribute =
        let attrType = TypeFactory.CreateTypeByCLRName(attrTypeName, NullableAnnotation.Unknown, psiModule)

        match attrType.GetTypeElement() with
        | null -> failwithf $"getting param array type element in {psiModule}" // todo: safer handling
        | typeElement ->

        let ctor = typeElement.Constructors.First(fun ctor -> args.IsEmpty = ctor.IsParameterless)
        let methodSpec = ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, [])
        ILAttribute.Decoded(methodSpec, args, [])

    let mkCompilerGeneratedAttributeNoArgs (attrTypeName: IClrTypeName): ILAttribute =
        mkCompilerGeneratedAttribute attrTypeName []

    let paramArrayAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.PARAM_ARRAY_ATTRIBUTE_CLASS

    let extensionAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.EXTENSION_ATTRIBUTE_CLASS

    let internalsVisibleToAttribute arg =
        let args = [ ILAttribElem.String(Some(arg)) ]
        mkCompilerGeneratedAttribute PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS args

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
        let _namedArgs = attrInstance.NamedParameters() |> List.ofSeq

        let attrType = TypeFactory.CreateType(ctor.ContainingType)
        let methodSpec = ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, [])

        let positionalArgs = 
            attrInstance.PositionParameters()
            |> List.ofSeq
            |> List.map (fun attrValue ->
                let constantValue = attrValue.ConstantValue
                if constantValue.IsString() then ILAttribElem.String(Some constantValue.StringValue) else

                let declaredType = constantValue.Type.As<IDeclaredType>()
                if isNull declaredType then ILAttribElem.Null else

                // todo: typeof, arrays

                let mutable literalType = Unchecked.defaultof<_>
                match attributeValueTypes.TryGetValue(declaredType.GetClrName(), &literalType) with
                | true -> cache.AttributeValues.Intern(literalType constantValue)
                | _ -> ILAttribElem.Null
            )

        ILAttribute.Decoded(methodSpec, positionalArgs, [])

    let mkCustomAttributes (attributesSet: IAttributesSet) =
        attributesSet.GetAttributeInstances(AttributesSource.Self)
        |> List.ofSeq
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

    // todo: unfinished field test (e.g. missing `;`)

    let mkField (field: IField): ILFieldDef =
        let name = field.ShortName
        let attributes = mkFieldAttributes field

        let fieldType = mkType field.Type
        let data = None // todo: check FCS
        let offset = None

        let valueType =
            if not field.IsEnumMember then field.Type else

            match field.GetContainingType() with
            | :? IEnum as enum -> enum.GetUnderlyingType()
            | _ -> null

        let value = field.ConstantValue
        let literalValue = getLiteralValue value valueType

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

    let mkParam (param: IParameter): ILParameter =
        let name = param.ShortName
        let paramType = mkType param.Type

        let defaultValue =
            let defaultValue = param.GetDefaultValue()
            if defaultValue.IsBadValue then None else
            getLiteralValue defaultValue.ConstantValue defaultValue.DefaultTypeValue

        // todo: other attrs
        let attrs = [ if param.IsParameterArray then paramArrayAttribute () ]

        { Name = Some(name) // todo: intern?
          Type = paramType
          Default = defaultValue
          Marshal = None // todo: used in infos.fs
          IsIn = param.Kind.Equals(ParameterKind.INPUT) // todo: add test
          IsOut = param.Kind.Equals(ParameterKind.OUTPUT) // todo: add test
          IsOptional = param.IsOptional
          CustomAttrsStored = attrs |> mkILCustomAttrs |> storeILCustomAttrs
          MetadataIndex = NoMetadataIdx }

    let mkParams (method: IFunction): ILParameter list =
        [ for parameter in method.Parameters do
            mkParam parameter ]

    let voidReturn = mkILReturn ILType.Void
    let methodBodyUnavailable = lazy MethodBody.NotAvailable

    let mkMethod (method: IFunction): ILMethodDef =
        let name = method.ShortName // todo: type parameter suffix?
        let methodAttrs = mkMethodAttributes method
        let callingConv = mkCallingConv method
        let parameters = mkParams method

        let ret =
            let returnType = method.ReturnType
            if returnType.IsVoid() then voidReturn else

            mkType returnType |> mkILReturn

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
                  extensionAttribute () ]
            |> mkILCustomAttrs 

        let implAttributes = MethodImplAttributes.Managed
        let body = methodBodyUnavailable
        let securityDecls = emptyILSecurityDecls
        let isEntryPoint = false

        ILMethodDef(name, methodAttrs, implAttributes, callingConv, parameters, ret, body, isEntryPoint, genericParams,
             securityDecls, customAttrs)

    let mkEvent (event: IEvent): ILEventDef =
        let eventType =
            let eventType = event.Type
            if eventType.IsUnknown then None else
            Some(mkType eventType)

        let name = event.ShortName
        let attributes = enum 0 // Not used by FCS.

        let addMethod =
            let adder = event.Adder
            if isNotNull adder then adder else ImplicitAccessor(event, AccessorKind.ADDER) :> _
            |> mkMethodRef

        let removeMethod =
            let remover = event.Remover
            if isNotNull remover then remover else ImplicitAccessor(event, AccessorKind.REMOVER) :> _
            |> mkMethodRef

        let fireMethod =
            match event.Raiser with
            | null -> None
            | adder -> Some(mkMethodRef adder)

        let otherMethods = []
        let customAttrs = mkCustomAttributes event |> mkILCustomAttrs

        ILEventDef(eventType, name, attributes, addMethod, removeMethod, fireMethod, otherMethods, customAttrs)

    let mkProperty (property: IProperty): ILPropertyDef =
        let name = property.ShortName

        let attrs = enum 0 // todo
        let callConv = mkCallingThisConv property
        let propertyType = mkType property.Type
        let init = None // todo

        let args =
            [ for parameter in property.Parameters do
                mkType parameter.Type ]

        let setter =
            match property.Setter with
            | null -> None
            | setter -> Some(mkMethodRef setter)

        let getter =
            match property.Getter with
            | null -> None
            | getter -> Some(mkMethodRef getter)

        let customAttrs = mkCustomAttributes property |> mkILCustomAttrs

        ILPropertyDef(name, attrs, setter, getter, callConv, propertyType, init, args, customAttrs)

    let usingTypeElement (typeName: IClrTypeName) defaultValue f =
        use cookie = ReadLockCookie.Create()
        use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

        let typeElement = symbolScope.GetTypeElementByCLRName(typeName)
        if isNull typeElement then defaultValue else

        currentTypeName <- typeName
        currentTypeUnresolvedUsedNames <- HashSet()

        f typeElement

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

    let mkMethods (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let methods =
            [| for method in typeElement.GetMembers().OfType<IFunction>() do
                 yield mkMethod method |]

        table.Methods <- methods
        methods

    let mkFields (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let fields =
            match typeElement with
            | :? IEnum as e -> e.EnumMembers
            | _ -> typeElement.GetMembers().OfType<IField>()

        let fields =
            [ for field in fields do
                yield mkField field ]

        let fields = 
            match typeElement with
            | :? IEnum as enum -> mkEnumInstanceValue enum :: fields
            | _ -> fields
        
        table.Fields <- fields
        fields

    let mkProperties (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let properties = 
            [ for property in typeElement.Properties do
                yield mkProperty property ]

        table.Properties <- properties
        properties

    let mkEvents (table: FcsTypeDefMembers) (typeElement: ITypeElement) =
        let events =
            [ for event in typeElement.Events do
                yield mkEvent event ]

        table.Events <- events
        events

    let getOrCreateMethods (typeName: IClrTypeName) =
        getOrCreateMembers typeName EmptyArray.Instance (fun members -> members.Methods) mkMethods

    let getOrCreateFields (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Fields) mkFields

    let getOrCreateProperties (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Properties) mkProperties

    let getOrCreateEvents (typeName: IClrTypeName) =
        getOrCreateMembers typeName [] (fun members -> members.Events) mkEvents

    member this.CreateAllTypeDefs(): unit =
        // todo: keep upToDate/dirty flag, don't do this if not needed
        let rec traverseTypes (ns: INamespace) =
            for typeElement in ns.GetNestedTypeElements(symbolScope) do
                visitType typeElement
            for nestedNs in ns.GetNestedNamespaces(symbolScope) do
                traverseTypes nestedNs

        and visitType (typeElement: ITypeElement) =
            this.CreateTypeDef(typeElement.GetClrName().GetPersistent()) |> ignore
            Seq.iter visitType typeElement.NestedTypes

        traverseTypes symbolScope.GlobalNamespace

    member this.CreateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()

        match typeDefs.TryGetValue(clrTypeName) with
        | NotNull typeDef -> typeDef.TypeDef
        | _ ->

        currentTypeName <- clrTypeName
        currentTypeUnresolvedUsedNames <- HashSet()

        use cookie = ReadLockCookie.Create()
        use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

        match symbolScope.GetTypeElementByCLRName(clrTypeName) with
        | null ->
            // The type doesn't exist in the module anymore.
            // The project has likely changed and FCS will invalidate cache for this module.
            mkDummyTypeDef clrTypeName.ShortName

        // For multiple types with the same name we'll get some random/first one here.
        // todo: add a test case
        | typeElement ->
            let name =
                match typeElement.GetContainingType() with
                | null -> clrTypeName.FullName
                | _ -> ProjectFcsModuleReader.mkNameFromClrTypeName clrTypeName

            let typeAttributes = mkTypeAttributes typeElement
            let extends = extends typeElement

            let implements =
                [ for declaredType in typeElement.GetSuperTypesWithoutCircularDependent() do
                    if declaredType.GetTypeElement() :? IInterface then
                        mkType declaredType ]

            let nestedTypes =
                let preTypeDefs =
                    [| for typeElement in typeElement.NestedTypes do
                        PreTypeDef(typeElement, this) :> ILPreTypeDef |]
                mkILTypeDefsComputed (fun _ -> preTypeDefs)

            let genericParams =
                let typeParameters = typeElement.GetAllTypeParameters().ResultingList()
                [ for i in typeParameters.Count - 1 .. -1 .. 0 do
                    mkGenericParameterDef typeParameters[i] ]

            let methods = mkILMethodsComputed (fun _ -> getOrCreateMethods clrTypeName)
            let fields = mkILFieldsLazy (lazy getOrCreateFields clrTypeName)
            let properties = mkILPropertiesLazy (lazy getOrCreateProperties clrTypeName)
            let events = mkILEventsLazy (lazy getOrCreateEvents clrTypeName)

            let hasExtensions =
                let typeElement = typeElement.As<TypeElement>()
                if isNull typeElement then false else

                typeElement.EnumerateParts()
                |> Seq.exists (fun part -> not (Array.isEmpty part.ExtensionMethodInfos))

            let customAttrs =
                let customAttributes = mkCustomAttributes typeElement
                [ yield! customAttributes
                  if hasExtensions then
                      extensionAttribute () ]
                |> mkILCustomAttrs

            let typeDef =
                ILTypeDef(name, typeAttributes, ILTypeDefLayout.Auto, implements, genericParams,
                    extends, methods, nestedTypes, fields, emptyILMethodImpls, events, properties,
                    emptyILSecurityDecls, customAttrs)

            let fcsTypeDef = 
                { TypeDef = typeDef
                  Members = Unchecked.defaultof<_> }

            currentTypeName <- null
            currentTypeUnresolvedUsedNames <- null

            clrNamesByShortNames.Add(typeElement.ShortName, clrTypeName)
            typeDefs[clrTypeName] <- fcsTypeDef
            typeDef

    member this.InvalidateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()
        match typeDefs.TryRemove(clrTypeName) with
        | true, _ ->
            moduleDef <- None
            timestamp <- DateTime.UtcNow
        | _ -> ()

    interface IProjectFcsModuleReader with
        member this.ILModuleDef =
            match moduleDef with
            | Some(moduleDef) -> moduleDef
            | None ->

            use readLockCookie = ReadLockCookie.Create()
            use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

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
                     | s -> internalsVisibleToAttribute s |]

            let newModuleDef =
                if ivtAttributes.IsEmpty() then newModuleDef else

                let attrs = mkILCustomAttrsFromArray ivtAttributes |> storeILCustomAttrs
                let manifest = { newModuleDef.Manifest.Value with CustomAttrsStored = attrs }
                { newModuleDef with Manifest = Some(manifest) }

            moduleDef <- Some(newModuleDef)
            newModuleDef

        member this.Dispose() =
            match realModuleReader with
            | Some(moduleReader) -> moduleReader.Dispose()
            | _ -> ()

        member this.ILAssemblyRefs = []

        member this.Timestamp =
            shim.InvalidateDirty()
            timestamp

        member this.Path = path
        member this.PsiModule = psiModule

        member val RealModuleReader = None with get, set

        member this.InvalidateReferencingTypes(shortName) =
            for referencingTypeName in usedShortNamesToUsingTypes.GetValuesSafe(shortName) do
                this.InvalidateTypeDef(referencingTypeName)

        member this.InvalidateTypesReferencingFSharpModule(fsharpProjectModule: IPsiModule) =
            for referencingTypeName in usedFSharpModulesToTypes.GetValuesSafe(fsharpProjectModule) do
                this.InvalidateTypeDef(referencingTypeName)

        member this.InvalidateTypeDef(typeName) =
            this.InvalidateTypeDef(typeName)

        member this.CreateAllTypeDefs() =
            this.CreateAllTypeDefs()


type PreTypeDef(clrTypeName: IClrTypeName, reader: ProjectFcsModuleReader) =
    new (typeElement: ITypeElement, reader: ProjectFcsModuleReader) =
        PreTypeDef(typeElement.GetClrName().GetPersistent(), reader) // todo: intern

    interface ILPreTypeDef with
        member x.Name =
            let typeName = clrTypeName.TypeNames.Last() // todo: use clrTypeName.ShortName ? (check type params)
            ProjectFcsModuleReader.mkNameFromTypeNameAndParamsNumber typeName

        member x.Namespace =
            if not (clrTypeName.TypeNames.IsSingle()) then [] else
            clrTypeName.NamespaceNames |> List.ofSeq

        member x.GetTypeDef() =
            reader.CreateTypeDef(clrTypeName)
