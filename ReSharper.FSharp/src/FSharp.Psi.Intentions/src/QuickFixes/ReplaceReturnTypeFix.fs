namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(expr: IFSharpExpression, fcsErrorMessage: string, replaceFirstType: bool) =
    inherit FSharpQuickFixBase()

    let parentExpr = expr.GetOuterMostParentExpression()
    
    let getReplacementTypeFromErrorMessage (errorMessage:string) =
        let regexMatches = Regex.Matches(errorMessage, "'((\\w|\.|\d)+)'")

        if regexMatches.Count < 2 && regexMatches.Count > 3 then
            None
        elif regexMatches.Count = 3 then
            let idx = if replaceFirstType then 1 else 2
            Some (regexMatches[idx].Value.Trim([| '\'' |]))
        else
            let idx = if replaceFirstType then 0 else 1
            Some (regexMatches[idx].Value.Trim([| '\'' |]))

    new (error: TypeConstraintMismatchError) =
        // error FS0193: Type constraint mismatch. The type ↔    'A.B'    ↔is not compatible with type↔    'Thing'
        ReplaceReturnTypeFix(error.Expr, error.FcsMessage, true)
    
    new (error: TypeDoesNotMatchTypeError) =
        // error FS0001: The type 'double' does not match the type 'int'
        ReplaceReturnTypeFix(error.Expr, error.FcsMessage, false)

    (*
        match () with
        | _ -> ""
    *)
    new (error: TypeEquationError) =
        // error FS0001: This expression was expected to have type↔    'int'    ↔but here has type↔    'string'
        ReplaceReturnTypeFix(error.Expr, error.FcsMessage, false)

    new (error: IfExpressionNeedsTypeToSatisfyTypeRequirementsError) =
        // error FS0001: The 'if' expression needs to have type 'string' to satisfy context type requirements. It currently has type 'int'.
        ReplaceReturnTypeFix(error.Expr, error.FcsMessage, false)
    
    override this.Text = "Replace return type"
    override this.IsAvailable _ =
        if isNull parentExpr then false else
        let binding = BindingNavigator.GetByExpression(parentExpr)
        if isNull binding then false else
        Option.isSome (getReplacementTypeFromErrorMessage fcsErrorMessage)
    
    override this.ExecutePsiTransaction(_solution) =
        match getReplacementTypeFromErrorMessage fcsErrorMessage with
        | None -> ()
        | Some replacementTypeName ->

        let binding = BindingNavigator.GetByExpression(parentExpr)
        
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
                
            match binding.ReturnTypeInfo.ReturnType with
            | :? INamedTypeUsage as ntu ->
                ModificationUtil.ReplaceChild(ntu, typeUsage) |> ignore
            | :? IFunctionTypeUsage as ftu ->
                ftu.SetReturnTypeUsage(typeUsage) |> ignore
            | _ -> ()
