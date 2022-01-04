namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer([| typeof<IAppExpr> |],
                         HighlightingTypes = [| typeof<RedundantApplicationWarning> |])>]
type AppExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<IAppExpr>()

    override x.Run(appExpr, _, consumer) =
        let mutable arg = Unchecked.defaultof<_>

        match getPossibleFunctionAppName appExpr with
        | "id" when isPredefinedFunctionApp "id" appExpr &arg ->
            consumer.AddHighlighting(RedundantApplicationWarning(appExpr, arg))

        | "ignore" when isPredefinedFunctionApp "ignore" appExpr &arg ->
            let argType = arg.GetExpressionTypeFromFcs()
            let declType = argType.As<IDeclaredType>()
            if argType.IsVoid() || isNotNull declType && declType.GetClrName() = FSharpPredefinedType.unitTypeName then
                consumer.AddHighlighting(RedundantApplicationWarning(appExpr, arg))

        | _ -> ()
