﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System.Linq
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithAssignmentExpressionFix(expr: IBinaryAppExpr) =
    inherit FSharpQuickFixBase()

    new (error: UnitTypeExpectedError) =
        ReplaceWithAssignmentExpressionFix(error.Expr.As<IBinaryAppExpr>())

    new (warning: UnitTypeExpectedWarning) =
        ReplaceWithAssignmentExpressionFix(warning.Expr.As<IBinaryAppExpr>())

    override x.IsAvailable _ =
        if not (isValid expr && isPredefinedFunctionRef "=" expr.Operator) then false else

        match expr.LeftArgument with
        | :? IReferenceExpr as ref ->
            let declaredElement = ref.Reference.Resolve().DeclaredElement

            match ref.Reference.GetFcsSymbol() with
            | :? FSharpField as field ->
                field.IsMutable ||

                not (declaredElement :? ICompiledElement) &&
                match field.DeclaringEntity with
                | Some(entity) -> entity.IsFSharpRecord
                | None -> false

            | :? FSharpMemberOrFunctionOrValue as memberOrFunctionOrValue ->
                if memberOrFunctionOrValue.IsMember then
                    memberOrFunctionOrValue.IsMutable || memberOrFunctionOrValue.HasSetterMethod
                else
                    let referencePat = declaredElement.GetDeclarations().FirstOrDefault().As<IReferencePat>()
                    let binding = BindingNavigator.GetByHeadPattern(referencePat)
                    isNotNull binding && not binding.HasParameters
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
