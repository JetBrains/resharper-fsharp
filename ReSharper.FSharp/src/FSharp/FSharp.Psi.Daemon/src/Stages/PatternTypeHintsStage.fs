namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Highlightings.FSharpTypeHintsBulbActionsProvider
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Utils.VisibleRangeContainer
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil
open JetBrains.TextControl.DocumentMarkup.Adornments.IntraTextAdornments

type private NodesRequiringHints =
    { TopLevelNodes: VisibilityConsumer<ITreeNode>; LocalNodes: VisibilityConsumer<ITreeNode> } with

    member x.HasVisibleItems =
        x.TopLevelNodes.HasVisibleItems ||
        x.LocalNodes.HasVisibleItems

type private FSharpTypeHintSettings =
    { TopLevelMembers: PushToHintMode; LocalBindings: PushToHintMode } with

    static member Create(settingsStore: IContextBoundSettingsStore) =
        { TopLevelMembers = settingsStore.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowTypeHintsForTopLevelMembers)
                                         .EnsureInlayHintsDefault(settingsStore)
          LocalBindings = settingsStore.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowTypeHintsForLocalBindings)
                                       .EnsureInlayHintsDefault(settingsStore) }

    member x.IsDisabled =
        x.TopLevelMembers = PushToHintMode.Never &&
        x.LocalBindings = PushToHintMode.Never


type private MembersVisitor(settings) =
    inherit TreeNodeVisitor<NodesRequiringHints>()

    let isTopLevelMember (node: ITreeNode) =
        match node with
        | :? ITopBinding
        | :? IMemberDeclaration
        | :? IConstructorDeclaration -> true
        | _ -> false

    override x.VisitNode(node, context) =
        if settings.LocalBindings = PushToHintMode.Never && isTopLevelMember node then () else

        for child in node.Children() do
            if settings.TopLevelMembers = PushToHintMode.Never &&
               isTopLevelMember child then x.VisitNode(child, context) else

            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    override x.VisitLambdaExpr(lambda, context) =
        let result = collectTypeHintAnchorsForLambda lambda
        context.LocalNodes.AddRange(result)

        x.VisitNode(lambda, context)

    override x.VisitLocalBinding(binding, context) =
        let result = collectTypeHintAnchorsForBinding binding
        context.LocalNodes.AddRange(result)

        x.VisitNode(binding, context)

    override x.VisitTopBinding(binding, context) =
        let result = collectTypeHintAnchorsForBinding binding
        context.TopLevelNodes.AddRange(result)

        x.VisitNode(binding, context)

    override x.VisitMemberDeclaration(memberDecl, context) =
        let result = collectTypeHintsAnchorsForMember memberDecl
        context.TopLevelNodes.AddRange(result)

        x.VisitNode(memberDecl, context)

    override x.VisitPrimaryConstructorDeclaration(constructor, context) =
        let result = collectTypeHintAnchorsForConstructor constructor
        context.TopLevelNodes.AddRange(result)

        x.VisitNode(constructor, context)

    override x.VisitSecondaryConstructorDeclaration(constructor, context) =
        let result = collectTypeHintAnchorsForConstructor constructor
        context.TopLevelNodes.AddRange(result)

        x.VisitNode(constructor, context)

    override x.VisitForEachExpr(forEachExpr, context) =
        let result = collectTypeHintAnchorsForEachExpr forEachExpr
        context.LocalNodes.AddRange(result)

        x.VisitNode(forEachExpr, context)

type private PatternsHighlightingProcess(fsFile, settingsStore: IContextBoundSettingsStore, daemonProcess: IDaemonProcess, settings) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)
    static let defaultDisplayContext = FSharpDisplayContext.Empty.WithShortTypeNames(true)

    let createTypeHintHighlighting
        (fcsType: FSharpType)
        (displayContext: FSharpDisplayContext)
        range
        pushToHintMode
        actionsProvider
        isFromReturnType =
        let suffix = if isFromReturnType then " " else ""
        TypeHintHighlighting(fcsType.Format(displayContext), range, pushToHintMode, suffix, actionsProvider)

    let getReturnTypeHint (decl: IParameterOwnerMemberDeclaration) pushToHintMode actionsProvider =
        match decl with
        | :? IConstructorDeclaration
        | :? IAccessorDeclaration -> ValueNone
        | _ ->

        let equalsToken = decl.EqualsToken
        let range =
            match decl with
            | :? IBinding as binding when not binding.HasParameters ->
                binding.HeadPattern.GetDocumentRange().EndOffsetRange()

            | :? IMemberDeclaration as memberDeclaration when memberDeclaration.ParameterPatternsEnumerable.IsEmpty() ->
                memberDeclaration.NameIdentifier.GetDocumentRange().EndOffsetRange()

            | _ -> equalsToken.GetDocumentRange().StartOffsetRange()

        let symbolUse =
            match decl with
            | :? IBinding as binding ->
                match binding.HeadPattern with
                | :? IReferencePat as refPat -> refPat.GetFcsSymbolUse()
                | _ -> Unchecked.defaultof<_>
            | :? IMemberDeclaration as memberDeclaration -> memberDeclaration.GetFcsSymbolUse()
            | _ -> Unchecked.defaultof<_>

        if isNull symbolUse then ValueNone else

        let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
        if isNull symbol then ValueNone else

        createTypeHintHighlighting symbol.ReturnParameter.Type defaultDisplayContext range pushToHintMode actionsProvider true
        |> ValueSome

    let rec getHintForPattern (pattern: IFSharpPattern) pushToHintMode actionsProvider =
        match pattern with
        | :? IParametersOwnerPat as pattern ->
            let asPat = AsPatNavigator.GetByLeftPattern(pattern.IgnoreParentParens())
            if isNull asPat then ValueNone else

            let reference = pattern.Reference
            if isNull reference then ValueNone else

            match reference.GetFcsSymbol() with
            | :? FSharpActivePatternCase ->
                getHintForPattern (asPat.RightPattern.IgnoreInnerParens()) pushToHintMode actionsProvider
            | _ -> ValueNone

        | :? IReferencePat as refPat ->
            let symbolUse = refPat.GetFcsSymbolUse()
            if isNull symbolUse then ValueNone else

            let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
            if isNull symbol then ValueNone else

            let fcsType = symbol.FullType
            let range = pattern.GetNavigationRange().EndOffsetRange()

            let isOptional = isNotNull (OptionalValPatNavigator.GetByPattern(refPat))
            let fcsType =
                if isOptional then
                    let genericParameters = fcsType.GenericArguments
                    if genericParameters.Count = 0 then fcsType else fcsType.GenericArguments[0]
                else fcsType

            createTypeHintHighlighting fcsType defaultDisplayContext range pushToHintMode actionsProvider false
            |> ValueSome

        | _ -> ValueNone

    let rec getHighlighting (node: ITreeNode) pushToHintMode actionsProvider =
        match node with
        | :? IFSharpPattern as pattern ->
            getHintForPattern pattern pushToHintMode actionsProvider

        | :? IParameterOwnerMemberDeclaration as decl ->
            getReturnTypeHint decl pushToHintMode actionsProvider

        | _ -> ValueNone

    let adornNodes (topLevelNodes : ITreeNode ICollection) (localNodes : ITreeNode ICollection) =
        let highlightingConsumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settingsStore)

        let inline adornNodes nodes pushToHintMode actionsProvider =
            for node in nodes do
                if daemonProcess.InterruptFlag then raise <| OperationCanceledException()

                match getHighlighting node pushToHintMode actionsProvider with
                | ValueSome highlighting -> highlightingConsumer.AddHighlighting(highlighting)
                | _ -> ()

        adornNodes topLevelNodes settings.TopLevelMembers FSharpTopLevelMembersTypeHintBulbActionsProvider.Instance
        adornNodes localNodes settings.LocalBindings FSharpLocalBindingTypeHintBulbActionsProvider.Instance

        highlightingConsumer.CollectHighlightings()

    override x.Execute(committer) =
        // Visible range may be larger than document range by 1 char
        // Intersect them to ensure commit doesn't throw
        let documentRange = daemonProcess.Document.GetDocumentRange()
        let visibleRange = daemonProcess.VisibleRange.Intersect(&documentRange)
        let consumer = { TopLevelNodes = VisibilityConsumer(visibleRange, _.GetNavigationRange())
                         LocalNodes = VisibilityConsumer(visibleRange, _.GetNavigationRange()) }
        fsFile.Accept(MembersVisitor(settings), consumer)

        let topLevelNodes = consumer.TopLevelNodes
        let localNodes = consumer.LocalNodes

        // Partition the expressions to adorn by whether they're visible in the viewport or not
        let remainingHighlightings =
            if consumer.HasVisibleItems then
                // Adorn visible expressions first
                let visibleHighlightings = adornNodes topLevelNodes.VisibleItems localNodes.VisibleItems
                committer.Invoke(DaemonStageResult(visibleHighlightings, visibleRange))

            // Finally adorn expressions that aren't visible in the viewport
            adornNodes topLevelNodes.NonVisibleItems localNodes.NonVisibleItems

        committer.Invoke(DaemonStageResult remainingHighlightings)

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type PatternTypeHintsStage() =
    inherit FSharpDaemonStageBase(true, false)

    override x.CreateStageProcess(fsFile, settingsStore, daemonProcess, _) =
        let isEnabled = settingsStore.GetValue(fun (key: GeneralInlayHintsOptions) -> key.EnableInlayHints)
        if not isEnabled then null else

        let settings = FSharpTypeHintSettings.Create(settingsStore)
        if settings.IsDisabled then null
        else PatternsHighlightingProcess(fsFile, settingsStore, daemonProcess, settings)
