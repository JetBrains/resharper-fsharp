module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.RedundantQualifierAnalyzer

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
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

    let declaredElement =
        match reference.Resolve().DeclaredElement with
        | :? IConstructor as ctor -> ctor.GetContainingType() :> IDeclaredElement
        | declaredElement -> declaredElement

    if isNull declaredElement then false else

    // Don't make namespace usages unqualified, e.g. don't remove `System` leaving `Collections.Generic.List`.
    if declaredElement :? INamespace && qualifierName <> FSharpTokenType.GLOBAL.TokenRepresentation then false else

    // todo: try to check next qualified names in case we got into multiple-result resolve, i.e. for module?
    FSharpResolveUtil.resolvesToUnqualified declaredElement reference OpName


[<ElementProblemAnalyzer([| typeof<IReferenceExpr>; typeof<IReferenceName>; typeof<ITypeExtensionDeclaration> |],
                         HighlightingTypes = [| typeof<RedundantQualifierWarning> |])>]
type RedundantQualifierExpressionAnalyzer() =
    interface IElementProblemAnalyzer with
        member x.Run(refExpr, data, consumer) =
            let referenceOwner = refExpr :?> IFSharpReferenceOwner
            if isRedundant data referenceOwner then
                consumer.AddHighlighting(RedundantQualifierWarning(refExpr))
