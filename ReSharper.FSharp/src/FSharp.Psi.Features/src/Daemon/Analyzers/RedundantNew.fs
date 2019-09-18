namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer(typeof<INewExpr>,
                         HighlightingTypes = [| typeof<RedundantNewWarning> |])>]
type RedundantNewAnalyzer() =
    inherit ElementProblemAnalyzer<INewExpr>()

    override x.Run(newExpr, _, consumer) =
        let resolveResult = newExpr.TypeName.Reference.Resolve()
        match resolveResult.DeclaredElement.As<ITypeElement>() with
        | null -> ()
        | typeElement ->

        let predefinedType = newExpr.GetPsiModule().GetPredefinedType()
        if typeElement.IsDescendantOf(predefinedType.IDisposable.GetTypeElement()) then() else

        if isNotNull newExpr.NewKeyword then
            consumer.AddHighlighting(RedundantNewWarning(newExpr))
