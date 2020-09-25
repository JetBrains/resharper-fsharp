namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type AddDiscriminatedUnionAllClauses(warning: MatchIncompleteWarning) =
    inherit FSharpQuickFixBase()

    override x.Text = "Autogenerate all discriminated union cases"

    override x.IsAvailable _ =
        let fsharpType = warning.Expr.Expression.TryGetFcsType()
        
        isValid warning.Expr
        && (isNotNull fsharpType)
        && fsharpType.HasTypeDefinition
        && warning.Expr.Clauses.IsEmpty
        && fsharpType.TypeDefinition.IsFSharpUnion

    override x.ExecutePsiTransaction _ =
        let expr = warning.Expr
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()
        use enableFormatter = FSharpRegistryUtil.AllowFormatterCookie.Create()

        let entity = warning.Expr.Expression.TryGetFcsType().TypeDefinition
            
        let nodesToAdd =
            entity.UnionCases
            |> Seq.collect(fun case ->
                [NewLine(expr.GetLineEnding()) :> ITreeNode
                 factory.CreateMatchClause(case.DisplayName, case.HasFields) :> ITreeNode])
            |> Seq.toList
        
        addNodesAfter expr.WithKeyword nodesToAdd |> ignore
