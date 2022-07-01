namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer([| typeof<IAppExpr> |],
                         HighlightingTypes = [| typeof<RedundantApplicationWarning> |])>]
type AppExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<IAppExpr>()

    override x.Run(appExpr, _, consumer) =
        let (|Predefined|_|) name possibleName =
            let mutable arg = Unchecked.defaultof<_>
            if name = possibleName && isPredefinedFunctionApp name appExpr &arg then
                Some(arg)
            else
                None

        match getPossibleFunctionAppName appExpr with
        | Predefined "id" arg ->
            consumer.AddHighlighting(RedundantApplicationWarning(appExpr, arg))

        |  Predefined "ignore" arg ->
            let argType = arg.GetExpressionTypeFromFcs()
            let declType = argType.As<IDeclaredType>()
            if argType.IsVoid() || isNotNull declType && declType.GetClrName() = FSharpPredefinedType.unitTypeName then
                consumer.AddHighlighting(RedundantApplicationWarning(appExpr, arg))

        |  Predefined "sprintf" arg ->
            let arg = arg.IgnoreInnerParens()
            let literalExpr = arg.As<ILiteralExpr>()
            if isNotNull literalExpr && literalExpr.Type().IsString() || arg :? IInterpolatedStringExpr then
                consumer.AddHighlighting(RedundantApplicationWarning(appExpr, arg))

        | _ -> ()
