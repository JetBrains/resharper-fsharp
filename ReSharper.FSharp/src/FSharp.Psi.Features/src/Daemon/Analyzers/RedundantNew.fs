namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi

[<ElementProblemAnalyzer(typeof<INewExpr>,
                         HighlightingTypes = [| typeof<RedundantNewWarning> |])>]
type RedundantNewAnalyzer() =
    inherit ElementProblemAnalyzer<INewExpr>()

    override x.Run(newExpr, data, consumer) =
        let typeName = newExpr.TypeName
        if isNull typeName then () else

        let resolveResult = typeName.Reference.Resolve()
        match resolveResult.DeclaredElement.As<ITypeElement>() with
        | null -> ()
        | typeElement ->

        let predefinedType = newExpr.GetPredefinedType()
        if typeElement.IsDescendantOf(predefinedType.IDisposable.GetTypeElement()) then () else
        if isNull newExpr.NewKeyword then () else

        match newExpr.CheckerService.ResolveNameAtLocation(typeName, typeName.Names, "RedundantNewAnalyzer") with
        | None -> ()
        | Some symbolUse ->

        let fcsEntity = symbolUse.Symbol.As<FSharpEntity>()
        if isNull fcsEntity || isNull typeName.TypeArgumentList && fcsEntity.GenericParameters.Count <> 0 then () else

        if not (FSharpResolveUtil.mayShadowPartially newExpr data fcsEntity) then
            consumer.AddHighlighting(RedundantNewWarning(newExpr))
