module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.RedundantQualifierAnalyzer

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI

let [<Literal>] OpName = "RedundantQualifierAnalyzer"

// todo: module decls

let isRedundant (data: ElementProblemAnalyzerData) (referenceOwner: IFSharpReferenceOwner) =
    let reference = referenceOwner.Reference

    let qualifierExpr = reference.QualifierReference
    if isNull qualifierExpr then false else

    let qualifierName = qualifierExpr.GetName()
    if qualifierName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

    let opens = data.GetData(FSharpErrorsStage.openedModulesProvider).GetOpenedModuleNames
    let scopes = opens.GetValuesSafe(qualifierName)

    let inAnyScope =
        if scopes.Count = 0 then false else

        let offset = referenceOwner.GetTreeStartOffset()
        if scopes.Count = 1 then
            OpenScope.includesOffset offset scopes.[0]
        else
            scopes |> Seq.exists (OpenScope.includesOffset offset)

    if not inAnyScope then false else

//    if not (opens.GetValuesSafe(shortName) |> Seq.exists (endsWith qualifierExpr.QualifiedName)) then () else

    let referenceName = referenceOwner.As<IReferenceName>()
    if isNotNull referenceName && isInOpen referenceName then false else

    let fcsSymbol = reference.GetFSharpSymbol()
    if isNull fcsSymbol then false else

    let fcsSymbol: FSharpSymbol =
        match fcsSymbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when mfv.IsConstructor ->
            match mfv.DeclaringEntity with
            | Some(fcsEntity) -> fcsEntity :> _
            | _ -> Unchecked.defaultof<_>
        | _ -> fcsSymbol

    if isNull fcsSymbol then false else

    match fcsSymbol with
    | :? FSharpEntity as fcsEntity when
            fcsEntity.IsNamespace && qualifierName <> FSharpTokenType.GLOBAL.TokenRepresentation ->
        // Don't make namespace usages unqualified, e.g. don't remove `System` leaving `Collections.Generic.List`.
        false
    | _ ->

    // todo: try to check next qualified names in case we got into multiple-result resolve, i.e. for module?
    FSharpResolveUtil.resolvesToFcsSymbolUnqualified fcsSymbol reference OpName


[<ElementProblemAnalyzer([| typeof<IReferenceExpr>; typeof<IReferenceName>; typeof<ITypeExtensionDeclaration> |],
                         HighlightingTypes = [| typeof<RedundantQualifierWarning> |])>]
type RedundantQualifierExpressionAnalyzer() =
    interface IElementProblemAnalyzer with
        member x.Run(refExpr, data, consumer) =
            let referenceOwner = refExpr :?> IFSharpReferenceOwner
            if isRedundant data referenceOwner then
                consumer.AddHighlighting(RedundantQualifierWarning(refExpr))
