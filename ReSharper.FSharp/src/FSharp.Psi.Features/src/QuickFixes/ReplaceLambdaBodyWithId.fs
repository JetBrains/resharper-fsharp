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

    override x.IsAvailable _ =
        isValid lambda &&
        resolvesToPredefinedFunction lambda.RArrow "id" "ReplaceLambdaBodyWithIdFix"

    override x.Text = "Replace lambda body with 'id'"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(lambda.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = lambda.CreateElementFactory()
                
        deletePatternsFromEnd lambda 1

        lambda.SetExpression(factory.CreateReferenceExpr("id")) |> ignore
