namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree

//[<ElementProblemAnalyzer([| typeof<ILiteralExpr> |], HighlightingTypes = [| typeof<UseNameofExpressionWarning> |])>]
type UseNameofExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<ILiteralExpr>()

    // todo: cache language level in data
    override this.Run(literalExpr, data, consumer) =
        if not (literalExpr.IsStringLiteralExpression()) then () else
        if not (FSharpLanguageLevel.isFSharp50Supported literalExpr) then () else

        let references = literalExpr.GetReferences<IReferenceFromStringLiteral>()
        if references.Count <> 1 then () else

        let reference = references.[0]
        let declaredElement = reference.Resolve().DeclaredElement
        if isNotNull declaredElement then
            consumer.AddHighlighting(UseNameofExpressionWarning(literalExpr, reference))
