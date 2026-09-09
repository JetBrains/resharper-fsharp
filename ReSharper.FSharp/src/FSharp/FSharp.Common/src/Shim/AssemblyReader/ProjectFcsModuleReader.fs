namespace rec JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Generic
open System.Linq
open System.Collections.Concurrent
open System.Reflection
open FSharp.Compiler.AbstractIL.IL
open FSharp.Compiler.AbstractIL.ILBinaryReader
open Internal.Utilities.Library
open JetBrains.Application
open JetBrains.Application.Threading
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
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds
open JetBrains.Util.Logging

[<AutoOpen>]
module ProjectFcsModuleReader =
    let inline forallPaired ([<InlineIfLambda>] isSame: 'a -> 'b -> bool) (first: seq<'a>) (second: seq<'b>) =
        use firstItems = first.GetEnumerator()
        use secondItems = second.GetEnumerator()

        let mutable result = true
        let mutable goOn = true

        while goOn do
            let hasFirst = firstItems.MoveNext()
            let hasSecond = secondItems.MoveNext()

            if hasFirst <> hasSecond then
                result <- false
                goOn <- false
            elif not hasFirst then
                goOn <- false
            else
                result <- isSame firstItems.Current secondItems.Current
                goOn <- result

        result

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


    [<Struct>]
    type LocalReadWriteLockCookie(locker: JetFastSemiReenterableRWLock) =
        interface IDisposable with
            member this.Dispose() =
                locker.Release()


type FcsTypeDefMemberTables =
    { Methods: ILMethodDef[]
      Fields: ILFieldDef list
      Events: ILEventDef list
      Properties: ILPropertyDef list }

    static member Create() =
        { Fields = Unchecked.defaultof<_>
          Methods = Unchecked.defaultof<_>
          Events = Unchecked.defaultof<_>
          Properties = Unchecked.defaultof<_> }


type FcsTypeDefMembers =
    { mutable MemberTables: FcsTypeDefMemberTables
      mutable NestedTypes: ILPreTypeDef[] }

    static member Create() =
        { MemberTables = Unchecked.defaultof<_>
          NestedTypes = Unchecked.defaultof<_> }

type FcsTypeDef =
    { TypeDef: ILTypeDef
      mutable Members: FcsTypeDefMembers }


type FcsModuleReaderCompilerGeneratedType(clrTypeName, psiModule) =
    inherit DeclaredTypeFromCLRName(clrTypeName, psiModule)

    override this.ChooseBestCandidate(candidates) =
        let frameworkCandidates =
            candidates
            |> Seq.filter (fun candidate -> 
                match candidate.Module with
                | :? IAssemblyPsiModule as assemblyPsiModule -> assemblyPsiModule.Assembly.IsFrameworkAssembly
                | _ -> false
            )
            |> Seq.toArray

        match frameworkCandidates with
        | [| candidate |] -> candidate
        | [||] -> base.ChooseBestCandidate(candidates)
        | _ ->

        match base.ChooseBestCandidate(frameworkCandidates) with
        | null -> base.ChooseBestCandidate(candidates)
        | typeElement -> typeElement


type ProjectFcsModuleReader(psiModule: IPsiModule, cache: FcsModuleReaderCommonCache, path,
        shim: IFcsAssemblyReaderShim, realReader: ILModuleReader option) =
    let locks = psiModule.GetPsiServices().Locks

    let getSymbolScope () =
        // todo: make it safe to cache symbol scope in R#
        let symbolCache = psiModule.GetPsiServices().Symbols
        symbolCache.GetSymbolScope(psiModule, false, true)

    let locker = JetFastSemiReenterableRWLock()

    let usingWriteLock () =
        let mutable cookie = ValueNone

        while cookie.IsNone do
            if locker.TryAcquireWrite() then
                cookie <- ValueSome(new LocalReadWriteLockCookie(locker))
            elif locks.IsReadAccessAllowed() then
                FSharpAsyncUtil.ProcessEnqueuedReadRequests()

        cookie.Value

    let mutable isDirty = false

    let mutable isNullnessEnabled = false

    /// The types that have already been checked in isUpToDate check 
    let mutable upToDateCheckedTypes = null

    /// Set together with `upToDateChecked` set.
    /// It's placed outside `isUpToDate` to keep its state if an interruption happens during checking. 
    let mutable seenOutdatedTypes = false

    let mutable moduleDef: (ILModuleDef * ILPreTypeDef[]) option = None
    let mutable realModuleReader: ILModuleReader option = realReader

    // Initial timestamp should be earlier than any modifications observed by FCS.
    let mutable timestamp = DateTime.MinValue

    /// Type definitions imported by FCS.
    let typeDefs = ConcurrentDictionary<IClrTypeName, FcsTypeDef>() // todo: use non-concurrent, add locks
    let clrNamesByShortNames = CompactOneToSetMap<string, IClrTypeName>()

    let markDirty () =
        shim.Logger.Trace("Mark dirty: {0}", path)
        isDirty <- true
        upToDateCheckedTypes <- null
        seenOutdatedTypes <- false

    let readData f =
        FSharpAsyncUtil.CheckAndThrow()
        let logger = Logger.GetLogger<ProjectFcsModuleReader>()

        logger.Trace("readData: before UsingReadLockInsideFcs");
        FSharpAsyncUtil.UsingReadLockInsideFcs(locks, (fun _ ->
                locks.AssertReadAccessAllowed()

                FSharpAsyncUtil.CheckAndThrow()
                logger.Trace("readData: action: inside lambda");

                use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

                // Reading data on incomplete caches, e.g. when an assembly metadata is being imported.
                // Resolve results may be incorrect.
                // Mark this module dirty, so the data is invalidated before next FCS request. 
                if not (psiModule.GetPsiServices().CachesState.IsIdle.Value) then
                    logger.Trace("readData: action: marking as dirty");
                    shim.MarkDirty(psiModule)

                logger.Trace("readData: action: before f");
                f ()
                logger.Trace("readData: action: after f");
            )
        )
        logger.Trace("readData: after UsingReadLockInsideFcs");

        // The whole FCS request could've been cancelled and the call above silently exited.
        // We need to throw the exception to make FCS handle it properly.
        FSharpAsyncUtil.CheckAndThrow()
        logger.Trace("readData: after CheckAndThrow");

    let isDll (project: IProject) (targetFrameworkId: TargetFrameworkId) =
        let projectProperties = project.ProjectProperties
        match projectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
        | :? IManagedProjectConfiguration as cfg -> cfg.OutputType = ProjectOutputType.LIBRARY
        | _ -> false

    let mkDummyModuleDef () : ILModuleDef =
        // Should only be used as a recovery when the module is already invalid (e.g. the project is unloaded, etc.)
        assert not (psiModule.IsValid())

        let name = psiModule.Name
        let typeDefs = mkILTypeDefs []
        let flags = 0
        let exportedTypes = mkILExportedTypes []

        mkILSimpleModule
            name name true
            DummyValues.subsystemVersion
            DummyValues.useHighEntropyVA
            typeDefs
            None None flags exportedTypes
            DummyValues.metadataVersion

    let mkDummyTypeDef (name: string) =
        let attributes = enum 0
        let layout = ILTypeDefLayout.Auto
        let implements = []
        let genericParams = []
        let extends = None
        let nestedTypes = emptyILTypeDefs

        ILTypeDef(name, attributes, layout, implements, genericParams, extends, emptyILMethods, nestedTypes,
             emptyILFields, emptyILMethodImpls, emptyILEvents, emptyILProperties,
             emptyILSecurityDecls, emptyILCustomAttrsStored)

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

    let createAssemblyRef (assemblyName: AssemblyNameInfo) =
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

    let mkAssemblyScopeRef (assemblyName: AssemblyNameInfo) =
        let mutable scopeRef = Unchecked.defaultof<_>
        match cache.AssemblyRefs.TryGetValue(assemblyName, &scopeRef) with
        | true -> scopeRef
        | _ ->

        let assemblyRef = ILScopeRef.Assembly(createAssemblyRef assemblyName)
        cache.AssemblyRefs[assemblyName] <- assemblyRef
        assemblyRef

    let mkILScopeRef (targetModule: IPsiModule): ILScopeRef =
        if psiModule == targetModule then ILScopeRef.Local else

        let assemblyName =
            match targetModule.ContainingProjectModule with
            | :? IAssembly as assembly -> assembly.AssemblyName
            | :? IProject as project -> project.GetOutputAssemblyNameInfo(targetModule.TargetFrameworkId)
            | _ -> failwithf $"mkIlScopeRef: {psiModule} -> {targetModule}"

        mkAssemblyScopeRef assemblyName


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
                    mkNameFromTypeNameAndParamsNumber name ]

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
            | _ -> mkNameFromClrTypeName clrTypeName

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
            index <- index + parent.TypeParametersCount
            parent <- parent.GetContainingType()
        index

    let getTypeArgs (resolveResult: IResolveResult) =
        let substitution = resolveResult.Substitution
        let domain = substitution.Domain
        if domain.IsEmpty() then [] else

        domain
        |> List.ofSeq
        |> List.sortBy getGlobalIndex
        |> List.map (fun typeParameter -> substitution[typeParameter])

    let staticCallingConv = Callconv(ILThisConvention.Static, ILArgConvention.Default)
    let instanceCallingConv = Callconv(ILThisConvention.Instance, ILArgConvention.Default)

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
                let typeArgs = getTypeArgs resolveResult |> List.map mkType
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

        | :? IFunctionPointerType as functionPointerType ->
            let argTypes =
                [ for param in functionPointerType.Parameters do
                    mkParameterType param.Kind param.Type ]

            let returnType =
                mkReturnParameterType functionPointerType.ReturnKind functionPointerType.ReturnType

            ILType.FunctionPointer
                { CallingConv = staticCallingConv
                  ArgTypes = argTypes
                  ReturnType = returnType }

        | _ -> failwithf $"mkType: type: {t}"

    and mkParameterType (kind: ParameterKind) (t: IType): ILType =
        let ilType = mkType t
        if kind.IsByReference() then ILType.Byref ilType else ilType

    and mkReturnParameterType (kind: ReferenceKind) (t: IType) =
        mkParameterType (kind.ToParameterKind()) t

    and mkParameterOwnerReturnType (owner: IParametersOwner) =
        mkReturnParameterType owner.ReturnKind owner.ReturnType

    and mkUnresolvedType (psiModule: IPsiModule) =
        let objType = psiModule.GetPredefinedType().Object
        if objType.IsResolved then
            mkType objType
        else
            // todo: make a typeRef to System.Object in primary assembly
            ILType.Void

    let isInitOnlySetter (method: IFunction) =
        match method with
        | :? IAccessor as accessor -> accessor.IsInitOnly
        | _ -> false

    let getExternalInitTypeElement () =
        FcsModuleReaderCompilerGeneratedType(PredefinedType.IS_EXTERNAL_INIT_FQN, psiModule).GetTypeElement()

    let mkParamType (param: IParameter) =
        mkParameterType param.Kind param.Type

    /// FCS finds an init only setter through the `IsExternalInit` modifier on the return type.
    /// `infos.fs`, `HasExternalInit`.
    let mkReturnType (method: IFunction) =
        let returnType = mkParameterOwnerReturnType method
        if not (isInitOnlySetter method) then returnType else

        match getExternalInitTypeElement () with
        | null -> returnType
        | typeElement -> ILType.Modified(true, mkTypeRef typeElement, returnType)

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
                mkParamType parameter ]

        let returnType = mkReturnType method

        ILMethodRef.Create(typeRef, callingConv, name, typeParamsCount, paramTypes, returnType)

    let getBaseType (typeElement: ITypeElement) =
        let predefinedType = psiModule.GetPredefinedType()

        match typeElement with
        | :? IClass as c ->
            match c.GetBaseClassType() with
            | null -> predefinedType.Object
            | baseType -> baseType

        | :? IEnum -> predefinedType.Enum
        | :? IStruct -> predefinedType.ValueType
        | :? IDelegate -> predefinedType.MulticastDelegate
        | _ -> null

    let getInterfaces (typeElement: ITypeElement) =
        typeElement.GetSuperTypesWithoutCircularDependent()
        |> Seq.filter (fun declaredType -> declaredType.GetTypeElement() :? IInterface)

    let mkTypeDefExtends (typeElement: ITypeElement) =
        // todo: intern
        match getBaseType typeElement with
        | null -> None
        | baseType -> Some(mkType baseType)

    let mkCompilerGeneratedAttribute (attrTypeName: IClrTypeName) (args: ILAttribElem list) : ILAttribute option =
        let attrType = FcsModuleReaderCompilerGeneratedType(attrTypeName, psiModule)

        match attrType.GetTypeElement() with
        | null -> None
        | typeElement ->

        typeElement.Constructors
        |> Seq.tryFind (fun ctor -> ctor.Parameters.Count = args.Length)
        |> Option.map (fun ctor ->
            ILAttribute.Decoded(ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, []), args, [])
        )

    let mkCompilerGeneratedAttributeNoArgs (attrTypeName: IClrTypeName): ILAttribute option =
        mkCompilerGeneratedAttribute attrTypeName []

    let isCompilerGeneratedAttribute (attrTypeName: IClrTypeName) (ilAttr: ILAttribute) =
        match ilAttr with
        | ILAttribute.Decoded(methodSpec, [], []) ->
            methodSpec.MethodRef.DeclaringTypeRef.Name = attrTypeName.FullName
        | _ -> false

    let mkParamArrayAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.PARAM_ARRAY_ATTRIBUTE_CLASS

    let mkExtensionAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.EXTENSION_ATTRIBUTE_CLASS

    let mkIsReadOnlyAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.IS_READ_ONLY_ATTRIBUTE_FQN

    let mkIsByRefLikeAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.IS_BY_REF_LIKE_ATTRIBUTE_FQN

    let mkRequiredMemberAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.REQUIRED_MEMBER_ATTRIBUTE_FQN

    let getNullnessByte (t: IType) =
        match t.NullableAnnotation with
        | NullableAnnotation.NotAnnotated -> 1uy
        | NullableAnnotation.Annotated -> 2uy
        | _ -> 0uy

    let rec addNullnessFlags (flags: List<byte>) (t: IType) =
        if t.IsVoid() then () else

        if not t.IsResolved then flags.Add(getNullnessByte t) else

        match t with
        | :? IDeclaredType as declaredType ->
            let resolveResult = declaredType.Resolve()

            match resolveResult.DeclaredElement with
            | :? ITypeParameter -> flags.Add(getNullnessByte t)
            | :? ITypeElement as typeElement ->
                let typeArgs = getTypeArgs resolveResult

                match typeElement with
                | :? IEnum
                | :? IStruct ->
                    if not (List.isEmpty typeArgs) && not (declaredType.IsNullable()) then
                        flags.Add(0uy)
                | _ -> flags.Add(getNullnessByte t)

                for typeArg in typeArgs do
                    addNullnessFlags flags typeArg

            | _ -> ()

        | :? IArrayType as arrayType ->
            flags.Add(getNullnessByte t)
            addNullnessFlags flags arrayType.ElementType

        | :? IPointerType as pointerType ->
            addNullnessFlags flags pointerType.ElementType

        | _ -> ()

    let nullableAttributes =
        [| for value in 0uy .. 2uy ->
            InterruptibleLazy(fun _ ->
                mkCompilerGeneratedAttribute PredefinedType.NULLABLE_ATTRIBUTE_FQN [ ILAttribElem.Byte value ]) |]

    let getNullness (t: IType) =
        if not isNullnessEnabled || isNull t then None else

        let flags = List<byte>()
        addNullnessFlags flags t

        if flags.Count = 0 then None else

        let first = flags[0]
        if Seq.forall (fun flag -> flag = first) flags then
            if first = 0uy then None else Some(ILAttribElem.Byte first)
        else

        let byteIlType = mkType (psiModule.GetPredefinedType().Byte)
        Some(ILAttribElem.Array(byteIlType, [ for flag in flags -> ILAttribElem.Byte flag ]))

    let getTypeParameterNullness (typeParameter: ITypeParameter) =
        if not isNullnessEnabled then None else

        match typeParameter.Nullability with
        | TypeParameterNullability.NotNullableReferenceType
        | TypeParameterNullability.NotNullableValueOrReferenceType -> Some(ILAttribElem.Byte 1uy)
        | _ -> None

    let mkNullableAttribute (nullness: ILAttribElem option) =
        match nullness with
        | Some(ILAttribElem.Byte value) -> nullableAttributes[int value].Value
        | Some arg -> mkCompilerGeneratedAttribute PredefinedType.NULLABLE_ATTRIBUTE_FQN [arg]
        | None -> None

    let mkTypeDefImplements (typeElement: ITypeElement) =
        [ for declaredType in getInterfaces typeElement do
            match mkNullableAttribute (getNullness declaredType) with
            | None -> InterfaceImpl.Create(mkType declaredType)
            | Some attribute ->
                let attrs = storeILCustomAttrs (mkILCustomAttrs [attribute])
                InterfaceImpl.Create(mkType declaredType, attrs) ]

    let mkIsUnmanagedAttribute () =
        mkCompilerGeneratedAttributeNoArgs PredefinedType.IS_UNMANAGED_ATTRIBUTE_FQN

    let mkDefaultMemberAttribute name =
        mkCompilerGeneratedAttribute PredefinedType.DEFAULT_MEMBER_ATTRIBUTE_CLASS [ ILAttribElem.String(Some(name)) ]

    let mkInternalsVisibleToAttribute arg =
        mkCompilerGeneratedAttribute PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS [ ILAttribElem.String(Some(arg)) ]

    let internalsVisibleToNames () =
        psiModule.GetPsiServices().Symbols
            .GetModuleAttributes(psiModule)
            .GetAttributeInstances(PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS, false)
        |> Seq.choose (fun instance ->
            match instance.PositionParameter(0).ConstantValue.AsString() with
            | null -> None
            | name -> Some name)

    let isUnknownValueType (valueTypes: IDictionary<IClrTypeName, _>) (valueType: IType) =
        // The builders take a string before the table, so a string type is not unknown.
        not (valueType.IsString()) &&

        match valueType.As<IDeclaredType>() with
        | null -> true
        | declaredType -> not (valueTypes.ContainsKey(declaredType.GetClrName()))

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

    let mkAttribElement (attrValue: AttributeValue) =
        let constantValue = attrValue.ConstantValue

        // todo: use default value for type from parameter/property?
        if constantValue.IsBadValue() || constantValue.IsNull() then ILAttribElem.Null else

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

    let attributeNamedParameters (attrInstance: IAttributeInstance) =
        attrInstance.NamedParameters()
        |> Seq.filter (fun (Pair(_, attributeValue)) -> attributeValue.IsConstant)

    let mkCustomAttribute (attrInstance: IAttributeInstance) =
        let ctor = attrInstance.Constructor

        let attrType = TypeFactory.CreateType(ctor.ContainingType)
        let methodSpec = ILMethodSpec.Create(mkType attrType, mkMethodRef ctor, [])

        let positionalArgs =
            attrInstance.PositionParameters()
            |> List.ofSeq
            |> List.map mkAttribElement

        let namedArgs =
            attributeNamedParameters attrInstance
            |> Seq.map (fun (Pair(name, attributeValue)) ->
                let attribElement = mkAttribElement attributeValue
                let valueType = mkType attributeValue.ConstantValue.Type
                name, valueType, true, attribElement)
            |> List.ofSeq

        ILAttribute.Decoded(methodSpec, positionalArgs, namedArgs)

    let mkCustomAttributes (attributesSet: IAttributesSet) =
        attributesSet.GetAttributeInstances(AttributesSource.Self)
        |> List.ofSeq
        |> List.filter (fun attributeInstance -> isNotNull attributeInstance.Constructor)
        |> List.map mkCustomAttribute

    let mkCustomAttributesWithNullness (attributesSet: IAttributesSet) (t: IType) =
        [ match mkNullableAttribute (getNullness t) with
          | Some attribute -> attribute
          | _ -> ()

          yield! mkCustomAttributes attributesSet ]

    let mkGenericVariance (variance: TypeParameterVariance): ILGenericVariance =
        match variance with
        | TypeParameterVariance.IN -> ILGenericVariance.ContraVariant
        | TypeParameterVariance.OUT -> ILGenericVariance.CoVariant
        | _ -> ILGenericVariance.NonVariant

    // todo: test with same name parameter

    let hasDefaultConstructorConstraint (typeParameter: ITypeParameter) =
        typeParameter.HasDefaultConstructor || typeParameter.IsValueType

    let mkGenericParameterDef (typeParameter: ITypeParameter): ILGenericParameterDef =
        let typeConstraints =
            [ for typeConstraint in typeParameter.TypeConstraints do
                mkType typeConstraint ]

        let attributes =
            let attrs =
                [ if typeParameter.IsUnmanagedType then
                      match mkIsUnmanagedAttribute () with
                      | Some attribute -> attribute
                      | _ -> ()

                  match mkNullableAttribute (getTypeParameterNullness typeParameter) with
                  | Some attribute -> attribute
                  | _ -> () ]

            match attrs with
            | [] -> emptyILCustomAttrsStored
            | attrs -> storeILCustomAttrs (mkILCustomAttrs attrs)

        { Name = typeParameter.ShortName
          Constraints = typeConstraints
          Variance = mkGenericVariance typeParameter.Variance
          HasReferenceTypeConstraint = typeParameter.IsReferenceType
          HasNotNullableValueTypeConstraint = typeParameter.IsValueType
          HasDefaultConstructorConstraint = hasDefaultConstructorConstraint typeParameter
          CustomAttrsStored = attributes
          MetadataIndex = NoMetadataIdx
          HasAllowsRefStruct = false } // todo

    /// `GetAllTypeParameters` gives the innermost first, and FCS needs the outermost first.
    let getGenericParameters (typeElement: ITypeElement) =
        let typeParameters = typeElement.GetAllTypeParameters().ResultingList()
        [ for i in typeParameters.Count - 1 .. -1 .. 0 do
            typeParameters[i] ]

    let mkGenericParamDefs (typeElement: ITypeElement) =
        getGenericParameters typeElement |> List.map mkGenericParameterDef

    let rec hasExtensions (typeElement: ITypeElement) =
        let typeElement = typeElement.As<TypeElement>()
        if isNull typeElement then false else

        typeElement.EnumerateParts()
        |> Seq.exists (fun part -> not (Array.isEmpty part.ExtensionMemberInfos)) ||

        typeElement.NestedTypes
        |> Seq.exists hasExtensions

    let isByRefLikeType (typeElement: ITypeElement) =
        match typeElement with
        | :? IStruct as structType -> structType.IsByRefLike
        | _ -> false

    let getIndexerName (typeElement: ITypeElement) =
        typeElement.Properties
        |> Seq.tryFind _.IsDefault
        |> Option.map _.GetDefaultPropertyMetadataName()

    let mkTypeDefCustomAttrs (typeElement: ITypeElement) =
        [| match getIndexerName typeElement with
           | Some name ->
               match mkDefaultMemberAttribute name with
               | Some attribute -> attribute
               | _ -> ()
           | None -> ()

           if isByRefLikeType typeElement then
               match mkIsByRefLikeAttribute () with
               | Some attribute -> attribute
               | _ -> ()

           if hasExtensions typeElement then
               match mkExtensionAttribute () with
               | Some attribute -> attribute
               | _ -> ()

           yield! mkCustomAttributesWithNullness typeElement (getBaseType typeElement) |]

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

    let mkLiteralValue (value: ConstantValue) (valueType: IType) =
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
            let underlyingType = field.Type.GetEnumUnderlying()
            if isNotNull underlyingType then underlyingType else field.Type

        let value = field.ConstantValue
        mkLiteralValue value valueType

    // todo: unfinished field test (e.g. missing `;`)

    let mkFieldDef (field: IField): ILFieldDef =
        let name = field.ShortName
        let attributes = mkFieldAttributes field
        let fieldType = mkType field.Type
        let data = None // todo: check FCS
        let offset = None
        let literalValue = mkFieldLiteralValue field
        let marshal = None
        let customAttrs = mkCustomAttributesWithNullness field field.Type |> mkILCustomAttrs

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
        (if method.IsVirtual || method.IsOverride then MethodAttributes.Virtual else enum 0) ||| // todo: test
        (if not (method.GetHiddenMembers().IsEmpty()) then MethodAttributes.NewSlot else enum 0) ||| // todo: test
        (if method :? IConstructor || method :? IAccessor then MethodAttributes.SpecialName else enum 0)

    let mkParamDefaultValue (param: IParameter) =
        let defaultValue = param.GetDefaultValue()
        if defaultValue.IsBadValue then None else
        mkLiteralValue defaultValue.ConstantValue defaultValue.DefaultTypeValue

    /// FCS makes an `inref` from a byref with this attribute, and reads `in` and `ref readonly` alike.
    let isReadonlyRefParameter (param: IParameter) =
        match param.Kind with
        | ParameterKind.INPUT
        | ParameterKind.READONLY_REFERENCE -> true
        | _ -> false

    let isReadonlyRefReturn (method: IFunction) =
        method.ReturnKind = ReferenceKind.READONLY_REFERENCE

    let mkParam (param: IParameter): ILParameter =
        let name = param.ShortName
        let paramType = mkParamType param
        let defaultValue = mkParamDefaultValue param

        let attrs =
            [ if param.IsParameterArray then
                  match mkParamArrayAttribute () with
                  | Some attribute -> attribute
                  | _ -> ()

              if isReadonlyRefParameter param then
                  match mkIsReadOnlyAttribute () with
                  | Some attribute -> attribute
                  | _ -> ()

              yield! mkCustomAttributesWithNullness param param.Type ]

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
    let methodBodyUnavailable = InterruptibleLazy.FromValue(MethodBody.NotAvailable)

    let isExtensionMethod (method: IFunction) =
        match method with
        | :? IMethod as method -> method.IsExtensionMethod
        | _ -> false

    let mkMethodReturn (method: IFunction) =
        let ret =
            match mkReturnType method with
            | ILType.Void -> voidReturn
            | ilType -> mkILReturn ilType

        let attrs =
            [ if isReadonlyRefReturn method then
                  match mkIsReadOnlyAttribute () with
                  | Some attribute -> attribute
                  | _ -> ()
              yield! mkCustomAttributesWithNullness method.ReturnTypeAttributes method.ReturnType ]

        match attrs with
        | [] -> ret
        | attrs -> ret.WithCustomAttrs(mkILCustomAttrs attrs)

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
            let customAttributes = mkCustomAttributes method
            [ if isExtensionMethod method then
                  match mkExtensionAttribute () with
                  | Some attribute -> attribute
                  | _ -> ()
              yield! customAttributes ]
            |> mkILCustomAttrs

        let implAttributes = MethodImplAttributes.Managed
        let body = methodBodyUnavailable
        let securityDecls = emptyILSecurityDecls
        let isEntryPoint = false

        ILMethodDef(name, methodAttrs, implAttributes, callingConv, parameters, ret, body, isEntryPoint, genericParams,
             securityDecls, customAttrs)

    let getEventType (event: IEvent): IType =
        let eventType = event.Type
        if eventType.IsUnknown then null else eventType

    let getAddAccessor (event: IEvent): IFunction =
        let adder = event.Adder
        if isNotNull adder then adder else ImplicitAccessor(event, AccessorKind.ADDER) :> _

    let getRemoveAccessor (event: IEvent): IFunction =
        let remover = event.Remover
        if isNotNull remover then remover else ImplicitAccessor(event, AccessorKind.REMOVER) :> _

    let mkEventAddMethod (event: IEvent) =
        getAddAccessor event |> mkMethodRef

    let mkEventRemoveMethod (event: IEvent) =
        getRemoveAccessor event |> mkMethodRef

    let mkEventFireMethod (event: IEvent) =
        match event.Raiser with
        | null -> None
        | adder -> Some(mkMethodRef adder)

    let mkEventDef (event: IEvent): ILEventDef =
        let eventType = getEventType event
        let ilEventType = if isNull eventType then None else Some(mkType eventType)
        let name = event.ShortName
        let attributes = enum 0 // Not used by FCS.
        let addMethod = mkEventAddMethod event
        let removeMethod = mkEventRemoveMethod event
        let fireMethod = mkEventFireMethod event
        let otherMethods = []
        let customAttrs = mkCustomAttributesWithNullness event eventType |> mkILCustomAttrs

        ILEventDef(ilEventType, name, attributes, addMethod, removeMethod, fireMethod, otherMethods, customAttrs)

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
        let name = property.GetDefaultPropertyMetadataName()
        let attrs = enum 0 // todo
        let callConv = mkCallingThisConv property
        let propertyType = mkParameterOwnerReturnType property
        let init = None // todo
        let args = mkPropertyParams property
        let setter = mkPropertySetter property
        let getter = mkPropertyGetter property

        let customAttrs =
            [ if property.IsRequired then
                  match mkRequiredMemberAttribute () with
                  | Some attribute -> attribute
                  | _ -> ()

              yield! mkCustomAttributesWithNullness property property.ReturnType ]
            |> mkILCustomAttrs

        ILPropertyDef(name, attrs, setter, getter, callConv, propertyType, init, args, customAttrs)

    let usingTypeElement (typeName: IClrTypeName) defaultValue f =
        let mutable result = defaultValue
        readData (fun _ ->
            if not (psiModule.IsValid()) then () else

            let symbolScope = getSymbolScope ()
            let typeElement = symbolScope.GetTypeElementByCLRName(typeName)
            if isNull typeElement then () else

            result <- f typeElement
        )

        result

    let isInaccessibleExplicitImpl (typeMember: ITypeMember) =
        let overridableMember = typeMember.As<IOverridableMember>()
        isNotNull overridableMember && overridableMember.IsExplicitImplementation &&

        // Checking access rights is required, because VB.NET explicit implementations may be accessible
        match overridableMember.GetAccessRights() with
        | AccessRights.PRIVATE
        | AccessRights.FILE_LOCAL
        | AccessRights.NONE -> true
        | _ -> false

    let getSignature (parametersOwner: IParametersOwner) =
        parametersOwner.GetSignature(parametersOwner.IdSubstitution)

    let getMethods (typeElement: ITypeElement) =
        seq {
            let seenMethods = HashSet(CSharpInvocableSignatureComparer.Overload)
            for method in typeElement.GetMembers().OfType<IFunction>() do
                if not (isInaccessibleExplicitImpl method) && seenMethods.Add(getSignature method) then
                    method
        }

    let getFields (typeElement: ITypeElement): seq<IField> =
        match typeElement with
        | :? IEnum as e -> e.EnumMembers
        | _ -> typeElement.GetMembers().OfType<IField>()

    let getProperties (typeElement: ITypeElement) =
        seq {
            let seenProperties = HashSet(CSharpInvocableSignatureComparer.Overload)
            for property in typeElement.Properties do
                if not (isInaccessibleExplicitImpl property) && seenProperties.Add(getSignature property) then
                    property
        }

    let getEvents (typeElement: ITypeElement) =
        typeElement.Events
        |> Seq.filter (fun event -> not (isInaccessibleExplicitImpl event))

    let mkMethods (typeElement: ITypeElement) =
        getMethods typeElement |> Seq.map mkMethodDef |> Array.ofSeq

    let mkFields (typeElement: ITypeElement) =
        let fields =
            [ for field in getFields typeElement do
                yield mkFieldDef field ]

        match typeElement with
        | :? IEnum as enum -> mkEnumInstanceValue enum :: fields
        | _ -> fields

    let mkProperties (typeElement: ITypeElement) =
        getProperties typeElement |> Seq.map mkPropertyDef |> List.ofSeq

    let mkEvents (typeElement: ITypeElement) =
        getEvents typeElement |> Seq.map mkEventDef |> List.ofSeq

    let mkNestedTypes reader (typeElement: ITypeElement) =
        [| for typeElement in typeElement.NestedTypes do
            PreTypeDef(typeElement, reader) :> ILPreTypeDef |]

    let mkTypeDefName (typeElement: ITypeElement) (clrTypeName: IClrTypeName) =
        match typeElement.GetContainingType() with
        | null -> clrTypeName.FullName
        | _ -> mkNameFromClrTypeName clrTypeName

    let moduleTypeElements () =
        getSymbolScope().GetAllTypeElementsGroupedByName()
        |> Seq.filter (fun typeElement -> isNull (typeElement.GetContainingType()))

    let mkPreTypeDefs reader =
        // todo: make inner types computed on demand, needs an Fcs patch
        let result = List<ILPreTypeDef>()

        for typeElement in moduleTypeElements () do
            result.Add(PreTypeDef(typeElement, reader))

        result.ToArray()

    let cacheMembersTable (table: FcsTypeDefMembers) (typeName: IClrTypeName) =
        let fcsTypeDef = typeDefs.TryGetValue(typeName)
        if isNotNull fcsTypeDef && isNull fcsTypeDef.Members then
            fcsTypeDef.Members <- table

    let getOrCreateNestedTypes (table: FcsTypeDefMembers) (typeName: IClrTypeName) defaultValue reader =
        use _ = usingWriteLock ()

        let typeTable = table.NestedTypes
        if isNotNull typeTable then typeTable else

        lock table (fun _ ->
            usingTypeElement typeName () (fun typeElement ->
                table.NestedTypes <- mkNestedTypes reader typeElement
                cacheMembersTable table typeName
            )

            let memberTables = table.NestedTypes

            // false when could not get the type element
            if isNull memberTables then defaultValue else

            memberTables
        )

    let getOrCreateMembers (table: FcsTypeDefMembers) (typeName: IClrTypeName) (defaultValue: 'Table) (getMemberTable: FcsTypeDefMemberTables -> 'Table) =
        use _ = usingWriteLock ()

        let memberTables = table.MemberTables
        if isNotNull memberTables then getMemberTable memberTables else

        lock table (fun _ ->
            usingTypeElement typeName () (fun typeElement ->
                let memberTables =
                    { Methods = mkMethods typeElement
                      Fields = mkFields typeElement
                      Events = mkEvents typeElement
                      Properties = mkProperties typeElement }

                table.MemberTables <- memberTables
                cacheMembersTable table typeName
            )

            let memberTables = table.MemberTables

            // false when could not get the type element
            if isNull memberTables then defaultValue else

            getMemberTable memberTables
        )


    let getOrCreateMethods (table: FcsTypeDefMembers) (typeName: IClrTypeName) =
        getOrCreateMembers table typeName EmptyArray.Instance (fun members -> members.Methods)

    let getOrCreateFields (table: FcsTypeDefMembers) (typeName: IClrTypeName) =
        getOrCreateMembers table typeName [] (fun members -> members.Fields)

    let getOrCreateProperties (table: FcsTypeDefMembers) (typeName: IClrTypeName) =
        getOrCreateMembers table typeName [] (fun members -> members.Properties)

    let getOrCreateEvents (table: FcsTypeDefMembers) (typeName: IClrTypeName) =
        getOrCreateMembers table typeName [] (fun members -> members.Events)

    let getOrCreateNestedTypes (table: FcsTypeDefMembers) reader (typeName: IClrTypeName) =
        getOrCreateNestedTypes table typeName [||] reader


    let rec typeParametersCount (typeElement: ITypeElement) =
        typeElement.TypeParametersCount +

        match typeElement.GetContainingType() with
        | null -> 0
        | containingType -> typeParametersCount containingType

    let rec isIdentityTypeArgs (ilTypeArgs: ILType list) index count =
        match ilTypeArgs with
        | [] -> index = count
        | ilTypeArg :: rest ->

        match ilTypeArg with
        | ILType.TypeVar ilIndex -> ilIndex = uint16 index && isIdentityTypeArgs rest (index + 1) count
        | _ -> false

    /// Follows `mkType` case by case, so a change there needs the same change here.
    let rec isSameType (t: IType) (ilType: ILType) =
        if t.IsVoid() then ilType = ILType.Void else

        if not t.IsResolved then isUnresolvedType ilType else

        match t with
        | :? IDeclaredType as declaredType ->
            match declaredType.Resolve() with
            | :? EmptyResolveResult -> isObjectType ilType
            | resolveResult ->

            match resolveResult.DeclaredElement with
            | :? ITypeParameter as typeParameter ->
                match typeParameter.Owner with
                | null -> isObjectType ilType
                | _ ->

                match ilType with
                | ILType.TypeVar index -> index = uint16 (getGlobalIndex typeParameter)
                | _ -> false

            | :? ITypeElement as typeElement ->
                let isValueType =
                    match typeElement with
                    | :? IEnum
                    | :? IStruct -> true
                    | _ -> false

                match ilType, isValueType with
                | ILType.Value typeSpec, true
                | ILType.Boxed typeSpec, false ->
                    mkTypeRef typeElement = typeSpec.TypeRef &&

                    let substitution = resolveResult.Substitution
                    let domain = substitution.Domain
                    if domain.IsEmpty() then List.isEmpty typeSpec.GenericArgs else

                    // `mkType` orders the arguments by the global index.
                    let genericArgs = typeSpec.GenericArgs
                    domain.Count = genericArgs.Length &&

                    domain |> Seq.forall (fun typeParameter ->
                        let index = getGlobalIndex typeParameter
                        index < genericArgs.Length &&
                        isSameType substitution[typeParameter] genericArgs[index])

                | _ -> false

            | _ -> false

        | :? IArrayType as arrayType ->
            match ilType with
            | ILType.Array(shape, elementType) ->
                shape.Rank = arrayType.Rank &&
                isSameType arrayType.ElementType elementType
            | _ -> false

        | :? IPointerType as pointerType ->
            match ilType with
            | ILType.Ptr elementType -> isSameType pointerType.ElementType elementType
            | _ -> false

        | :? IFunctionPointerType as functionPointerType ->
            match ilType with
            | ILType.FunctionPointer signature ->
                isSameReturnParameterType functionPointerType.ReturnKind functionPointerType.ReturnType
                    signature.ReturnType &&

                (functionPointerType.Parameters, signature.ArgTypes)
                ||> forallPaired (fun param ilType -> isSameParameterType param.Kind param.Type ilType)

            | _ -> false

        | _ -> false

    and isSameParameterType (kind: ParameterKind) (t: IType) (ilType: ILType) =
        if not (kind.IsByReference()) then isSameType t ilType else

        match ilType with
        | ILType.Byref elementType -> isSameType t elementType
        | _ -> false

    and isSameReturnParameterType (kind: ReferenceKind) (t: IType) (ilType: ILType) =
        isSameParameterType (kind.ToParameterKind()) t ilType

    and isSameParameterOwnerReturnType (owner: IParametersOwner) (ilType: ILType) =
        isSameReturnParameterType owner.ReturnKind owner.ReturnType ilType

    and isObjectType ilType =
        isSameType (psiModule.GetPredefinedType().Object) ilType

    and isUnresolvedType ilType =
        let objType = psiModule.GetPredefinedType().Object
        if objType.IsResolved then isObjectType ilType else ilType = ILType.Void

    let isSameAttributeValue (valueType: IType) (c: ConstantValue) (ilElem: ILAttribElem) =
        match ilElem with
        | ILAttribElem.String(Some value) -> valueType.IsString() && c.StringValue = value
        | ILAttribElem.Bool value   -> valueType.IsBool()   && c.BoolValue = value
        | ILAttribElem.Char value   -> valueType.IsChar()   && c.CharValue = value
        | ILAttribElem.SByte value  -> valueType.IsSbyte()  && c.SbyteValue = value
        | ILAttribElem.Byte value   -> valueType.IsByte()   && c.ByteValue = value
        | ILAttribElem.Int16 value  -> valueType.IsShort()  && c.ShortValue = value
        | ILAttribElem.UInt16 value -> valueType.IsUshort() && c.UshortValue = value
        | ILAttribElem.Int32 value  -> valueType.IsInt()    && c.IntValue = value
        | ILAttribElem.UInt32 value -> valueType.IsUint()   && c.UintValue = value
        | ILAttribElem.Int64 value  -> valueType.IsLong()   && c.LongValue = value
        | ILAttribElem.UInt64 value -> valueType.IsUlong()  && c.UlongValue = value
        | ILAttribElem.Single value -> valueType.IsFloat()  && c.FloatValue = value
        | ILAttribElem.Double value -> valueType.IsDouble() && c.DoubleValue = value
        | _ -> false

    let isSameAttribElement (attrValue: AttributeValue) (ilElem: ILAttribElem) =
        let constantValue = attrValue.ConstantValue
        if constantValue.IsBadValue() || constantValue.IsNull() then ilElem = ILAttribElem.Null else

        let valueType =
            if constantValue.IsEnum() then
                constantValue.Type.GetEnumUnderlying()
            else
                constantValue.Type

        match ilElem with
        | ILAttribElem.Null -> isUnknownValueType attributeValueTypes valueType
        | _ -> isSameAttributeValue valueType constantValue ilElem

    let isSameLiteralValue (valueType: IType) (c: ConstantValue) (ilValue: ILFieldInit) =
        match ilValue with
        | ILFieldInit.String value -> valueType.IsString() && c.StringValue = value
        | ILFieldInit.Bool value   -> valueType.IsBool()   && c.BoolValue = value
        | ILFieldInit.Char value   -> valueType.IsChar()   && uint16 c.CharValue = value
        | ILFieldInit.Int8 value   -> valueType.IsSbyte()  && c.SbyteValue = value
        | ILFieldInit.UInt8 value  -> valueType.IsByte()   && c.ByteValue = value
        | ILFieldInit.Int16 value  -> valueType.IsShort()  && c.ShortValue = value
        | ILFieldInit.UInt16 value -> valueType.IsUshort() && c.UshortValue = value
        | ILFieldInit.Int32 value  -> valueType.IsInt()    && c.IntValue = value
        | ILFieldInit.UInt32 value -> valueType.IsUint()   && c.UintValue = value
        | ILFieldInit.Int64 value  -> valueType.IsLong()   && c.LongValue = value
        | ILFieldInit.UInt64 value -> valueType.IsUlong()  && c.UlongValue = value
        | ILFieldInit.Single value -> valueType.IsFloat()  && c.FloatValue = value
        | ILFieldInit.Double value -> valueType.IsDouble() && c.DoubleValue = value
        | _ -> false

    let isSameOptionalLiteralValue (c: ConstantValue) (valueType: IType) (ilValue: ILFieldInit option) =
        if c.IsBadValue() then ilValue.IsNone else
        if c.IsNull() then ilValue = nullLiteralValue else

        match ilValue with
        | Some ilValue -> isSameLiteralValue valueType c ilValue
        | None -> isUnknownValueType literalTypes valueType

    let isSameFieldLiteralValue (field: IField) (ilValue: ILFieldInit option) =
        if not (field.IsConstant || field.IsEnumMember) then ilValue.IsNone else

        let valueType =
            let underlyingType = field.Type.GetEnumUnderlying()
            if isNotNull underlyingType then underlyingType else field.Type

        isSameOptionalLiteralValue field.ConstantValue valueType ilValue

    let isSameParamDefaultValue (param: IParameter) (ilValue: ILFieldInit option) =
        if not param.IsOptional then ilValue.IsNone else

        let defaultValue = param.GetDefaultValue()
        if defaultValue.IsBadValue then ilValue.IsNone else

        isSameOptionalLiteralValue defaultValue.ConstantValue defaultValue.DefaultTypeValue ilValue

    let isSameReturnType (method: IFunction) (ilType: ILType) =
        if isInitOnlySetter method then
            match ilType with
            | ILType.Modified(_, _, modifiedType) -> isSameParameterOwnerReturnType method modifiedType
            | _ -> isNull (getExternalInitTypeElement ()) && isSameParameterOwnerReturnType method ilType
        else
            match ilType with
            | ILType.Modified _ -> false
            | _ -> isSameParameterOwnerReturnType method ilType

    /// `mkCustomAttribute` uses the identity substitution, so each argument is its type variable.
    let isSameDeclaringType (typeElement: ITypeElement) (ilType: ILType) =
        match ilType with
        | ILType.Boxed typeSpec ->
            mkTypeRef typeElement = typeSpec.TypeRef &&
            isIdentityTypeArgs typeSpec.GenericArgs 0 (typeParametersCount typeElement)

        | _ -> false

    /// The method def check compares the accessor types, so the name and the count are enough here.
    let isSameAccessor (accessor: IFunction) (methodRef: ILMethodRef) =
        accessor.ShortName = methodRef.Name &&
        accessor.Parameters.Count = methodRef.ArgCount

    let isSameOptionalAccessor (accessor: IFunction) (methodRef: ILMethodRef option) =
        match methodRef with
        | None -> isNull accessor
        | Some methodRef -> isNotNull accessor && isSameAccessor accessor methodRef

    let customAttributeInstances (attributesSet: IAttributesSet) =
        attributesSet.GetAttributeInstances(AttributesSource.Self)
        |> Seq.filter (fun attributeInstance -> isNotNull attributeInstance.Constructor)

    let isSameCustomAttribute (attrInstance: IAttributeInstance) (ilAttr: ILAttribute) =
        match ilAttr with
        | ILAttribute.Encoded _ -> false
        | ILAttribute.Decoded(methodSpec, fixedArgs, namedArgs) ->

        let ctor = attrInstance.Constructor
        isSameDeclaringType ctor.ContainingType methodSpec.DeclaringType &&
        isSameAccessor ctor methodSpec.MethodRef &&
        forallPaired isSameAttribElement (attrInstance.PositionParameters()) fixedArgs &&

        (attributeNamedParameters attrInstance, namedArgs) ||> forallPaired (fun (Pair(name, attributeValue)) (ilName, ilType, ilIsField, ilElem) ->
            name = ilName &&
            ilIsField &&
            isSameAttribElement attributeValue ilElem &&
            isSameType attributeValue.ConstantValue.Type ilType)

    let hasGeneratedAttribute (attrTypeName: IClrTypeName) (attrs: ILAttribute seq) =
        match Seq.tryHead attrs with
        | Some attr when isCompilerGeneratedAttribute attrTypeName attr -> true
        | _ ->

        let typeElement = FcsModuleReaderCompilerGeneratedType(attrTypeName, psiModule).GetTypeElement()
        isNull typeElement || typeElement.Constructors |> Seq.exists _.IsParameterless |> not

    let isNullableAttribute (expected: ILAttribElem) (ilAttr: ILAttribute) =
        match ilAttr with
        | ILAttribute.Decoded(methodSpec, [ actual ], []) ->
            actual = expected &&
            methodSpec.MethodRef.DeclaringTypeRef.Name = PredefinedType.NULLABLE_ATTRIBUTE_FQN.FullName
        | _ -> false

    let isSameNullness (nullness: ILAttribElem option) (attrs: ILAttribute seq) =
        match nullness with
        | None -> true
        | Some expected ->

        match Seq.tryHead attrs with
        | Some attr when isNullableAttribute expected attr -> true
        | _ ->

        let attrTypeName = PredefinedType.NULLABLE_ATTRIBUTE_FQN
        let typeElement = FcsModuleReaderCompilerGeneratedType(attrTypeName, psiModule).GetTypeElement()
        isNull typeElement || typeElement.Constructors |> Seq.exists (fun ctor -> ctor.Parameters.Count = 1) |> not

    let skipNullableAttribute (nullness: ILAttribElem option) (attrs: ILAttribute seq) =
        if nullness.IsSome then Seq.tail attrs else attrs

    let isSameNullnessAttributes (nullness: ILAttribElem option) (attrs: ILAttribute seq) =
        isSameNullness nullness attrs &&
        Seq.isEmpty (skipNullableAttribute nullness attrs)

    let isSameCustomAttributes (attributesSet: IAttributesSet) (attrs: ILAttribute seq) =
        forallPaired isSameCustomAttribute (customAttributeInstances attributesSet) attrs

    let isSameCustomAttributesWithNullness (attributesSet: IAttributesSet) (t: IType) (attrs: ILAttribute seq) =
        let nullness = getNullness t
        isSameNullness nullness attrs &&
        isSameCustomAttributes attributesSet (skipNullableAttribute nullness attrs)

    let isUpToDateTypeParamDef (typeParameter: ITypeParameter) (genericParameterDef: ILGenericParameterDef) =
        typeParameter.ShortName = genericParameterDef.Name &&
        mkGenericVariance typeParameter.Variance = genericParameterDef.Variance &&
        typeParameter.IsReferenceType = genericParameterDef.HasReferenceTypeConstraint &&
        typeParameter.IsValueType = genericParameterDef.HasNotNullableValueTypeConstraint &&
        hasDefaultConstructorConstraint typeParameter = genericParameterDef.HasDefaultConstructorConstraint &&
        forallPaired isSameType typeParameter.TypeConstraints genericParameterDef.Constraints &&

        let attrs = genericParameterDef.CustomAttrs.AsArray()
        let isUnmanaged = typeParameter.IsUnmanagedType
        (not isUnmanaged || hasGeneratedAttribute PredefinedType.IS_UNMANAGED_ATTRIBUTE_FQN attrs) &&

        let attrs = if isUnmanaged then Seq.tail attrs else attrs
        isSameNullnessAttributes (getTypeParameterNullness typeParameter) attrs

    let isUpToDateTypeDefCustomAttributes (typeElement: ITypeElement) (typeDef: ILTypeDef) =
        let attrs = typeDef.CustomAttrsStored.CustomAttrs.AsArray()

        let indexerName = getIndexerName typeElement
        (indexerName.IsNone || hasGeneratedAttribute PredefinedType.DEFAULT_MEMBER_ATTRIBUTE_CLASS attrs) &&

        let attrs = if indexerName.IsSome then Seq.tail attrs else attrs
        let isByRefLike = isByRefLikeType typeElement
        (not isByRefLike || hasGeneratedAttribute PredefinedType.IS_BY_REF_LIKE_ATTRIBUTE_FQN attrs) &&

        let attrs = if isByRefLike then Seq.tail attrs else attrs
        let hasExtensions = hasExtensions typeElement
        (not hasExtensions || hasGeneratedAttribute PredefinedType.EXTENSION_ATTRIBUTE_CLASS attrs) &&

        let attrs = if hasExtensions then Seq.tail attrs else attrs
        isSameCustomAttributesWithNullness typeElement (getBaseType typeElement) attrs

    let isSameParameterOrReturnAttributes (attributesSet: IAttributesSet) isReadonlyRef (t: IType)
            (attrs: ILAttribute seq) =
        (not isReadonlyRef || hasGeneratedAttribute PredefinedType.IS_READ_ONLY_ATTRIBUTE_FQN attrs) &&

        let attrs = if isReadonlyRef then Seq.tail attrs else attrs
        isSameCustomAttributesWithNullness attributesSet t attrs

    let isUpToDateParameterDef (param: IParameter) (paramDef: ILParameter) =
        // `IsIn` and `IsOut` hold the `in`, `out`, and `ref` modifiers.
        Some(param.ShortName) = paramDef.Name &&
        (param.Kind = ParameterKind.INPUT) = paramDef.IsIn &&
        (param.Kind = ParameterKind.OUTPUT) = paramDef.IsOut &&
        param.IsOptional = paramDef.IsOptional &&

        isSameParameterType param.Kind param.Type paramDef.Type &&
        isSameParamDefaultValue param paramDef.Default &&

        let attrs = paramDef.CustomAttrs.AsArray()
        let isParameterArray = param.IsParameterArray
        (not isParameterArray || hasGeneratedAttribute PredefinedType.PARAM_ARRAY_ATTRIBUTE_CLASS attrs) &&

        let attrs = if isParameterArray then Seq.tail attrs else attrs
        isSameParameterOrReturnAttributes param (isReadonlyRefParameter param) param.Type attrs

    let isUpToDateReturn (method: IFunction) (methodDef: ILMethodDef) =
        let methodDefReturn = methodDef.Return
        isSameReturnType method methodDefReturn.Type &&

        let attrs = methodDefReturn.CustomAttrs.AsArray()
        isSameParameterOrReturnAttributes method.ReturnTypeAttributes (isReadonlyRefReturn method) method.ReturnType attrs

    let isUpToDateMethodCustomAttributes (method: IFunction) (ilAttrs: ILAttributes) =
        let attrs = ilAttrs.AsArray()
        let isExtension = isExtensionMethod method
        (not isExtension || hasGeneratedAttribute PredefinedType.EXTENSION_ATTRIBUTE_CLASS attrs) &&

        let attrs = if isExtension then Seq.tail attrs else attrs
        isSameCustomAttributes method attrs

    let isUpToDateMethodDef (method: IFunction) (methodDef: ILMethodDef) =
        method.ShortName = methodDef.Name &&
        mkMethodAttributes method = methodDef.Attributes &&
        mkCallingConv method = methodDef.CallingConv &&

        let parameters = method.Parameters
        parameters.Count = methodDef.Parameters.Length &&

        let asMethod = method.As<IMethod>()
        (isNull asMethod || forallPaired isUpToDateTypeParamDef asMethod.TypeParameters methodDef.GenericParams) &&

        Seq.forall2 isUpToDateParameterDef parameters methodDef.Parameters &&

        isUpToDateReturn method methodDef &&
        isUpToDateMethodCustomAttributes method methodDef.CustomAttrs

    let isUpToDateMethodsDefs (typeElement: ITypeElement) (methodDefs: ILMethodDef[]) =
        isNull methodDefs ||
        forallPaired isUpToDateMethodDef (getMethods typeElement) methodDefs

    let isUpToDateFieldDef (field: IField) (fieldDef: ILFieldDef) =
        field.ShortName = fieldDef.Name &&
        mkFieldAttributes field = fieldDef.Attributes &&
        isSameType field.Type fieldDef.FieldType &&
        isSameFieldLiteralValue field fieldDef.LiteralValue &&
        isSameCustomAttributesWithNullness field field.Type (fieldDef.CustomAttrs.AsArray())

    let isUpToDateFieldDefs (typeElement: ITypeElement) (fieldDefs: ILFieldDef list) =
        isNull fieldDefs ||

        // An enum gets a leading `value__` field, which no declared element gives.
        let fieldDefs =
            match typeElement, fieldDefs with
            | :? IEnum, _ :: rest -> rest
            | _ -> fieldDefs

        forallPaired isUpToDateFieldDef (getFields typeElement) fieldDefs

    let isSameEventType (eventType: IType) (ilEventType: ILType option) =
        match eventType, ilEventType with
        | null, ilEventType -> ilEventType.IsNone
        | _, None -> false
        | eventType, Some ilType -> isSameType eventType ilType

    let isUpToDateEventDef (event: IEvent) (eventDef: ILEventDef) =
        event.ShortName = eventDef.Name &&
        isSameAccessor (getAddAccessor event) eventDef.AddMethod &&
        isSameAccessor (getRemoveAccessor event) eventDef.RemoveMethod &&
        isSameOptionalAccessor event.Raiser eventDef.FireMethod &&

        let eventType = getEventType event
        isSameEventType eventType eventDef.EventType &&
        isSameCustomAttributesWithNullness event eventType (eventDef.CustomAttrs.AsArray())

    let isUpToDateEventDefs (typeElement: ITypeElement) (eventDefs: ILEventDef list) =
        isNull eventDefs ||

        forallPaired isUpToDateEventDef (getEvents typeElement) eventDefs

    let isUpToDatePropertyDef (property: IProperty) (propertyDef: ILPropertyDef) =
        property.GetDefaultPropertyMetadataName() = propertyDef.Name &&
        mkCallingThisConv property = propertyDef.CallingConv &&
        isSameOptionalAccessor property.Setter propertyDef.SetMethod &&
        isSameOptionalAccessor property.Getter propertyDef.GetMethod &&
        property.Parameters.Count = propertyDef.Args.Length &&

        isSameParameterOwnerReturnType property propertyDef.PropertyType &&

        (property.Parameters, propertyDef.Args)
        ||> forallPaired (fun (parameter: IParameter) ilType -> isSameType parameter.Type ilType) &&

        let attrs = propertyDef.CustomAttrs.AsArray()
        let isRequired = property.IsRequired
        (not isRequired || hasGeneratedAttribute PredefinedType.REQUIRED_MEMBER_ATTRIBUTE_FQN attrs) &&

        let attrs = if isRequired then Seq.tail attrs else attrs
        isSameCustomAttributesWithNullness property property.ReturnType attrs

    let isUpToDatePropertyDefs (typeElement: ITypeElement) (propertyDefs: ILPropertyDef list) =
        isNull propertyDefs ||

        forallPaired isUpToDatePropertyDef (getProperties typeElement) propertyDefs

    let isUpToDateInterfaceImpls (typeElement: ITypeElement) (typeDef: ILTypeDef) =
        let expected = typeDef.Implements
        not expected.IsValueCreated ||

        (getInterfaces typeElement, expected.Value)
        ||> forallPaired (fun declaredType (impl: InterfaceImpl) ->
            isSameType declaredType impl.Type &&
            isSameNullnessAttributes (getNullness declaredType) (impl.CustomAttrs.AsArray()))

    let isUpToDateBaseType (typeDef: ILTypeDef) (typeElement: ITypeElement) =
        not typeDef.Extends.IsValueCreated ||

        match getBaseType typeElement, typeDef.Extends.Value with
        | null, extends -> extends.IsNone
        | _, None -> false
        | baseType, Some ilType -> isSameType baseType ilType

    let rec isUpToDateTypeDef (typeElement: ITypeElement) (fcsTypeDef: FcsTypeDef) =
        let typeDef = fcsTypeDef.TypeDef

        mkTypeAttributes typeElement = typeDef.Attributes &&
        isUpToDateBaseType typeDef typeElement &&
        isUpToDateInterfaceImpls typeElement typeDef &&
        forallPaired isUpToDateTypeParamDef (getGenericParameters typeElement) typeDef.GenericParams &&
        isUpToDateTypeDefCustomAttributes typeElement typeDef &&
        isUpToDateNestedTypesAndMembers typeElement fcsTypeDef.Members

    and isUpToDateNestedTypeDefs (typeElement: ITypeElement) (preTypeDefs: ILPreTypeDef[]) =
        isNull preTypeDefs ||

        forallPaired (fun (nestedType: ITypeElement) (preTypeDef: ILPreTypeDef) ->
            let clrTypeName: IClrTypeName = (preTypeDef :?> PreTypeDef).ClrTypeName
            clrTypeName.ShortName = nestedType.ShortName &&
            clrTypeName.TypeParametersCount = nestedType.TypeParametersCount)
            typeElement.NestedTypes preTypeDefs &&

        preTypeDefs |> Array.forall (fun preTypeDef ->
            let preTypeDef = preTypeDef :?> PreTypeDef
            let clrTypeName = preTypeDef.ClrTypeName
            let typeDef = typeDefs.TryGetValue(clrTypeName)
            isNull typeDef ||

            let symbolScope = getSymbolScope ()
            let typeElement = symbolScope.GetTypeElementByCLRName(clrTypeName)
            isNotNull typeElement && isUpToDateTypeDef typeElement typeDef)

    and isUpToDateNestedTypesAndMembers (typeElement: ITypeElement) (members: FcsTypeDefMembers) =
        isNull members ||

        isUpToDateNestedTypeDefs typeElement members.NestedTypes &&
        isUpToDateMembers typeElement members.MemberTables

    and isUpToDateMembers (typeElement: ITypeElement) (tables: FcsTypeDefMemberTables) =
        isNull tables ||

        isUpToDateMethodsDefs typeElement tables.Methods &&
        isUpToDateFieldDefs typeElement tables.Fields &&
        isUpToDateEventDefs typeElement tables.Events &&
        isUpToDatePropertyDefs typeElement tables.Properties

    let isUpToDateTypeDef (clrTypeName: IClrTypeName) (fcsTypeDef: FcsTypeDef) =
        let symbolScope = getSymbolScope ()
        match symbolScope.GetTypeElementByCLRName(clrTypeName) with
        | null -> false
        | typeElement -> isUpToDateTypeDef typeElement fcsTypeDef

    let isUpToDatePreTypeDefs (preTypeDefs: ILPreTypeDef[]) =
        // todo: can the order change? Do we want to support it, if yes?
        (moduleTypeElements (), preTypeDefs)
        ||> forallPaired (fun typeElement preTypeDef ->
            let clrTypeName: IClrTypeName = (preTypeDef :?> PreTypeDef).ClrTypeName
            clrTypeName.ShortName = typeElement.ShortName &&
            clrTypeName.TypeParametersCount = typeElement.TypeParametersCount &&
            clrTypeName.GetNamespaceName() = typeElement.GetContainingNamespace().QualifiedName)

    /// `mkILSimpleModule` gives the manifest no attribute, so only `InternalsVisibleTo` is there.
    let isUpToDateManifestCustomAttributes (attrs: ILAttributes) =
        (internalsVisibleToNames (), attrs.AsArray())
        ||> forallPaired (fun name ilAttr ->
            match ilAttr with
            | ILAttribute.Decoded(methodSpec, [ ILAttribElem.String(Some ilName) ], []) ->
                name = ilName &&
                methodSpec.MethodRef.DeclaringTypeRef.Name =
                    PredefinedType.INTERNALS_VISIBLE_TO_ATTRIBUTE_CLASS.FullName
            | _ -> false)

    // todo: check added/removed types
    /// Checks if any external change has lead to a metadata change,
    /// e.g. a super type resolves to a different thing.
    let isUpToDate () =
        locks.AssertReadAccessAllowed()
        use lock = usingWriteLock ()

        if not isDirty then true else

        if isNull upToDateCheckedTypes then
            upToDateCheckedTypes <- HashSet()
            seenOutdatedTypes <- false

        use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

        match moduleDef with
        | None -> ()
        | Some(moduleDef, oldPreTypeDefs) ->
            let upToDate =
                isUpToDatePreTypeDefs oldPreTypeDefs &&

                match moduleDef.Manifest with
                | Some manifest -> isUpToDateManifestCustomAttributes manifest.CustomAttrsStored.CustomAttrs
                | None -> true

            if not upToDate then
                seenOutdatedTypes <- true

        for KeyValue(clrTypeName, fcsTypeDef) in List.ofSeq typeDefs do
            Interruption.Current.CheckAndThrow()

            if upToDateCheckedTypes.Contains(clrTypeName) then () else

            if not (isUpToDateTypeDef clrTypeName fcsTypeDef) then
                typeDefs.TryRemove(clrTypeName) |> ignore
                seenOutdatedTypes <- true

            upToDateCheckedTypes.Add(clrTypeName) |> ignore

        isDirty <- false
        upToDateCheckedTypes <- null
        not seenOutdatedTypes

    member this.CreateTypeDef(clrTypeName: IClrTypeName) =
        FSharpAsyncUtil.CheckAndThrow()
        use lock = usingWriteLock ()

        match typeDefs.TryGetValue(clrTypeName) with
        | NotNull typeDef -> typeDef.TypeDef
        | _ ->

        readData (fun _ ->
            if not (psiModule.IsValid()) then () else

            let symbolScope = getSymbolScope ()
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
                let extends = InterruptibleLazy(fun _ -> usingTypeElement clrTypeName None mkTypeDefExtends)
                let genericParams = mkGenericParamDefs typeElement

                // todo: pass this table in nested types too?
                let membersTable = FcsTypeDefMembers.Create()
                let implements = InterruptibleLazy (fun _ -> usingTypeElement clrTypeName [] mkTypeDefImplements)
                let nestedTypes = mkILTypeDefsComputed (fun _ -> getOrCreateNestedTypes membersTable this clrTypeName)
                let methods = mkILMethodsComputed (fun _ -> getOrCreateMethods membersTable clrTypeName)
                let fields = mkILFieldsLazy (InterruptibleLazy(fun _ -> getOrCreateFields membersTable clrTypeName))
                let properties = mkILPropertiesLazy (InterruptibleLazy(fun _ -> getOrCreateProperties membersTable clrTypeName))
                let events = mkILEventsLazy (InterruptibleLazy(fun _ -> getOrCreateEvents membersTable clrTypeName))

                let hasExtensions = hasExtensions typeElement
                let customAttrs = mkILCustomAttrsComputed (fun _ ->
                    usingTypeElement clrTypeName [||] mkTypeDefCustomAttrs
                )

                let typeKind =
                    match typeElement with
                    | :? IEnum -> ILTypeDefAdditionalFlags.Enum
                    | :? IStruct -> ILTypeDefAdditionalFlags.ValueType
                    | :? IDelegate -> ILTypeDefAdditionalFlags.Delegate
                    | :? IInterface -> ILTypeDefAdditionalFlags.Interface
                    | _ -> ILTypeDefAdditionalFlags.Class

                let additionalFlags =
                    if hasExtensions then ILTypeDefAdditionalFlags.CanContainExtensionMethods ||| typeKind
                    else typeKind

                let typeDef =
                    ILTypeDef(name, typeAttributes, ILTypeDefLayout.Auto, implements,
                        genericParams, extends, methods, nestedTypes, fields, emptyILMethodImpls, events, properties,
                        additionalFlags, emptyILSecurityDecls, customAttrs)

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
        use lock = usingWriteLock ()
        typeDefs.TryRemove(clrTypeName) |> ignore
        shim.Logger.Trace("Invalidate TypeDef: {0}: {1}", path, clrTypeName)

        // todo: invalidate timestamp on seen-by-FCS type changes only
        // todo: add test for adding/removing not-seen-by-FCS types
        moduleDef <- None
        timestamp <- DateTime.UtcNow
        shim.Logger.Trace("New timestamp: {0}: {1}", path, timestamp)

    member this.GetFcsIlType(t: IType) =
        mkType t

    interface IProjectFcsModuleReader with
        member this.EnableNullness() =
            if isNullnessEnabled then () else

            use lock = usingWriteLock ()
            isNullnessEnabled <- true
            shim.Logger.Trace("Enable nullness: {0}", path)

            markDirty ()

        member this.ILModuleDef =
            FSharpAsyncUtil.CheckAndThrow()
            use lock = usingWriteLock ()

            match moduleDef with
            | Some(moduleDef, _) -> moduleDef
            | None ->

            readData (fun _ ->
                if not (psiModule.IsValid()) then () else

                let project = psiModule.ContainingProjectModule :?> IProject
                let moduleName = project.Name
                let assemblyName = project.GetOutputAssemblyName(psiModule.TargetFrameworkId)
                let isDll = isDll project psiModule.TargetFrameworkId

                let preTypeDefs = mkPreTypeDefs this
                let typeDefs = mkILTypeDefsComputed (fun _ -> preTypeDefs)

                let flags = 0 // todo
                let exportedTypes = mkILExportedTypes []

                let newModuleDef =
                    mkILSimpleModule
                        assemblyName moduleName isDll
                        DummyValues.subsystemVersion
                        DummyValues.useHighEntropyVA
                        typeDefs
                        None None flags exportedTypes
                        DummyValues.metadataVersion

                let ivtAttributes =
                    [| for name in internalsVisibleToNames () do
                         match mkInternalsVisibleToAttribute name with
                         | Some attribute -> attribute
                         | _ -> () |]

                let newModuleDef =
                    if ivtAttributes.IsEmpty() then newModuleDef else

                    let attrs = mkILCustomAttrsFromArray ivtAttributes |> storeILCustomAttrs
                    let manifest = { newModuleDef.Manifest.Value with CustomAttrsStored = attrs }
                    { newModuleDef with Manifest = Some(manifest) }

                moduleDef <- Some(newModuleDef, preTypeDefs)
            )

            match moduleDef with
            | None -> mkDummyModuleDef ()
            | Some(moduleDef, _) -> moduleDef

        member this.Dispose() =
            match realModuleReader with
            | Some(moduleReader) -> moduleReader.Dispose()
            | _ -> ()

        member this.ILAssemblyRefs = []

        member this.Timestamp =
            FSharpAsyncUtil.CheckAndThrow()
            use lock = locker.UsingReadLock()

            timestamp

        member this.Path = path
        member this.PsiModule = psiModule

        member val RealModuleReader = None with get, set

        member this.UpdateTimestamp() =
            use _ = usingWriteLock ()
            shim.Logger.Trace("Checking up to date: {0}", path)
            if isUpToDate () then
                shim.Logger.Trace("Up to date: {0}", path)
            else
                upToDateCheckedTypes <- null
                seenOutdatedTypes <- false
                moduleDef <- None
                timestamp <- DateTime.UtcNow
                shim.Logger.Trace("New timestamp: {0}: {1}", path, timestamp)

        member this.MarkDirty() =
            use _ = usingWriteLock ()
            markDirty ()


type PreTypeDef(clrTypeName: IClrTypeName, reader: ProjectFcsModuleReader) =
    new (typeElement: ITypeElement, reader: ProjectFcsModuleReader) =
        PreTypeDef(typeElement.GetClrName().GetPersistent(), reader) // todo: intern

    member this.ClrTypeName = clrTypeName

    interface ILPreTypeDef with
        member x.Name =
            let typeName = clrTypeName.TypeNames.Last() // todo: use clrTypeName.ShortName ? (check type params)
            mkNameFromTypeNameAndParamsNumber typeName

        member x.Namespace =
            if not (clrTypeName.TypeNames.IsSingle()) then [] else
            clrTypeName.NamespaceNames |> List.ofSeq

        member x.GetTypeDef() =
            reader.CreateTypeDef(clrTypeName)
