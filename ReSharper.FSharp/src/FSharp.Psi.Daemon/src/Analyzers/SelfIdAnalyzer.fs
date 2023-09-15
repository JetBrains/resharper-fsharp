namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer([| typeof<INamedSelfId> |], HighlightingTypes = [| typeof<UseWildSelfIdWarning> |])>]
type SelfIdAnalyzer() =
    inherit ElementProblemAnalyzer<INamedSelfId>()

    let hasUsages (expr: IFSharpExpression) =
        isNotNull expr &&

        let nameUsages = FSharpNamingService.getUsedNamesUsages [expr] EmptyList.Instance null false
        nameUsages.ContainsKey("__")

    override this.Run(selfId, data, consumer) =
        if selfId.SourceName <> "__" || not data.IsFSharp47Supported then () else

        let memberDeclaration = MemberDeclarationNavigator.GetBySelfId(selfId)
        if isNull memberDeclaration || hasUsages memberDeclaration.Expression then () else

        let accessorDecls = memberDeclaration.AccessorDeclarationsEnumerable
        if Seq.isEmpty accessorDecls || not (accessorDecls |> Seq.exists (fun decl -> hasUsages decl.Expression)) then
            consumer.AddHighlighting(UseWildSelfIdWarning(selfId))
