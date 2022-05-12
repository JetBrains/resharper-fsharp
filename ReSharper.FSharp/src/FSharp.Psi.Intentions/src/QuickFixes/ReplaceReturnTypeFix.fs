namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(expr: IFSharpExpression, fcsErrorMessage: string, replaceFirstType: bool) =
    inherit FSharpQuickFixBase()
    
    let rec visitParent (expr: IFSharpExpression): IFSharpExpression option =
        match expr.Parent with
        | :? ISequentialExpr as seqExpr ->
            if seqExpr.Expressions.Last() <> expr then None else
            visitParent seqExpr
        | :? IMatchClause as clause ->
            match clause.Parent with
            | :? IFSharpExpression as parent -> visitParent parent
            | _ -> None
        | :? IFSharpExpression as parent -> visitParent parent
        | _ -> Some expr
    
    let parentExpr = visitParent expr
    
    let getReplacementTypeFromErrorMessage (errorMessage:string) =
        let regexMatches = Regex.Matches(errorMessage, "'((\\w|\.|\d)+)'")

        if regexMatches.Count <> 2 then
            None
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

    override this.Text = "Replace return type"
    override this.IsAvailable _ =
        match parentExpr with
        | None -> false
        | Some parentExpr ->

        let binding = BindingNavigator.GetByExpression(parentExpr)
        if isNull binding then false else
        Option.isSome (getReplacementTypeFromErrorMessage fcsErrorMessage)
    
    override this.ExecutePsiTransaction(_solution) =
        match getReplacementTypeFromErrorMessage fcsErrorMessage with
        | None -> ()
        | Some replacementTypeName ->

        parentExpr
        |> Option.iter (fun parentExpr ->
            let binding = BindingNavigator.GetByExpression(parentExpr)
            
            use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let refPat = binding.HeadPattern.As<IReferencePat>()
            if isNull refPat then () else

            let symbolUse = refPat.GetFcsSymbolUse()
            if isNull symbolUse then () else

            if isNotNull binding.ReturnTypeInfo then
                let factory = binding.CreateElementFactory()
                let typeUsage = factory.CreateTypeUsage(replacementTypeName)
                let currentReturnType = binding.ReturnTypeInfo
                ModificationUtil.ReplaceChild(currentReturnType, factory.CreateReturnTypeInfo(typeUsage)) |> ignore)
