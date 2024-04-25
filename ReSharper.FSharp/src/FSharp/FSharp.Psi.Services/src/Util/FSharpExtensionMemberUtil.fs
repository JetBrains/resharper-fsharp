module JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpExtensionMemberUtil

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.ExtensionMethods
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util.DataStructures.Collections

let getQualifierExpr (reference: IReference) =
    let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
    if isNull refExpr then Unchecked.defaultof<_> else

    refExpr.Qualifier

let getExtensionMembers (context: IFSharpTreeNode) (fcsType: FSharpType) =
    let exprType = fcsType.MapType(context)

    let psiModule = context.GetPsiModule()
    let symbolScope = getSymbolScope psiModule true
    use namespaceQueue = PooledQueue<INamespace>.GetInstance()
    namespaceQueue.Enqueue(symbolScope.GlobalNamespace)

    let openedModulesProvider = OpenedModulesProvider(context.FSharpFile)
    let scopes = openedModulesProvider.OpenedModuleScopes

    let result = List()

    let addMethods (ns: INamespace) =
        let scopes = scopes.GetValuesSafe(ns.ShortName) // todo: use qualified names in the map
        if OpenScope.inAnyScope context scopes then () else

        let addExtensionMethods (methodsIndex: IExtensionMethodsIndex) =
            if isNull methodsIndex then () else

            for extensionMethodProxy in methodsIndex.Lookup() do
                // C#-compatible extension methods are only seen as extensions in other languages
                // todo: expose language instead of checking source file
                let sourceFile = extensionMethodProxy.TryGetSourceFile()
                if isValid sourceFile && sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() then () else

                let methods = extensionMethodProxy.FindExtensionMethod()
                for method in methods do
                    if isFSharpAssembly method.Module then () else

                    let parameters = method.Parameters
                    if parameters.Count = 0 then () else

                    let thisParam = parameters[0]
                    if exprType.IsSubtypeOf(thisParam.Type) then
                        result.Add(method)

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
