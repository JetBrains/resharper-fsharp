namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(expr: IFSharpExpression, diagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit FSharpQuickFixBase()

    let expr, path = FSharpTypeUsageUtil.getTupleParentNavigationPath expr
    let mostOuterParentExpr = expr.GetOutermostParentExpressionFromItsReturn()
    let binding = BindingNavigator.GetByExpression(mostOuterParentExpr)
    let actualFcsType = diagnosticInfo.TypeMismatchData.ActualType

    new (error: TypeConstraintMismatchError) =
        // error FS0193: Type constraint mismatch. The type 'A.B' is not compatible with type 'Thing'
        ReplaceReturnTypeFix(error.Expr, error.DiagnosticInfo)

    new (error: TypeEquationError) =
        // error FS0001: This expression was expected to have type 'int' but here has type 'string'
        ReplaceReturnTypeFix(error.Expr, error.DiagnosticInfo)

    new (error: MatchClauseWrongTypeError) =
        // All branches of a pattern match expression must return values implicitly convertible to the type of the first branch, which here is 'int'.
        // This branch returns a value of type 'string'.
        ReplaceReturnTypeFix(error.Expr, error.DiagnosticInfo)

    override this.Text =
        if not path.IsEmpty then
            $"Change type to '{actualFcsType.Format()}'"
        else
            let name =
                match binding.GetHeadPatternName() with
                | SharedImplUtil.MISSING_DECLARATION_NAME -> "binding"
                | name -> $"'{name}'"

            $"Change type of {name} to '{actualFcsType.Format()}'"

    override this.IsAvailable _ =
        let canUpdateReturnType (returnTypeInfo: IReturnTypeInfo) =
            let returnTypeUsage = returnTypeInfo.ReturnType
            isNotNull returnTypeUsage &&

            let typeUsage = FSharpTypeUsageUtil.navigateTuplePath returnTypeUsage path
            isNotNull typeUsage

        // An invalid binary infix application will yield a similar error and could be mistaken for an invalid return type.
        // F.ex. 1 + "a", error FS0001: The type 'string' does not match the type 'int'
        // We ignore this scenario for now.
        match mostOuterParentExpr with
        | :? IBinaryAppExpr
        | :? IMatchLambdaExpr -> false
        | _ ->

        isNotNull binding && binding.HeadPattern :? IReferencePat &&

        let returnTypeInfo = binding.ReturnTypeInfo
        isNull returnTypeInfo || canUpdateReturnType returnTypeInfo

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())

        if isNull binding.ReturnTypeInfo then
            let factory = binding.CreateElementFactory()
            let typeUsage = factory.CreateTypeUsage("_", TypeUsageContext.TopLevel)
            let returnTypeInfo = factory.CreateReturnTypeInfo(typeUsage)
            binding.SetReturnTypeInfo(returnTypeInfo) |> ignore

        let typeUsage = FSharpTypeUsageUtil.navigateTuplePath binding.ReturnTypeInfo.ReturnType path
        FSharpTypeUsageUtil.updateTypeUsage actualFcsType typeUsage
