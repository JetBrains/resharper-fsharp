namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaBodyWithIdFix(warning: LambdaBodyCanBeReplacedWithIdWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.Lambda
    let expr = lambda.Expression

    override x.IsAvailable _ =
        isValid expr &&
        not (isPredefinedFunctionShadowed lambda.RArrow "id" "ReplaceLambdaBodyWithIdFix")

    override x.Text = "Replace lambda body with 'id'"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()
                
        deletePatternsFromEnd lambda 1

        replace expr (factory.CreateReferenceExpr("id"))
