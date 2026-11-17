namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpLambdaUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type SimplifyLambdaFix(warning: LambdaCanBeSimplifiedWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.LambdaExpr
    let replaceCandidate = warning.ReplaceCandidate

    override x.Text = "Simplify lambda"

    override x.IsAvailable _ = isValid lambda && isValid replaceCandidate

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(lambda.IsPhysical())

        let redundantArgsCount = getPrefixAppExprArgs replaceCandidate |> Seq.length
        ModificationUtil.ReplaceChild(lambda.Expression, replaceCandidate) |> ignore
        deletePatternsFromEnd lambda redundantArgsCount
