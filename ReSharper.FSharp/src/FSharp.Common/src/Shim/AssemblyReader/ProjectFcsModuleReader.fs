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
open JetBrains.ReSharper.Psi.Impl.Special
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
            typeParameterCountStrings.[paramsCount]

        name + paramsCountString

    let mkNameFromTypeNameAndParamsNumber (nameAndParametersCount: TypeNameAndTypeParameterNumber) =
        mkTypeName nameAndParametersCount.TypeName nameAndParametersCount.TypeParametersNumber

    let mkNameFromClrTypeName (clrTypeName: IClrTypeName) =
        mkTypeName clrTypeName.ShortName clrTypeName.TypeParametersCount

type ProjectFcsModuleReader(psiModule: IPsiModule, _cache: FcsModuleReaderCommonCache) =
    // todo: is it safe to keep symbolScope?
    let symbolScope = psiModule.GetPsiServices().Symbols.GetSymbolScope(psiModule, false, true)

    let locker = JetFastSemiReenterableRWLock()

    let mutable moduleDef: ILModuleDef option = None

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, ILTypeDef>()

//    let usedTypeNames = Dictionary<string, IClrTypeName>()

    // todo: store in reader/cache, so it doesn't leak after solution close
    let cultures = DataIntern()
    let publicKeys = DataIntern()
    let literalValues = DataIntern()

    let assemblyRefs = ConcurrentDictionary<AssemblyNameInfo, ILScopeRef>()

    /// References to types in the same module.
    let localTypeRefs = ConcurrentDictionary<IClrTypeName, ILTypeRef>()

    /// References to types in a different assemblies (currently keyed by primary psi module).
    let assemblyTypeRefs = ConcurrentDictionary<IPsiModule, ConcurrentDictionary<IClrTypeName, ILTypeRef>>()

    let getAssemblyTypeRefCache (targetModule: IPsiModule) =
        let mutable cache = Unchecked.defaultof<_>
        match assemblyTypeRefs.TryGetValue(targetModule, &cache) with
        | true -> cache
        | _ ->

        let cache = ConcurrentDictionary()
        assemblyTypeRefs.[targetModule] <- cache
        cache

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

    let getAssemblyScopeRef (assemblyName: AssemblyNameInfo): ILScopeRef =
        let mutable scopeRef = Unchecked.defaultof<_>
        match assemblyRefs.TryGetValue(assemblyName, &scopeRef) with
        | true -> scopeRef
        | _ ->

        let assemblyRef = ILScopeRef.Assembly(createAssemblyScopeRef assemblyName)
        assemblyRefs.[assemblyName] <- assemblyRef
        assemblyRef

    let mkILScopeRef (targetModule: IPsiModule): ILScopeRef =
        if psiModule == targetModule then ILScopeRef.Local else

        let assemblyName =
            match targetModule.ContainingProjectModule with
            | :? IAssembly as assembly -> assembly.AssemblyName
            | :? IProject as project -> project.GetOutputAssemblyNameInfo(targetModule.TargetFrameworkId)
            | _ -> failwithf $"mkIlScopeRef: {psiModule} -> {targetModule}"

        getAssemblyScopeRef assemblyName

    let mkTypeRef (typeElement: ITypeElement) =
        let clrTypeName = typeElement.GetClrName()
        let targetModule = typeElement.Module

        let typeRefCache =
            if psiModule == targetModule then localTypeRefs else
            getAssemblyTypeRefCache targetModule

        let mutable typeRef = Unchecked.defaultof<_>
        match typeRefCache.TryGetValue(clrTypeName, &typeRef) with
        | true -> typeRef
        | _ ->

        let scopeRef = mkILScopeRef targetModule

        let typeRef =
            if psiModule != targetModule && localTypeRefs.TryGetValue(clrTypeName, &typeRef) then
                ILTypeRef.Create(scopeRef, typeRef.Enclosing, typeRef.Name) else

            let containingType = typeElement.GetContainingType()

            let enclosingTypes =
                match containingType with
                | null -> []
                | _ ->

                let enclosingTypeNames =
                    containingType.GetClrName().TypeNames
                    |> List.ofSeq
                    |> List.map ProjectFcsModuleReader.mkNameFromTypeNameAndParamsNumber

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

            ILTypeRef.Create(scopeRef, enclosingTypes, name)

//        typeRefCache.[clrTypeName.GetPersistent()] <- typeRef
        typeRef

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
                // todo: store unresolved type short name to invalidate the type def when that type appears
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
                    |> List.map (fun typeParameter -> mkType substitution.[typeParameter])

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
            method.Parameters
            |> List.ofSeq
            |> List.map (fun param -> mkType param.Type)

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

    let mkCompilerGeneratedAttribute (attrType: IClrTypeName): ILAttribute =
        let attrType = TypeFactory.CreateTypeByCLRName(attrType, NullableAnnotation.Unknown, psiModule)

        match attrType.GetTypeElement() with
        | null -> failwithf $"getting param array type element in {psiModule}" // todo: safer handling
        | typeElement ->

        let attrType = mkType attrType
        let ctor = typeElement.Constructors.First(fun ctor -> ctor.IsParameterless) // todo: safer handling
        let ctorMethodRef = mkMethodRef ctor

        let methodSpec = ILMethodSpec.Create(attrType, ctorMethodRef, [])
        ILAttribute.Decoded(methodSpec, [], [])

    let paramArrayAttribute () =
        mkCompilerGeneratedAttribute PredefinedType.PARAM_ARRAY_ATTRIBUTE_CLASS

    let extensionAttribute () =
        mkCompilerGeneratedAttribute PredefinedType.EXTENSION_ATTRIBUTE_CLASS

    let mkGenericVariance (variance: TypeParameterVariance): ILGenericVariance =
        match variance with
        | TypeParameterVariance.IN -> ILGenericVariance.ContraVariant
        | TypeParameterVariance.OUT -> ILGenericVariance.CoVariant
        | _ -> ILGenericVariance.NonVariant

    // todo: test with same name parameter

    let mkGenericParameterDef (typeParameter: ITypeParameter): ILGenericParameterDef =
        let typeConstraints =
            typeParameter.TypeConstraints
            |> List.ofSeq
            |> List.map mkType

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
        let unbox f = unbox >> f
        [| PredefinedType.BOOLEAN_FQN, unbox ILFieldInit.Bool
           PredefinedType.CHAR_FQN,    unbox ILFieldInit.Char
           PredefinedType.SBYTE_FQN,   unbox ILFieldInit.Int8
           PredefinedType.BYTE_FQN,    unbox ILFieldInit.UInt8
           PredefinedType.SHORT_FQN,   unbox ILFieldInit.Int16
           PredefinedType.USHORT_FQN,  unbox ILFieldInit.UInt16
           PredefinedType.INT_FQN,     unbox ILFieldInit.Int32
           PredefinedType.UINT_FQN,    unbox ILFieldInit.UInt32
           PredefinedType.LONG_FQN,    unbox ILFieldInit.Int64
           PredefinedType.ULONG_FQN,   unbox ILFieldInit.UInt64
           PredefinedType.FLOAT_FQN,   unbox ILFieldInit.Single
           PredefinedType.DOUBLE_FQN,  unbox ILFieldInit.Double |]
        |> dict

    let nullLiteralValue = Some(ILFieldInit.Null)

    // todo: cache

    let getLiteralValue (value: ConstantValue) (valueType: IType): ILFieldInit option =
        if value.IsBadValue() then None else
        if value.IsNull() then nullLiteralValue else

        // A separate case to prevent interning string literals.
        if value.IsString() then Some(ILFieldInit.String(unbox value.Value)) else

        match valueType with
        | :? IDeclaredType as declaredType ->
            let mutable literalType = Unchecked.defaultof<_>
            match literalTypes.TryGetValue(declaredType.GetClrName(), &literalType) with
            | true -> literalValues.Intern(Some(literalType value.Value))
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
        let customAttrs = emptyILCustomAttrs // todo

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
        method.Parameters
        |> List.ofSeq
        |> List.map mkParam

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
                method.TypeParameters
                |> List.ofSeq
                |> List.map mkGenericParameterDef
            | _ -> []

        // todo: other attrs
        let attrs =
            match method with
            | :? IMethod as method when method.IsExtensionMethod -> [ extensionAttribute () ] // todo: test
            | _ -> []
            |> mkILCustomAttrs

        let implAttributes = MethodImplAttributes.Managed
        let body = methodBodyUnavailable
        let securityDecls = emptyILSecurityDecls
        let isEntryPoint = false

        ILMethodDef(name, methodAttrs, implAttributes, callingConv, parameters, ret, body, isEntryPoint, genericParams,
             securityDecls, attrs)

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
        let customAttrs = emptyILCustomAttrs
        ILEventDef(eventType, name, attributes, addMethod, removeMethod, fireMethod, otherMethods, customAttrs)

    let mkProperty (property: IProperty): ILPropertyDef =
        let name = property.ShortName

        let attrs = enum 0 // todo
        let callConv = mkCallingThisConv property
        let propertyType = mkType property.Type
        let init = None // todo

        let args =
            property.Parameters
            |> List.ofSeq
            |> List.map (fun p -> mkType p.Type)

        let setter =
            match property.Setter with
            | null -> None
            | setter -> Some(mkMethodRef setter)

        let getter =
            match property.Getter with
            | null -> None
            | getter -> Some(mkMethodRef getter)

        ILPropertyDef(name, attrs, setter, getter, callConv, propertyType, init, args, emptyILCustomAttrs)

    member this.Timestamp = timestamp
    member this.PsiModule = psiModule

    member val RealModuleReader: ILModuleReader option = None with get, set

    member this.ForceCreateTypeDefs(): unit =
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
        | NotNull typeDef -> typeDef
        | _ ->

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
                typeElement.GetSuperTypesWithoutCircularDependent()
                |> Array.filter (fun t -> t.GetTypeElement() :? IInterface)
                |> Array.map mkType
                |> Array.toList

            let methods =
                typeElement.GetMembers().OfType<IFunction>()
                |> List.ofSeq
                |> List.map mkMethod
                |> mkILMethods

            let nestedTypes =
                let preTypeDefs =
                    typeElement.NestedTypes
                    |> Array.ofSeq
                    |> Array.map (fun typeElement -> PreTypeDef(typeElement, this) :> ILPreTypeDef)
                mkILTypeDefsComputed (fun _ -> preTypeDefs)

            let genericParams =
                typeElement.GetAllTypeParameters().ResultingList()
                |> List.ofSeq
                |> List.rev
                |> List.map mkGenericParameterDef

            let fields =
                let fields =
                    match typeElement with
                    | :? IEnum as e -> e.EnumMembers
                    | _ -> typeElement.Fields |> Seq.append typeElement.Constants

                let fields =
                    fields
                    |> List.ofSeq
                    |> List.map mkField

                let fieldDefs =
                    match typeElement with
                    | :? IEnum as enum -> mkEnumInstanceValue enum :: fields
                    | _ -> fields

                match fieldDefs with
                | [] -> emptyILFields
                | _ -> mkILFields fieldDefs

            let properties =
                typeElement.Properties
                |> List.ofSeq
                |> List.map mkProperty
                |> mkILProperties

            let events =
                typeElement.Events
                |> List.ofSeq
                |> List.map mkEvent
                |> mkILEvents

            let typeDef =
                ILTypeDef(name, typeAttributes, ILTypeDefLayout.Auto, implements, genericParams,
                    extends, methods, nestedTypes, fields, emptyILMethodImpls, events, properties,
                    emptyILSecurityDecls, emptyILCustomAttrs)

            typeDefs.[clrTypeName] <- typeDef
            typeDef

    // todo: change to shortName
    member this.InvalidateTypeDef(clrTypeName: IClrTypeName) =
        use lock = locker.UsingWriteLock()
        typeDefs.TryRemove(clrTypeName) |> ignore
        moduleDef <- None
        timestamp <- DateTime.UtcNow

    interface ILModuleReader with
        member this.ILModuleDef =
            match moduleDef with
            | Some(moduleDef) -> moduleDef
            | None ->

            use readLockCookie = ReadLockCookie.Create()

            let project = psiModule.ContainingProjectModule :?> IProject
            let moduleName = project.Name
            let assemblyName = project.GetOutputAssemblyName(psiModule.TargetFrameworkId)
            let isDll = isDll project psiModule.TargetFrameworkId

            let typeDefs =
                let result = List<ILPreTypeDef>()

                let rec addTypes (ns: INamespace) =
                    for typeElement in ns.GetNestedTypeElements(symbolScope) do
                        result.Add(PreTypeDef(typeElement, this))
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

            moduleDef <- Some(newModuleDef)
            newModuleDef

        member this.Dispose() =
            match this.RealModuleReader with
            | Some(moduleReader) -> moduleReader.Dispose()
            | _ -> ()

        member this.ILAssemblyRefs = []


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
