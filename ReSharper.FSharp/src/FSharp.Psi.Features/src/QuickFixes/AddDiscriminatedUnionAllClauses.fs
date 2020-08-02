namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open FSharp.Compiler.SourceCodeServices

type AddDiscriminatedUnionAllClauses(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    override x.Text = "Autogenerate all discriminated union cases"

    override x.IsAvailable _ =
        let refExpr = warning.Expr.Expression.As<IReferenceExpr>()
        if isNull refExpr then false else
        let symbol = refExpr.Reference.GetFSharpSymbol()
        let typeValidForFix =
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv ->
                mfv.FullType.HasTypeDefinition
                && mfv.FullType.TypeDefinition.IsFSharpUnion
            | _ -> false
            
        
        // TODO: In future could be made to handle partially complete cases, but for now just handle the simple empty case
        isValid warning.Expr
        && warning.Expr.Clauses.IsEmpty
        && typeValidForFix

    override x.ExecutePsiTransaction _ =
        let expr = warning.Expr
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()
        use enableFormatter = FSharpRegistryUtil.AllowFormatterCookie.Create()

        let refExpr = warning.Expr.Expression.As<IReferenceExpr>()
        let symbol = refExpr.Reference.GetFSharpSymbol()
        let entity =
            match symbol with
            | :? FSharpMemberOrFunctionOrValue as mfv -> mfv.FullType.TypeDefinition
            | _ -> failwith "FSharpMemberOrFunctionOrValue"
            
        let nodesToAdd =
            entity.UnionCases
            |> Seq.collect(fun case ->
                [NewLine(expr.GetLineEnding()) :> ITreeNode
                 factory.CreateMatchClause(case.DisplayName, case.HasFields) :> ITreeNode])
            |> Seq.toList
        
        addNodesAfter expr.WithKeyword nodesToAdd |> ignore
