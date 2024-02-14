namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Generate
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.ObjExprUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ToObjectExpressionFix(error: AbstractTypeCannotBeInstantiatedError) =
    inherit FSharpQuickFixBase()

    let expr = error.Expr

    override this.Text = "Convert to object expression"

    override this.IsAvailable(cache) =
        let appExpr = expr.As<IPrefixAppExpr>()
        isNotNull appExpr &&

        let refExpr = appExpr.FunctionExpression.As<IReferenceExpr>()
        isNotNull refExpr &&

        NewObjPostfixTemplate.isApplicableExpr refExpr

    override this.ExecutePsiTransaction(solution, var0) =
        let factory = expr.CreateElementFactory()

        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let psiModule = expr.GetPsiModule()
        let objExpr = GenerateOverrides.convertToObjectExpression factory psiModule expr
        GenerateOverrides.selectObjExprMemberOrCallCompletion objExpr 
