namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type ReplaceLambdaWithInnerExpressionFix(warning: LambdaCanBeReplacedWithInnerExpressionWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.LambdaExpr, warning.ReplaceCandidate)

    let replaceCandidate = warning.ReplaceCandidate

    override x.Text =
        match replaceCandidate with
        | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
            $"Replace lambda with '{ref.QualifiedName}'"
        | :? IPrefixAppExpr as app ->
            match getRootFunctionExpr app with
            | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
                $"Replace with '{ref.QualifiedName}' partial application"
            | _ -> "Replace lambda with partial application"
        | _ -> "Simplify lambda"
