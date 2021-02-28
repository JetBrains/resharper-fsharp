namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type ReplaceLambdaWithInnerExpressionFix(warning: LambdaCanBeReplacedWithInnerExpressionWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.LambdaExpr, warning.ReplaceCandidate)

    let replaceCandidate = warning.ReplaceCandidate

    override x.Text =
        match replaceCandidate with
        | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
            $"Replace lambda with '{ref.QualifiedName}'"
        | :? IPrefixAppExpr as app ->
            match app.InvokedReferenceExpression with
            | null -> "Replace lambda with partial application"
            | refExpr -> $"Replace with '{refExpr.ShortName}' partial application"
        | _ -> "Simplify lambda"
