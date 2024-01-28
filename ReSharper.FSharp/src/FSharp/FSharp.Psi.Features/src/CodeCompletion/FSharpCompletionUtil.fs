module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil

open JetBrains.Application.Threading
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.TextControl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

let (|BasicCompletion|SmartCompletion|ImportCompletion|) completionType =
    if completionType == CodeCompletionType.BasicCompletion then BasicCompletion else
    if completionType == CodeCompletionType.SmartCompletion then SmartCompletion else
    if completionType == CodeCompletionType.ImportCompletion then ImportCompletion else
    failwithf "Unexpected completion type %O" completionType


type ISolution with
    member x.CompletionSessionManager =
        x.GetComponent<ICodeCompletionSessionManager>()


type ITextControl with
    member x.RescheduleCompletion(solution: ISolution) =
        solution.Locks.QueueReadLockOrRunSync(solution.GetSolutionLifetimes().MaximumLifetime, "Next code completion", fun _ ->
            solution.CompletionSessionManager
                .ExecuteAutomaticCompletionAsync(x, FSharpLanguage.Instance, AutopopupType.SoftAutopopup))


// A tribute to IntellijIdeaRulezzz:
// intellij-community/blob/master/platform/core-api/src/com/intellij/codeInsight/completion/CompletionUtilCore.java
let [<Literal>] DummyIdentifier = "ReSharperFSharpRulezzz"


let inline markRelevance (lookupItem: ILookupItem) (relevance: 'T) =
    lookupItem.Placement.Relevance <- lookupItem.Placement.Relevance ||| uint64 relevance

type ILookupItem with
    member this.WithRelevance(relevance: uint64) =
        this.Placement.Relevance <- this.Placement.Relevance ||| relevance
        this

    member this.WithRelevance(relevance: CLRLookupItemRelevance) =
        this.WithRelevance(uint64 relevance)

let getParametersOwnerPatFromReference (reference: IReference) : IParametersOwnerPat =
    let reference = reference.As<FSharpSymbolReference>()
    if isNull reference then null else

    let exprRefName = reference.GetElement().As<IExpressionReferenceName>()
    if isNull exprRefName || exprRefName.IsQualified then null else

    let refPat = ReferencePatNavigator.GetByReferenceName(exprRefName)
    let parentPat: IFSharpPattern =
        // Try finding a reference pattern in case there is no existing named access.
        if isNotNull refPat then
            ParenPatNavigator.GetByPattern(refPat)
        else
            // Otherwise, try and find an incomplete field pat
            let fieldPat = FieldPatNavigator.GetByReferenceName(exprRefName)
            NamedUnionCaseFieldsPatNavigator.GetByFieldPattern(fieldPat)

    ParametersOwnerPatNavigator.GetByParameter(parentPat)
