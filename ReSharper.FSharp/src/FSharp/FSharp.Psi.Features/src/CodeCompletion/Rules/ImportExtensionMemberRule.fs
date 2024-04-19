namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Lookup
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Util
open JetBrains.UI.RichText
open JetBrains.Util.DataStructures.Collections

[<Language(typeof<FSharpLanguage>)>]
type ImportExtensionMemberRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getQualifierExpr (context: FSharpCodeCompletionContext) =
        let reparsedContext = context.ReparsedContext
        let reference = reparsedContext.Reference
        if isNull reference then Unchecked.defaultof<_> else

        let refExpr = reference.GetTreeNode().As<IReferenceExpr>()
        if isNull refExpr then Unchecked.defaultof<_> else

        refExpr.Qualifier

    override this.IsAvailable(context) =
        context.IsQualified &&

        let qualifierExpr = getQualifierExpr context
        isNotNull qualifierExpr &&

        let fcsType = qualifierExpr.TryGetFcsType()
        isNotNull fcsType

    override this.AddLookupItems(context, collector) =
        let qualifierExpr = getQualifierExpr context
        let fcsType = qualifierExpr.TryGetFcsType()
        let exprType = fcsType.MapType(context.NodeInFile)

        let symbolScope = getSymbolScope context.PsiModule true
        use namespaceQueue = PooledQueue<INamespace>.GetInstance()
        namespaceQueue.Enqueue(symbolScope.GlobalNamespace)

        let openedModulesProvider = OpenedModulesProvider(qualifierExpr.FSharpFile)
        let scopes = openedModulesProvider.OpenedModuleScopes
        let psiServices = context.PsiModule.GetPsiServices()
        let solution = psiServices.Solution
        let iconManager = solution.GetComponent<PsiIconManager>()

        let result = List()

        let addMethods (ns: INamespace) =
            let scopes = scopes.GetValuesSafe(ns.ShortName) // todo: use qualified names in the map
            if OpenScope.inAnyScope qualifierExpr scopes then () else

            let ns = ns.As<Namespace>()
            // todo: compiled
            for extensionMethodsIndex in ns.SourceExtensionMethods do
                for extensionMethodProxy in extensionMethodsIndex.Lookup() do
                    let sourceFile = extensionMethodProxy.TryGetSourceFile()
                    if not (isValid sourceFile) || sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() then () else

                    let methods = extensionMethodProxy.FindExtensionMethod()
                    for method in methods do
                        let parameters = method.Parameters
                        if parameters.Count = 0 then () else

                        let thisParam = parameters[0]
                        if exprType.IsSubtypeOf(thisParam.Type) then
                            result.Add(method)

        while namespaceQueue.Count > 0 do
            let ns = namespaceQueue.Dequeue()

            addMethods ns

            for nestedNamespace in ns.GetNestedNamespaces(symbolScope) do
                namespaceQueue.Enqueue(nestedNamespace)

        for method in result do
            let name = method.ShortName
            let containingType = method.ContainingType
            let ns = containingType.GetContainingNamespace().QualifiedName
            let info = ImportInfo(containingType, name, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let name = RichText(name)
                        LookupUtil.AddInformationText(name, $"(in {ns})")
                        TextualPresentation(name, info, iconManager.GetImage(method, method.PresentationLanguage, true)))
                    .WithBehavior(fun _ -> ImportBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(name, info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.ImportedType)

            collector.Add(item)

        false
