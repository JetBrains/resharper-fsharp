module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpExtensionMemberUtil

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util.DataStructures.Collections

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

let getExtensionMembers (context: IFSharpTreeNode) (fcsType: FSharpType) =
    let psiModule = context.GetPsiModule()
    use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())
    let exprType = fcsType.MapType(context)

    use namespaceQueue = PooledQueue<INamespace>.GetInstance()
    let symbolScope = getSymbolScope psiModule true
    let accessContext = ElementAccessContext(context)
    namespaceQueue.Enqueue(symbolScope.GlobalNamespace)

    let openedModulesProvider = OpenedModulesProvider(context.FSharpFile)
    let scopes = openedModulesProvider.OpenedModuleScopes

    let result = List()

    let isInScope (typeMember: ITypeMember) =
        let isInScope name =
            // todo: use qualified names in the map
            let scopes = scopes.GetValuesSafe(name)
            OpenScope.inAnyScope context scopes
        
        match typeMember.ContainingType with
        | :? IFSharpModule as fsModule ->
            isInScope fsModule.SourceName

        | containingType ->
            let ns = containingType.GetContainingNamespace()
            isInScope ns.ShortName

    let matchesType (typeMember: ITypeMember) (exprType: IType) : bool =
        let matchesWithoutSubstitution (extendedTypeElement: ITypeElement) =
            if isNull extendedTypeElement then false else

            // todo: arrays and other non-declared-types?
            let exprTypeElement = exprType.GetTypeElement()
            if isNull exprTypeElement then false else
                
            exprTypeElement.Equals(extendedTypeElement) ||
            exprTypeElement.GetSuperTypeElements() |> Seq.exists extendedTypeElement.Equals

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

    let isAccessible (typeMember: ITypeMember) =
        let isTypeAccessible = 
            let containingType = typeMember.ContainingType
            let accessRightsOwner = containingType :?> IAccessRightsOwner
            match accessRightsOwner.GetAccessRights() with
            | AccessRights.PUBLIC -> true
            | _ -> containingType.Module.AreInternalsVisibleTo(psiModule)

        isTypeAccessible &&
        AccessUtil.IsSymbolAccessible(typeMember, accessContext)

    let addMethods (ns: INamespace) =
        let addExtensionMethods (methodsIndex: IExtensionMethodsIndex) =
            if isNull methodsIndex then () else

            for extensionMethodProxy in methodsIndex.Lookup() do
                let members = extensionMethodProxy.FindExtensionMember()
                for typeMember in members do
                    // todo: use extension member info to check kind and skip namespaces
                    if isInScope typeMember then () else
                    if not (isAccessible typeMember) then () else

                    if matchesType typeMember exprType then
                        result.Add(typeMember)

        let ns = ns.As<Namespace>()

        for extensionMethodsIndex in ns.SourceExtensionMethods do
            addExtensionMethods extensionMethodsIndex

        addExtensionMethods ns.CompiledExtensionMethods

    while namespaceQueue.Count > 0 do
        let ns = namespaceQueue.Dequeue()

        addMethods ns

        for nestedNamespace in ns.GetNestedNamespaces(symbolScope) do
            namespaceQueue.Enqueue(nestedNamespace)

    result
