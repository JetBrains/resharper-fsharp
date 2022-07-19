namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(expr: IFSharpExpression, replacementTypeName: string) =
    inherit FSharpQuickFixBase()

    let mostOuterParentExpr = expr.GetOutermostParentExpressionFromItsReturn()
    let binding = BindingNavigator.GetByExpression(mostOuterParentExpr)

    new (error: TypeConstraintMismatchError) =
        // error FS0193: Type constraint mismatch. The type ↔    'A.B'    ↔is not compatible with type↔    'Thing'
        ReplaceReturnTypeFix(error.Expr, error.MismatchedType)
    
    new (error: TypeDoesNotMatchTypeError) =
        // error FS0001: The type 'double' does not match the type 'int'
        ReplaceReturnTypeFix(error.Expr, error.ActualType)

    (*
        match () with
        | _ -> ""
    *)
    new (error: TypeEquationError) =
        // error FS0001: This expression was expected to have type↔    'int'    ↔but here has type↔    'string'
        ReplaceReturnTypeFix(error.Expr, error.ActualType)

    new (error: IfExpressionNeedsTypeToSatisfyTypeRequirementsError) =
        // error FS0001: The 'if' expression needs to have type 'string' to satisfy context type requirements. It currently has type 'int'.
        ReplaceReturnTypeFix(error.Expr, error.ActualType)

    new (error: TypeMisMatchTuplesHaveDifferingLengthsError) =
        // Type mismatch. Expecting a
        //     'string * string'    
        // but given a
        //     'string * string * 'a'
        // The tuples have differing lengths of 2 and 3
        ReplaceReturnTypeFix(error.Expr, error.ActualType)

    new (error: MatchClauseWrongTypeError) =
        // All branches of a pattern match expression must return values implicitly convertible to the type of the first branch, which here is 'int'.
        // This branch returns a value of type 'string'.
        ReplaceReturnTypeFix(error.Expr, error.ActualType)

    override this.Text =
        let name = 
            match binding.GetHeadPatternName() with
            | SharedImplUtil.MISSING_DECLARATION_NAME -> "binding"
            | name -> $"'{name}'"

        $"Change type of {name} to '{replacementTypeName}'"

    override this.IsAvailable _ =
        if isNull binding || isNull binding.ReturnTypeInfo then false else
        if not (binding.HeadPattern :? IReferencePat) then false else

        // An invalid binary infix application will yield a similar error and could be mistaken for an invalid return type.
        // F.ex. 1 + "a", error FS0001: The type 'string' does not match the type 'int'
        // We ignore this scenario for now.
        match mostOuterParentExpr with
        | :? IBinaryAppExpr
        | :? IMatchLambdaExpr -> false
        | _ -> true

    override this.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let bindingReturnTypeInfo = binding.ReturnTypeInfo
        if isNotNull bindingReturnTypeInfo && isNotNull bindingReturnTypeInfo.ReturnType then
            let typeUsage = binding.CreateElementFactory().CreateTypeUsage(replacementTypeName)
            let returnTypeUsage = bindingReturnTypeInfo.ReturnType.IgnoreInnerParens()

            ModificationUtil.ReplaceChild(returnTypeUsage, typeUsage) |> ignore
