namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.TypeAnnotationsUtil
open JetBrains.Util.Logging

type private NodesRequiringHints =
    { TopLevelNodes: List<ITreeNode>; LocalNodes: List<ITreeNode> }

type private FSharpTypeHintSettings =
    { TopLevelMembers: PushToHintMode; LocalBindings: PushToHintMode } with

    static member Create(settingsStore: IContextBoundSettingsStore) =
        { TopLevelMembers = settingsStore.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowTypeHintsForTopLevelMembers)
          LocalBindings = settingsStore.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowTypeHintsForLocalBindings) }

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

type private PatternsHighlightingProcess(logger: ILogger, fsFile, settingsStore: IContextBoundSettingsStore,
                                           daemonProcess: IDaemonProcess, settings) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let createTypeHintHighlighting (fcsType: FSharpType) (displayContext: FSharpDisplayContext) range pushToHintMode =
        let displayContext = displayContext.WithShortTypeNames(true)
        TypeHintHighlighting(fcsType.Format(displayContext), range, pushToHintMode)

    let getReturnTypeHint (decl: IParameterOwnerMemberDeclaration) pushToHintMode =
        match decl with
        | :? IConstructorDeclaration
        | :? IAccessorDeclaration -> ValueNone
        | _ ->

        let equalsToken = decl.EqualsToken
        let patterns = decl.ParameterPatterns
        let range =
            match decl with
            | :? IBinding as binding when patterns.Count = 0 ->
                binding.HeadPattern.GetDocumentRange().EndOffsetRange()

            | :? IMemberDeclaration as memberDeclaration when patterns.Count = 0 ->
                memberDeclaration.NameIdentifier.GetDocumentRange().EndOffsetRange()

            | _ -> equalsToken.GetDocumentRange().StartOffsetRange()

        let symbolUse =
            match decl with
            | :? IBinding as binding ->
                let refPat = binding.HeadPattern.As<IReferencePat>()
                if isNull refPat then Unchecked.defaultof<_> else
                refPat.GetFcsSymbolUse()
            | :? IMemberDeclaration as memberDeclaration ->
                memberDeclaration.GetFcsSymbolUse()
            | _ -> Unchecked.defaultof<_>

        if isNull symbolUse then ValueNone else
        let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
        if isNull symbol then ValueNone else

        let displayContext = symbolUse.DisplayContext
        createTypeHintHighlighting symbol.ReturnParameter.Type displayContext range pushToHintMode |> ValueSome

    let rec getHintForPattern (pattern: IFSharpPattern) pushToHintMode =
        match pattern with
        | :? IParametersOwnerPat as pattern ->
            let asPat = AsPatNavigator.GetByLeftPattern(pattern.IgnoreParentParens())
            if isNull asPat then ValueNone else
            let reference = pattern.Reference
            if isNull reference then ValueNone else
            let symbol = reference.GetFcsSymbol().As<FSharpActivePatternCase>()
            if isNull symbol then ValueNone
            else getHintForPattern (asPat.RightPattern.IgnoreInnerParens()) pushToHintMode

        | :? IReferencePat as refPat ->
            let symbolUse = refPat.GetFcsSymbolUse()
            if isNull symbolUse then ValueNone else
            let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
            if isNull symbol then ValueNone else

            let fcsType = symbol.FullType
            let range = pattern.GetNavigationRange().EndOffsetRange()
            let displayContext = symbolUse.DisplayContext

            createTypeHintHighlighting fcsType displayContext range pushToHintMode |> ValueSome

        | _ -> ValueNone

    let rec getHighlighting (node: ITreeNode) pushToHintMode =
        match node with
        | :? IFSharpPattern as pattern ->
            getHintForPattern pattern pushToHintMode

        | :? IParameterOwnerMemberDeclaration as decl ->
            getReturnTypeHint decl pushToHintMode

        | :? IAutoPropertyDeclaration as decl ->
            let symbolUse = decl.GetFcsSymbolUse()
            if isNull symbolUse then ValueNone else
            let symbol = symbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
            if isNull symbol then ValueNone else

            let fcsType = symbol.ReturnParameter.Type
            let range = decl.NameIdentifier.GetNavigationRange().EndOffsetRange()
            let displayContext = symbolUse.DisplayContext.WithShortTypeNames(true)

            createTypeHintHighlighting fcsType displayContext range pushToHintMode |> ValueSome

        | _ -> ValueNone

    let adornNodes logKey (topLevelNodes : ITreeNode array) (localNodes : ITreeNode array) =
        use _swc = logger.StopwatchCookie($"Adorning %s{logKey} nodes", $"topLevelNodesCount=%d{topLevelNodes.Length} localNodesCount=%d{localNodes.Length} sourceFile=%s{daemonProcess.SourceFile.Name}")
        let highlightingConsumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settingsStore)

        let adornNodes nodes pushToHintMode =
            for node in nodes do
                if daemonProcess.InterruptFlag then raise <| OperationCanceledException()

                match getHighlighting node pushToHintMode with
                | ValueSome highlighting -> highlightingConsumer.AddHighlighting(highlighting)
                | _ -> ()

        adornNodes topLevelNodes settings.TopLevelMembers
        adornNodes localNodes settings.LocalBindings

        highlightingConsumer.CollectHighlightings()

    override x.Execute(committer) =
        let consumer = { TopLevelNodes = List(); LocalNodes = List() }
        fsFile.Accept(MembersVisitor(settings), consumer)

        let topLevelNodes = consumer.TopLevelNodes |> Array.ofSeq
        let localNodes = consumer.LocalNodes |> Array.ofSeq

        // Visible range may be larger than document range by 1 char
        // Intersect them to ensure commit doesn't throw
        let documentRange = daemonProcess.Document.GetDocumentRange()
        let visibleRange = daemonProcess.VisibleRange.Intersect(&documentRange)

        let partition (nodes: ITreeNode array) visibleRange =
            nodes
            |> Array.partition _.GetNavigationRange().IntersectsOrContacts(&visibleRange)

        let remainingHighlightings =
            if visibleRange.IsValid() then
                // Partition the expressions to adorn by whether they're visible in the viewport or not
                let topLevelVisible, topLevelNotVisible = partition topLevelNodes visibleRange
                let localNodesVisible, localNodesNotVisible = partition localNodes visibleRange

                // Adorn visible expressions first
                let visibleHighlightings = adornNodes "visible" topLevelVisible localNodesVisible
                committer.Invoke(DaemonStageResult(visibleHighlightings, visibleRange))

                // Finally adorn expressions that aren't visible in the viewport
                adornNodes "not visible" topLevelNotVisible localNodesNotVisible
            else
                adornNodes "all" topLevelNodes localNodes

        committer.Invoke(DaemonStageResult remainingHighlightings)

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type PatternTypeHintsStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settingsStore, daemonProcess, _) =
        let settings = FSharpTypeHintSettings.Create(settingsStore)
        if settings.IsDisabled then null
        else PatternsHighlightingProcess(logger, fsFile, settingsStore, daemonProcess, settings)
