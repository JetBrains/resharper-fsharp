namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer([| typeof<IParametersOwnerPat> |],
                         HighlightingTypes = [| typeof<RedundantUnionCaseFieldPatternsWarning> |])>]
type RedundantUnionCaseFieldPatsAnalyzer() =
    inherit ElementProblemAnalyzer<IParametersOwnerPat>()

    let isWildTuplePat (pat: ITuplePat) =
        pat.PatternsEnumerable |> Seq.forall (fun pat -> pat.IgnoreInnerParens() :? IWildPat)

    let isApplicable (pat: IParenPat) =
        let tuplePat = pat.Pattern.IgnoreInnerParens().As<ITuplePat>()
        isNotNull tuplePat && isWildTuplePat tuplePat

    override x.Run(pat, _, consumer) =
        let parenPat = pat.ParametersEnumerable.SingleItem.As<IParenPat>()
        if isNotNull parenPat && isApplicable parenPat then
            match pat.ReferenceName.Reference.GetFSharpSymbol() with
            | :? FSharpUnionCase -> consumer.AddHighlighting(RedundantUnionCaseFieldPatternsWarning(parenPat))
            | _ -> ()
