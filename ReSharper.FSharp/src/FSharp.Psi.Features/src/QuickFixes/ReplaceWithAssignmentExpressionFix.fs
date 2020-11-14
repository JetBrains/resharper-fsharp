namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithAssignmentExpressionFix(warning: UnitTypeExpectedWarning) =
    inherit FSharpQuickFixBase()
   
    let expr = warning.Expr.As<IBinaryAppExpr>()

    override x.IsAvailable _ =
        if not (isValid expr && isPredefinedFunctionRef "=" expr.Operator) then false else

        match expr.LeftArgument with
        | :? IReferenceExpr as ref ->
            let declaredElement = ref.Reference.Resolve().DeclaredElement

            match ref.Reference.GetFSharpSymbol() with
            | :? FSharpField as field ->
                field.IsMutable ||
                not (declaredElement :? ICompiledElement) &&
                match field.DeclaringEntity with
                | Some (entity) -> entity.IsFSharpRecord
                | None -> false

            | :? FSharpMemberOrFunctionOrValue as memberOrFunctionOrValue ->
                if memberOrFunctionOrValue.IsMember then
                    memberOrFunctionOrValue.IsMutable || memberOrFunctionOrValue.HasSetterMethod
                else
                    match declaredElement.GetDeclarations() |> Seq.tryHead with
                    | Some decl when (decl :? IReferencePat) -> isNotNull (decl :?> IReferencePat).Binding
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
