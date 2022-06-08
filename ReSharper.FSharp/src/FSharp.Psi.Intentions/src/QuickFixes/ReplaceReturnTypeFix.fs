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

    override this.Text = "Replace return type"
    override this.IsAvailable _ =
        if isNull mostOuterParentExpr then false else
        let binding = BindingNavigator.GetByExpression(mostOuterParentExpr)
        if isNull binding then false else

        // Some types cannot be replaced properly solely on the FCS error message.
        // We will ignore these types for now.
        match binding.ReturnTypeInfo.ReturnType.IgnoreParentParens() with
        | :? ITupleTypeUsage
        | :? IFunctionTypeUsage -> false
        | _ ->
            // An invalid binary infix application will yield a similar error and could be mistaken for an invalid return type.
            // F.ex. 1 + "a", error FS0001: The type 'string' does not match the type 'int'
            // We ignore this scenario for now.
            match mostOuterParentExpr with
            | :? IBinaryAppExpr -> false
            | _ -> true

    override this.ExecutePsiTransaction(_solution) =
        let binding = BindingNavigator.GetByExpression(mostOuterParentExpr)
        
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        if isNotNull binding.ReturnTypeInfo
           && isNotNull binding.ReturnTypeInfo.ReturnType then
            let factory = binding.CreateElementFactory()
            let typeUsage = factory.CreateTypeUsage(replacementTypeName)
            let returnType = binding.ReturnTypeInfo.ReturnType.IgnoreInnerParens()

            match returnType with
            | :? INamedTypeUsage as ntu ->
                ModificationUtil.ReplaceChild(ntu, typeUsage) |> ignore
            | _ -> ()
