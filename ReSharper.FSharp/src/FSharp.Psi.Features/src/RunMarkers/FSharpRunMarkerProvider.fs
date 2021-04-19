module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.RunMarkers

open JetBrains.Application.Settings
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.Psi.EntryPoints
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Rider.Backend.Features.RunMarkers
open JetBrains.Util

let isEntryPoint (binding: ITopBinding) =
    let attribute = binding.AllAttributes.GetAttribute("EntryPoint")
    if isNull attribute then false else

    // todo: check attr resolves to FSharpPredefinedType.entryPointAttrTypeName
    let entryPointApplicability = EntryPointUtil.CheckTopLevelEntryPointApplicability(binding.GetPsiModule())
    if not entryPointApplicability.IsEntryPoint then false else

    let method = binding.DeclaredElement.As<IMethod>()
    if isNull method then false else

    let returnType = method.ReturnType
    if not (returnType.IsInt()) then false else

    let parameter = method.Parameters.SingleItem()
    if isNull parameter || parameter.Kind <> ParameterKind.VALUE then false else

    match parameter.Type with
    | :? IArrayType as parameterType -> parameterType.ElementType.IsString()
    | _ -> false

let isApplicableMethod (binding: IBinding) =
    let pattern = binding.ParameterPatterns.SingleItem.IgnoreInnerParens().As<IUnitPat>()
    if isNull pattern then false else

    let refPat = binding.HeadPattern.As<IReferencePat>()
    if isNull refPat then false else

    let method = refPat.DeclaredElement.As<IMethod>()
    isNotNull method && RunMarkerUtil.IsSuitableStaticMethod(method)

[<Language(typeof<FSharpLanguage>)>]
type FSharpRunMarkerProvider() =
    interface IRunMarkerProvider with
        member this.CollectRunMarkers(file, settings, consumer) =
            let showMarkerOnStaticMethods = settings.GetValue(fun (s: RunMarkerSettings) -> s.ShowMarkerOnStaticMethods)
            let showMarkerOnEntryPoint = settings.GetValue(fun (s: RunMarkerSettings) -> s.ShowMarkerOnEntryPoint)
            if not showMarkerOnStaticMethods && not showMarkerOnEntryPoint then () else

            let fsFile = file.As<IFSharpFile>()
            if isNull fsFile then () else

            let addHighlighting (binding: ITopBinding) markerId =
                let range = binding.GetNameDocumentRange()
                let targetFrameworkId = file.GetPsiModule().TargetFrameworkId
                let method = binding.DeclaredElement :?> IMethod
                let highlighting = RunMarkerHighlighting(method, binding, markerId, range, targetFrameworkId)
                consumer.AddHighlighting(highlighting, range)

            for binding in CachedDeclarationsCollector.Run<ITopBinding>(fsFile) do
                let parametersOwner = binding.HeadPattern.As<IParametersOwnerPat>()
                if isNull parametersOwner then () else

                let letBindings = LetBindingsDeclarationNavigator.GetByBinding(binding)
                if letBindings.IsInline || isNull (ModuleDeclarationNavigator.GetByMember(letBindings)) then () else

                if showMarkerOnStaticMethods && isApplicableMethod binding then
                    addHighlighting binding RunMarkerAttributeIds.RUN_METHOD_MARKER_ID

                if showMarkerOnEntryPoint && isEntryPoint binding then
                    addHighlighting binding RunMarkerAttributeIds.RUN_ENTRY_POINT_MARKER_ID

        member this.Priority = RunMarkerProviderPriority.DEFAULT
