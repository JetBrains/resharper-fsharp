module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpExtensionMemberUtil

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
open JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods.Queries
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Util

type FSharpRequest(psiModule, exprType: IType, name: string option) =
    static let memberKinds = [ExtensionMemberKind.ExtensionMethod; FSharpExtensionMemberKind.FSharpExtensionMember]

    let name = Option.toObj name

    let baseTypes: IReadOnlyList<IType> =
        let exprDeclaredType = exprType.As<IDeclaredType>()
        if isNull exprDeclaredType then [] else

        let result = List<IType>()
        result.Add(exprDeclaredType)
        for superType in exprDeclaredType.GetAllSuperTypes() do
            result.Add(superType)

        result.AsReadOnly()

    interface IRequest with
        member this.Name = name
        member this.BaseExpressionTypes = baseTypes
        member this.ExpressionType = exprType
        member this.ForModule = psiModule
        member this.Kinds = memberKinds

        member this.IsCaseSensitive = true
        member this.Namespaces = []
        member this.Types = []

        member this.WithExpressionType _ = failwith "todo"
        member this.WithModule _ = failwith "todo"
        member this.WithName _ = failwith "todo"
        member this.WithNamespaces _ = failwith "todo"
        member this.WithTypes _ = failwith "todo"

let getQualifierExpr (reference: IReference) =
    let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
    if isNull refExpr then Unchecked.defaultof<_> else

    refExpr.Qualifier

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

        let exprTypeElement = exprType.GetTypeElement()
        if isNotNull exprTypeElement then
            typeElements.Add(exprTypeElement)
            typeElements.AddRange(exprTypeElement.GetSuperTypeElements())

        typeElements.AsReadOnly()

    let autoOpenCache = solution.GetComponent<FSharpAutoOpenCache>()
    let openedModulesProvider = OpenedModulesProvider(context.FSharpFile, autoOpenCache)
    let scopes = openedModulesProvider.OpenedModuleScopes
    let accessContext = ElementAccessContext(context)

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

            exprType.IsSubtypeOf(parameters[0].Type)

        | _ -> false

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

    let isApplicable (typeMember: ITypeMember) =
        matchesName typeMember &&
        not (isInScope typeMember) &&
        isAccessible typeMember &&
        matchesType typeMember

    let query = ExtensionMethodsQuery(solution.GetPsiServices(), FSharpRequest(psiModule, exprType, nameOpt))

    let methods = query.EnumerateMethods()

    methods
    |> Seq.filter isApplicable
    |> List
