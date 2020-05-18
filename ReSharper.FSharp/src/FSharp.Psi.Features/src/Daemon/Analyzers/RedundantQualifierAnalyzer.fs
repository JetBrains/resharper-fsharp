module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers.RedundantQualifierAnalyzer

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Impl.Reflection2

let [<Literal>] OpName = "RedundantQualifierAnalyzer"

// todo: module decls

let isRedundant (data: ElementProblemAnalyzerData) (referenceOwner: IFSharpReferenceOwner) =
    let reference = referenceOwner.Reference

    let qualifierExpr = reference.QualifierReference
    if isNull qualifierExpr then false else

    let qualifierName = qualifierExpr.GetName()
    if qualifierName = SharedImplUtil.MISSING_DECLARATION_NAME then false else

    let opens = data.GetData(FSharpErrorsStage.openedModulesProvider).GetOpenedModuleNames
    if not (opens.Contains(qualifierName)) then false else
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
    let shortName = reference.GetName()
    match referenceOwner.CheckerService.ResolveNameAtLocation(referenceOwner, [shortName], OpName) with
    | None -> false
    | Some symbolUse ->

    let unqualifiedElement = symbolUse.Symbol.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner)
    if declaredElement.Equals(unqualifiedElement) then true else

    // Workaround for case where unqualified resolve may return module with implicit suffix instead of type.
    let compiledTypeElement = unqualifiedElement.As<CompiledTypeElement>()
    if isNull compiledTypeElement then false else

    if not (unqualifiedElement.ShortName.HasModuleSuffix() && not (shortName.HasModuleSuffix())) then false else
    if not (isCompiledModule compiledTypeElement) then false else

    let typeElement = FSharpImplUtil.TryGetAssociatedType(compiledTypeElement, shortName)
    declaredElement.Equals(typeElement)


[<ElementProblemAnalyzer([| typeof<IReferenceExpr>; typeof<IReferenceName>; typeof<ITypeExtensionDeclaration> |],
                         HighlightingTypes = [| typeof<RedundantQualifierWarning> |])>]
type RedundantQualifierExpressionAnalyzer() =
    interface IElementProblemAnalyzer with
        member x.Run(refExpr, data, consumer) =
            let referenceOwner = refExpr :?> IFSharpReferenceOwner
            if isRedundant data referenceOwner then
                consumer.AddHighlighting(RedundantQualifierWarning(refExpr))
