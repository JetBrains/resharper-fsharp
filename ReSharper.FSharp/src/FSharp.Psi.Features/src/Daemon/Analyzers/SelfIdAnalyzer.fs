namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

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
        
        let nameUsages = FSharpNamingService.getUsedNamesUsages expr EmptyList.Instance null false
        nameUsages.ContainsKey("__")

    override this.Run(selfId, data, consumer) =
        if selfId.SourceName <> "__" || not data.IsFSharp47Supported then () else

        let memberDeclaration = MemberDeclarationNavigator.GetBySelfId(selfId)
        if isNull memberDeclaration then () else

        if (hasUsages memberDeclaration.Expression) then () else

        let accessorDeclarations = memberDeclaration.AccessorDeclarations
        if not (Seq.isEmpty accessorDeclarations) &&
                accessorDeclarations |> Seq.exists (fun decl -> hasUsages decl.BodyExpression.Expression) then () else

        consumer.AddHighlighting(UseWildSelfIdWarning(selfId))
