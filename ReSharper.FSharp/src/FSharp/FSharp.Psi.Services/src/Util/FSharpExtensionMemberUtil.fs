module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpExtensionMemberUtil

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods.Queries
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve.TypeInference
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util

type FSharpRequest(psiModule, exprType: IType, name: string option) =
    static let memberKinds = [ExtensionMemberKind.CLASSIC_METHOD; FSharpExtensionMemberKind.INSTANCE]

    let name = Option.toObj name

    let baseTypes: IReadOnlyList<IType> =
        if isNull exprType then [] else

        let isArray = exprType :? IArrayType
    
        let rec removeArrayType (exprType: IType) =
            match exprType with
            | :? IArrayType as arrayType -> removeArrayType arrayType.ElementType
            | _ -> exprType.As<IDeclaredType>()
    
        let exprDeclaredType = removeArrayType exprType
        if isNull exprDeclaredType then [] else

        let result = List<IType>()
        result.Add(exprType)

        for superType in exprDeclaredType.GetAllSuperTypes() do
            let superType: IType =
                if isArray then
                    TypeFactory.CreateArrayType(superType, 1, NullableAnnotation.Unknown)
                else
                    superType

            result.Add(superType)
            
        if isArray then
            let predefinedType = exprType.Module.GetPredefinedType()
            result.Add(predefinedType.Array)
            result.AddRange(predefinedType.Array.GetAllSuperTypes())

        result.AsReadOnly()

    interface IExtensionMembersRequest with
        member this.Name = name
        member this.IsCaseSensitive = true

        member this.Kinds = memberKinds

        member this.ReceiverType = exprType
        member this.PossibleReceiverTypes = baseTypes

        member this.ForModule = psiModule
        member this.ContainingNamespaces = []
        member this.ContainingTypes = []

        member this.WithName _ = failwith "todo"
        member this.WithKinds _ = failwith "todo"
        member this.WithReceiverType _ = failwith "todo"
        member this.WithModule _ = failwith "todo"
        member this.WithContainingNamespaces _ = failwith "todo"
        member this.WithContainingTypes _ = failwith "todo"

[<return: Struct>]
let (|FSharpSourceExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | :? IFSharpTypeMember as fsTypeMember ->
        match fsTypeMember.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsExtensionMember ->
            match mfv.DeclaringEntity with
            | Some fcsEntity when fcsEntity.IsFSharpModule -> ValueSome mfv
            | _ -> ValueNone
        | _ -> ValueNone
    | _ -> ValueNone

[<return: Struct>]
let (|FSharpCompiledExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | :? IMethod as method ->
        let containingType = method.ContainingType
        if containingType :? IFSharpModule && containingType :? IFSharpCompiledTypeElement then
            let parameters = method.Parameters
            if parameters.Count = 0 then ValueNone else

            ValueSome(parameters[0].Type.GetTypeElement())
        else
            ValueNone

    | _ -> ValueNone

[<return: Struct>]
let (|FSharpExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | FSharpSourceExtensionMember _
    | FSharpCompiledExtensionMember _ -> ValueSome()
    | _ -> ValueNone

let getExtensionMembers (context: IFSharpTreeNode) (fcsType: FSharpType) (nameOpt: string option) =
    let psiModule = context.GetPsiModule()
    let solution = psiModule.GetSolution()
    use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())

    let exprType = fcsType.MapType(context)

    let exprTypeElements =
        let typeElements = List()

        let exprTypeElement =
            if exprType :? IArrayType then
                exprType.Module.GetPredefinedType().Array.GetTypeElement()
            else
                exprType.GetTypeElement()

        if isNotNull exprTypeElement then
            typeElements.Add(exprTypeElement)
            typeElements.AddRange(exprTypeElement.GetSuperTypeElements())

        typeElements.AsReadOnly()

    let autoOpenCache = solution.GetComponent<FSharpAutoOpenCache>()
    let openedModulesProvider = OpenedModulesProvider(context.FSharpFile, autoOpenCache)
    let scopes = openedModulesProvider.OpenedModuleScopes
    let accessContext = FSharpAccessContext(context)

    let isInScope (typeMember: ITypeMember) =
        let isInScope declaredElement =
            let name = getQualifiedName declaredElement
            let scopes = scopes.GetValuesSafe(name)
            OpenScope.inAnyScope context scopes
        
        match typeMember.ContainingType with
        | :? IFSharpModule as fsModule ->
            isInScope fsModule

        | containingType ->
            let ns = containingType.GetContainingNamespace()
            isInScope ns

    let matchesType (typeMember: ITypeMember) : bool =
        let matchesWithoutSubstitution (extendedTypeElement: ITypeElement) =
            if isNull extendedTypeElement then false else

            // todo: arrays and other non-declared-types?
            exprTypeElements |> Seq.exists extendedTypeElement.Equals

        match typeMember with
        | FSharpSourceExtensionMember mfv ->
            let extendedTypeElement = mfv.ApparentEnclosingEntity.GetTypeElement(typeMember.Module)
            matchesWithoutSubstitution extendedTypeElement

        | FSharpCompiledExtensionMember extendedTypeElement ->
            matchesWithoutSubstitution extendedTypeElement

        | :? IMethod as method ->
            let parameters = method.Parameters
            if parameters.Count = 0 then false else

            let consumer = RecursiveConsumer(method.TypeParameters.ToIReadOnlyList())
            let typeInferenceMatcher = CLRTypeInferenceMatcher.Instance
            typeInferenceMatcher.Match(TypeInferenceKind.LowerBound, exprType, parameters[0].Type, consumer)

        | _ -> false

    let isAccessible (typeMember: ITypeMember) =
        let isTypeAccessible = 
            let containingType = typeMember.ContainingType
            let accessRightsOwner = containingType :?> IAccessRightsOwner
            match accessRightsOwner.GetAccessRights() with
            | AccessRights.PUBLIC -> true
            | _ -> containingType.Module.AreInternalsVisibleTo(psiModule)

        isTypeAccessible &&

        match typeMember with
        | FSharpExtensionMember _ -> true
        | _ -> AccessUtil.IsSymbolAccessible(typeMember, accessContext)

    let matchesName (typeMember: ITypeMember) =
        match nameOpt with
        | None -> true
        | Some name ->

        match typeMember with
        | FSharpCompiledExtensionMember _ ->
            let memberName = typeMember.ShortName
            memberName = name ||
            memberName = $"get_{name}" ||
            memberName = $"set_{name}" ||
  
            memberName.EndsWith($".{name}") ||
            memberName.EndsWith($".get_{name}") ||
            memberName.EndsWith($".set_{name}")

        | _ -> typeMember.ShortName = name

    let resolvesAsExtensionMember (typeMember: ITypeMember) =
        match typeMember with
        | :? IFSharpDeclaredElement -> typeMember :? IFSharpMethod || typeMember :? IFSharpProperty
        | _ -> true

    let isApplicable (typeMember: ITypeMember) =
        resolvesAsExtensionMember typeMember &&
        matchesName typeMember &&
        not (isInScope typeMember) &&
        isAccessible typeMember &&
        matchesType typeMember

    let query = ExtensionMembersQuery(solution.GetPsiServices(), FSharpRequest(psiModule, exprType, nameOpt))
    let methods = query.EnumerateMembers() |> List.ofSeq

    methods
    |> Seq.filter isApplicable
    |> List
