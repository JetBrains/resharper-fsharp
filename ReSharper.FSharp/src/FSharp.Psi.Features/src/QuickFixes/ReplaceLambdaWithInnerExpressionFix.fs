namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI

type ReplaceLambdaWithInnerExpressionFix(warning: LambdaCanBeReplacedWithInnerExpressionWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.LambdaExpr, warning.ReplaceCandidate)

    let replaceCandidate = warning.ReplaceCandidate

    let rec getRootFunctionExpr (app : IPrefixAppExpr) =
        match app.FunctionExpression.IgnoreInnerParens() with
        | :? IPrefixAppExpr as previousApp -> getRootFunctionExpr previousApp
        | x -> x

    override x.Text =
        match replaceCandidate with
        | :? IReferenceExpr as ref when isSimpleQualifiedName ref ->
            $"Replace lambda with '{ref.QualifiedName}'"
        | :? IPrefixAppExpr as app ->
            match getRootFunctionExpr app with
            | :? IReferenceExpr as ref ->
                match ref.ShortName with
                | name when name = SharedImplUtil.MISSING_DECLARATION_NAME ||
                            PrettyNaming.IsOperatorName name ->
                    $"Replace with '{ref.Reference.GetName()}' partial application"
                | _ ->
                    $"Replace with '{ref.ShortName}' partial application"
            | _ -> "Replace lambda with partial application"
        | _ -> "Simplify lambda"
