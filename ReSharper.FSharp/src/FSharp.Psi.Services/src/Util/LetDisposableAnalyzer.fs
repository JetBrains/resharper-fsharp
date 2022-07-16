namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

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

        let refPat = bindings[0].HeadPattern.IgnoreInnerParens().As<IReferencePat>()
        if isNull refPat then false else

        let typeOwner = refPat.DeclaredElement.As<ITypeOwner>()
        if isNull typeOwner then false else

        let typeElement = typeOwner.Type.GetTypeElement()
        if isNull typeElement then false else

        typeElement.IsDescendantOf(letExpr.GetPredefinedType().IDisposable.GetTypeElement())
