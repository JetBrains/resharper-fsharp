namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil

type ReplaceLambdaBodyWithIdFix(warning: LambdaBodyCanBeReplacedWithIdWarning) =
    inherit ReplaceWithReferenceExprFixBase(warning.Lambda.Expression, "id", "Microsoft.FSharp.Core.Operators.id")

    let lambda = warning.Lambda

    override x.ResolveContext = lambda.RArrow :> _
    override x.Text = "Replace lambda body with 'id'"
    override _.AdditionalExecute() = deletePatternsFromEnd lambda 1
