namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util

// todo: generate configurable severity

//[<ElementProblemAnalyzer(typeof<ILetLikeExpr>,
//                         HighlightingTypes = [| typeof<RedundantNewWarning> |])>]
type LetDisposableAnalyzer() =
    inherit ElementProblemAnalyzer<ILetOrUseExpr>()

    override x.Run(letExpr, _, consumer) =
        if letExpr.IsUse then () else

        let bindings = letExpr.Bindings
        if bindings.Count > 1 then () else

        let binding = bindings.[0]
        let referencePat = binding.HeadPattern.IgnoreInnerParens().As<IReferencePat>()
        if isNull referencePat then () else

        if isNotNull referencePat.ReferenceName.Qualifier then () else

        let typeOwner = referencePat.DeclaredElement.As<ITypeOwner>()
        if isNull typeOwner then () else

        let typeElement = typeOwner.Type.GetTypeElement()
        if isNull typeElement then () else

        if not (typeElement.IsDescendantOf(letExpr.GetPredefinedType().IDisposable.GetTypeElement())) then () else

        consumer.AddHighlighting(ConvertToUseBindingWarning(letExpr))
