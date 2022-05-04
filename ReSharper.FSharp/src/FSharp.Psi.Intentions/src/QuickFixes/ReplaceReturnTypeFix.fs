namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Text.RegularExpressions
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceReturnTypeFix(error: TypeConstraintMismatchError) =
    inherit FSharpQuickFixBase()
    
    let bodyExpr = error.Expr
    let getTypesFromErrorMessage (errorMessage:string) =
        let regexMatches = Regex.Matches(errorMessage, "'.+'")

        if regexMatches.Count <> 2 then
            None
        else
            let getType idx = regexMatches.[idx].Value.Trim([| '\'' |])
            Some (getType 0, getType 1)

    override this.Text = "Replace return type"
    override this.IsAvailable _ =
        let binding = BindingNavigator.GetByExpression(bodyExpr)
        if isNull binding then false else
        Option.isSome (getTypesFromErrorMessage error.FcsMessage)
    
    override this.ExecutePsiTransaction(_solution) =
        match getTypesFromErrorMessage error.FcsMessage with
        | None -> ()
        | Some (actualTypeName, expectedTypeName) ->

        let binding = BindingNavigator.GetByExpression(bodyExpr)
        
        use writeCookie = WriteLockCookie.Create(binding.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let refPat = binding.HeadPattern.As<IReferencePat>()
        if isNull refPat then () else

        let symbolUse = refPat.GetFcsSymbolUse()
        if isNull symbolUse then () else

        if isNotNull binding.ReturnTypeInfo then
            let factory = binding.CreateElementFactory()
            let typeUsage = factory.CreateTypeUsage(actualTypeName)
            let currentReturnType = binding.ReturnTypeInfo
            ModificationUtil.ReplaceChild(currentReturnType, factory.CreateReturnTypeInfo(typeUsage)) |> ignore
