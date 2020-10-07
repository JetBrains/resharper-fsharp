namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

type ReplaceLambdaWithInnerExpressionFix(warning: LambdaCanBeReplacedWarning) =
    inherit ReplaceWithInnerExpressionFixBase(warning.LambdaExpr, warning.ReplaceCandidate)

    let replaceCandidate = warning.ReplaceCandidate

    override x.Text =
        match replaceCandidate with
        | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
            sprintf "Replace lambda with '%s'" ref.QualifiedName
        | :? IPrefixAppExpr -> "Replace lambda with partial application"
        | _ -> "Simplify lambda"
