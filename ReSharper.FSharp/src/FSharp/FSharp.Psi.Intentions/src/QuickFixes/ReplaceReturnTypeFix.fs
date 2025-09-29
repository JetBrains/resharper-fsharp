namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(expr: IFSharpExpression, diagnosticInfo: FcsCachedDiagnosticInfo) =
    inherit FSharpQuickFixBase()

    let unwrapDecl (decl: IDeclaration) =
        match decl with
        | :? IFSharpTypeOwnerDeclaration as typeOwnerDecl -> typeOwnerDecl
        | :? IReferencePat as refPat -> refPat.Binding
        | _ -> null

    let skipParentLambdaBodies (expr: IFSharpExpression) =
        let rec loop acc (expr: IFSharpExpression) =
            let lambdaExpr = LambdaExprNavigator.GetByExpression(expr)
            if isNotNull lambdaExpr then
                loop (acc + 1) lambdaExpr
            else
                expr, acc

        loop 0 expr

    let expr, path = FSharpTypeUsageUtil.getTupleParentNavigationPath expr
    let expr = expr.GetOutermostParentExpressionFromItsReturn()
    let expr, lambdaParametersCount = skipParentLambdaBodies expr

    let decl: IFSharpTypeOwnerDeclaration = FSharpTypeOwnerDeclarationNavigator.GetByExpression(expr)
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
            let nameSubstring =
                match decl.SourceName with
                | SharedImplUtil.MISSING_DECLARATION_NAME -> " of binding"
                | name -> $" of '{name}'"

            $"Change type{nameSubstring} to '{actualFcsType.Format()}'"

    override this.IsAvailable _ =
        let canUpdateReturnType (typeUsage: ITypeUsage) =
            let typeUsage = FSharpTypeUsageUtil.navigateTuplePath path typeUsage
            isNotNull typeUsage

        // An invalid binary infix application will yield a similar error and could be mistaken for an invalid return type.
        // F.ex. 1 + "a", error FS0001: The type 'string' does not match the type 'int'
        // We ignore this scenario for now.
        match expr with
        | :? IBinaryAppExpr
        | :? IMatchLambdaExpr -> false
        | _ ->

        isNotNull decl &&
        isNotNull decl.DeclaredElement &&

        let returnTypeUsage = decl.TypeUsage
        isNull returnTypeUsage || canUpdateReturnType returnTypeUsage

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(decl.IsPhysical())

        let paramCount =
            match decl with
            | :? IParameterOwnerMemberDeclaration as paramOwnerDecl -> paramOwnerDecl.ParametersDeclarations.Count
            | _ -> 0

        for decl in decl.DeclaredElement.GetDeclarations() do
            let decl = unwrapDecl decl
            if isNull decl then () else

            let paramsToSkip =
                if decl.IsFSharpSigFile() then
                    paramCount + lambdaParametersCount
                else
                    lambdaParametersCount

            if isNull decl.TypeUsage then
                FSharpTypeUsageUtil.setFcsParametersOwnerReturnType decl

            decl.TypeUsage
            |> FSharpTypeUsageUtil.skipParameters paramsToSkip
            |> FSharpTypeUsageUtil.navigateTuplePath path
            |> FSharpTypeUsageUtil.updateTypeUsage actualFcsType
