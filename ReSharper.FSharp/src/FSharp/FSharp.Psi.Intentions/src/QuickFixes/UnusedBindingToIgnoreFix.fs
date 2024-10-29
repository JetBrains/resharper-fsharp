namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions.QuickFixes

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type UnusedBindingToIgnoreFix(warning: UnusedValueWarning) =
    inherit FSharpQuickFixBase()

    let rec isUnusedPattern (pat: IFSharpPattern) =
        let pat = pat.IgnoreInnerParens()

        match pat with
        | :? IReferencePat as refPat ->
            let mfv = refPat.GetFcsSymbol().As<FSharpMemberOrFunctionOrValue>()
            isNotNull mfv && not mfv.IsReferencedValue

        | :? ITuplePat as tuplePat ->
            tuplePat.PatternsEnumerable |> Seq.forall isUnusedPattern

        | :? IWildPat -> true

        | _ -> false

    let canMoveExpr (binding: IBinding) =
        let expr = binding.Expression
        if isNull expr then false else

        expr.IsSingleLine ||
        expr.StartLine > binding.EqualsToken.StartLine

    override this.Text = "Ignore expression"

    override this.IsAvailable(cache) =
        let pat = warning.Pat.IgnoreParentParens()
        let binding, _ = pat.GetBinding(false)
        let binding = binding.As<IBinding>()
        isNotNull binding &&

        isUnusedPattern binding.HeadPattern &&
        Seq.isEmpty binding.ParametersDeclarationsEnumerable &&

        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
        isNotNull letExpr &&

        canMoveExpr binding

    override this.ExecutePsiTransaction(solution) =
        base.ExecutePsiTransaction(solution)
