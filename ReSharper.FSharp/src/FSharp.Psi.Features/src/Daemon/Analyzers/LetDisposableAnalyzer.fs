namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Util

// todo: generate configurable severity

module LetDisposableAnalyzer =
    let isApplicable (letExpr: ILetOrUseExpr) =
        if not (isValid letExpr) then false else
        if letExpr.IsUse then false else

        let bindings = letExpr.Bindings
        if bindings.Count > 1 then false else

        let isApplicableNamedPat (pat: INamedPat) =
            match pat with
            | :? IReferencePat as refPat -> not refPat.ReferenceName.IsQualified
            | :? IAsPat -> true
            | _ -> false

        let namedPat = bindings.[0].HeadPattern.IgnoreInnerParens().As<INamedPat>()
        if isNull namedPat || not (isApplicableNamedPat namedPat) then false else

        let typeOwner = namedPat.DeclaredElement.As<ITypeOwner>()
        if isNull typeOwner then false else

        let typeElement = typeOwner.Type.GetTypeElement()
        if isNull typeElement then false else

        typeElement.IsDescendantOf(letExpr.GetPredefinedType().IDisposable.GetTypeElement())

//[<ElementProblemAnalyzer(typeof<ILetLikeExpr>,
//                         HighlightingTypes = [| typeof<RedundantNewWarning> |])>]
type LetDisposableAnalyzer() =
    inherit ElementProblemAnalyzer<ILetOrUseExpr>()

    override x.Run(letExpr, _, consumer) =
        if LetDisposableAnalyzer.isApplicable letExpr then
            consumer.AddHighlighting(ConvertToUseBindingWarning(letExpr))
