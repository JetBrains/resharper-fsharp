namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree

[<ElementProblemAnalyzer(typeof<FSharpDeadCodeToken>,
                         HighlightingTypes = [| typeof<ReSharperSyntaxHighlighting> |])>]
type InactivePreprocessorBranchAnalyzer() =
    inherit ElementProblemAnalyzer<FSharpDeadCodeToken>()

    let [<Literal>] highlightingId = DefaultLanguageAttributeIds.PREPROCESSOR_INACTIVE_BRANCH
    let [<Literal>] tooltip = "Inactive Preprocessor Branch"

    override x.Run(token, _, consumer) =
        let range = token.GetHighlightingRange()
        let highlighting = ReSharperSyntaxHighlighting(highlightingId, tooltip, range)
        consumer.AddHighlighting(highlighting)
