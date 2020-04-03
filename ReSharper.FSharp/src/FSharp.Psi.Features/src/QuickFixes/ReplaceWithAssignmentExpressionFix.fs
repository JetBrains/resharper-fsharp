namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

type ReplaceWithAssignmentExpressionFix(warning: UnitTypeExpectedWarning) =
    inherit FSharpQuickFixBase()
   
    let expr = warning.Expr.As<IBinaryAppExpr>()
    override x.IsAvailable _ =
        if not (isValid expr) ||
            // Now we can’t find out if the operator is '=', so we define it bypassing the grammar rules
            // TODO: rewrite after https://youtrack.jetbrains.com/issue/RIDER-41848 fix
            not (getTokenType expr.Operator.FirstChild == FSharpTokenType.EQUALS) then false else

        match expr.LeftArgument with
        | :? IReferenceExpr as ref ->
            let declaredElement = ref.Reference.Resolve().DeclaredElement
            if declaredElement :? ICompiledElement then false else

            match ref.Reference.GetFSharpSymbol() with
            | :? FSharpField as field ->
                field.IsMutable ||
                match field.DeclaringEntity with
                | Some (entity) -> entity.IsFSharpRecord
                | None -> false

            | :? FSharpMemberOrFunctionOrValue as memberOrFunctionOrValue ->
                if memberOrFunctionOrValue.IsMember then memberOrFunctionOrValue.IsMutable else
                if memberOrFunctionOrValue.FullType.IsFunctionType then false else
                match declaredElement.GetDeclarations().[0] with
                | :? IReferencePat as pat -> isNotNull pat.Binding
                | _ -> false

            | _ -> false

        | :? IIndexerExpr -> true
        | _ -> false

    override x.Text = "Replace with '<-' assignment"
    
    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()
        let setExpr = factory.CreateSetExpr(expr.LeftArgument, expr.RightArgument)
        replace expr setExpr
