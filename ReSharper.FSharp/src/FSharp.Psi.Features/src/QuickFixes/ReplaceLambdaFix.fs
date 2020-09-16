namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceLambdaFix(warning: LambdaCanBeReplacedWarning) =
    inherit FSharpQuickFixBase()

    let lambda = warning.LambdaExpr
    let replaceCandidate = warning.ReplaceCandidate

    override x.Text =
        match replaceCandidate with
        | :? IReferenceExpr as ref -> sprintf "Replace lambda with '%s'" ref.ShortName
        | :? IPrefixAppExpr -> "Replace lambda with partial application"
        | _ -> "Simplify lambda"

    override x.IsAvailable _ = isValid lambda && isValid replaceCandidate

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(lambda.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let indentDiff = lambda.Indent - replaceCandidate.Indent
        let expr = ModificationUtil.ReplaceChild(lambda, replaceCandidate)
        shiftExpr indentDiff expr
