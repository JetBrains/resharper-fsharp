module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.RedundantQualifierAnalyzer

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI

let [<Literal>] OpName = "RedundantQualifierAnalyzer"

// todo: module decls

// todo: add better check for `global`
let isModuleOrNamespace (fcsReference: FSharpSymbolReference) =
    let entity = fcsReference.GetFcsSymbol().As<FSharpEntity>()
    if isNotNull entity then
        entity.IsFSharpModule || entity.IsNamespace
    else
        not fcsReference.IsQualified &&

        let referenceOwner = fcsReference.GetElement()
        isNotNull referenceOwner && getTokenType referenceOwner.FSharpIdentifier == FSharpTokenType.GLOBAL

let isRedundant (data: ElementProblemAnalyzerData) (referenceOwner: IFSharpReferenceOwner) =
    let reference = referenceOwner.Reference

    let qualifierExprReference = reference.QualifierReference
    if isNull qualifierExprReference then false else

    let qualifierName = qualifierExprReference.GetName()
    if qualifierName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

    if not (isModuleOrNamespace qualifierExprReference) then false else

    let opens = data.GetData(openedModulesProvider).OpenedModuleScopes
    let scopes = opens.GetValuesSafe(qualifierName)

    if not (OpenScope.inAnyScope referenceOwner scopes) then false else

//    if not (opens.GetValuesSafe(shortName) |> Seq.exists (endsWith qualifierExpr.QualifiedName)) then () else

    let referenceName = referenceOwner.As<IReferenceName>()
    if isNotNull referenceName && isInOpen referenceName then false else

    let fcsSymbol = reference.GetFcsSymbol()
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
    FSharpResolveUtil.resolvesToFcsSymbol fcsSymbol reference false false OpName &&
    not (FSharpResolveUtil.mayShadowPartially referenceOwner data fcsSymbol)

[<ElementProblemAnalyzer([| typeof<IReferenceExpr>; typeof<IReferenceName>; typeof<ITypeExtensionDeclaration> |],
                         HighlightingTypes = [| typeof<RedundantQualifierWarning> |])>]
type RedundantQualifierExpressionAnalyzer() =
    interface IElementProblemAnalyzer with
        member x.Run(refExpr, data, consumer) =
            let referenceOwner = refExpr :?> IFSharpReferenceOwner
            if isRedundant data referenceOwner then
                consumer.AddHighlighting(RedundantQualifierWarning(refExpr))
