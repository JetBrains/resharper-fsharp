namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util.PsiUtil
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type AddIgnoreFix(warning: UnitTypeExpectedWarning) =
    inherit QuickFixBase()

    let expr = warning.Expr

    override x.Text = "Ignore value"
    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let elementFactory = expr.FSharpLanguageService.CreateElementFactory(expr.GetPsiModule())
        replace expr (elementFactory.CreateIgnoreApp(expr.Copy())) 
        null
