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
let (|FSharpExtensionMember|_|) (typeMember: ITypeMember) =
    match typeMember with
    | :? IFSharpTypeMember as fsTypeMember ->
        let mfv = fsTypeMember.Symbol.As<FSharpMemberOrFunctionOrValue>()
        if isNull mfv || not mfv.IsExtensionMember then ValueNone else

        match mfv.DeclaringEntity with
        | Some fcsEntity when fcsEntity.IsFSharpModule -> ValueSome mfv
        | _ -> ValueNone
    | _ -> ValueNone

let getExtensionMembers (context: IFSharpTreeNode) (fcsType: FSharpType) =
    let psiModule = context.GetPsiModule()
    use compilationCookie = CompilationContextCookie.GetOrCreate(psiModule.GetContextFromModule())
    let exprType = fcsType.MapType(context)

    use namespaceQueue = PooledQueue<INamespace>.GetInstance()
    let symbolScope = getSymbolScope psiModule true
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
        match typeMember with
        | FSharpExtensionMember mfv ->
            // todo: arrays and other non-declared-types?
            let exprTypeElement = exprType.GetTypeElement()
            if isNull exprTypeElement then false else

            let extendedTypeElement = mfv.ApparentEnclosingEntity.GetTypeElement(typeMember.Module)
            if isNull extendedTypeElement then false else

            exprTypeElement.Equals(extendedTypeElement) ||
            exprTypeElement.GetSuperTypeElements() |> Seq.exists extendedTypeElement.Equals

        | :? IMethod as method ->
            let parameters = method.Parameters
            if parameters.Count = 0 then false else

            exprType.IsSubtypeOf(parameters[0].Type)

        | _ -> false

    let addMethods (ns: INamespace) =
        let addExtensionMethods (methodsIndex: IExtensionMethodsIndex) =
            if isNull methodsIndex then () else

            for extensionMethodProxy in methodsIndex.Lookup() do
                // C#-compatible extension methods are only seen as extensions in other languages
                // todo: expose language instead of checking source file
                let sourceFile = extensionMethodProxy.TryGetSourceFile()
                if not (isValid sourceFile) then () else

                let members = extensionMethodProxy.FindExtensionMember()
                for typeMember in members do
                    // todo: check module/member is accessible
                    // todo: use extension member info to check kind and skip namespaces
                    if isInScope typeMember then () else

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
