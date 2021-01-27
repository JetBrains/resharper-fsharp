namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.InlayHints
open JetBrains.Application.Settings
open JetBrains.Application.Settings.WellKnownRootKeys
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Feature.Services.TypeNameHints
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

[<SettingsKey(typeof<InlayHintsSettings>, "ReSharper F# Type Name Hints settings")>]
type FSharpTypeNameHintsOptions() =
    inherit TypeNameHintsOptionsBase()

    [<SettingsEntry(InlayHintsMode.PushToShowHints, "Visibility mode of type name hints for lambda expression parameters"); DefaultValue>]
    val mutable ShowTypeNameHintsForLambdaExpressionParameters: InlayHintsMode

    [<SettingsEntry(InlayHintsMode.PushToShowHints, "Visibility mode of type name hints for pattern matching expressions"); DefaultValue>]
    val mutable ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions: InlayHintsMode


type FSharpTypeNameHintHighlightingContext(settingsStore: IContextBoundSettingsStore) =
    inherit TypeNameHintHighlightingContext<FSharpTypeNameHintsOptions>(settingsStore)

    member val ShowTypeNameHintsForLambdaExpressionParameters =
        settingsStore.GetValue(fun (options: FSharpTypeNameHintsOptions) ->
            options.ShowTypeNameHintsForLambdaExpressionParameters)

    member val ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions =
        settingsStore.GetValue(fun (options: FSharpTypeNameHintsOptions) ->
            options.ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions)


type LocalReferencePatternVisitor
        (fsFile: IFSharpFile, context: FSharpTypeNameHintHighlightingContext, namingPolicyProvider, nameParser) =
    inherit TreeNodeVisitor<IHighlightingConsumer * InlayHintsMode>()

    let isTypeEvidentFromVariableNamePrefix (fcsType: FSharpType) (variableNameParts: string[]) =
        variableNameParts.Length > 0 &&

        match variableNameParts.[0] with
        | IgnoreCase "has" | IgnoreCase "is" -> fcsType.MapType(EmptyList.InstanceList, fsFile.GetPsiModule()).IsBool()
        | _ -> false

    let isEvidentFromVariableName (fcsType: FSharpType) variableName =
        if not context.HideTypeNameHintsWhenTypeNameIsEvidentFromVariableName then false else
        if not fcsType.HasTypeDefinition then false else

        let nameParts = NamesHelper.GetParts(nameParser, namingPolicyProvider, variableName)

        let fcsEntity = fcsType.TypeDefinition
        if isTypeEvidentFromVariableNamePrefix fcsType nameParts then true else

        match fcsEntity.LogicalName with
        | IgnoreCase variableName -> true
        | typeName -> NamesHelper.IsLike(nameParts, NamesHelper.GetParts(nameParser, namingPolicyProvider, typeName))

    let isTypeOfPatternEvident (pattern: IFSharpPattern) =

        // v-- not evident
        // x::y::z
        //    ^--  evident
        let listConsPat = ListConsPatNavigator.GetByHeadPattern pattern
        if isNotNull listConsPat then
            isNotNull (ListConsPatNavigator.GetByTailPattern(listConsPat))

        // x::y::z
        //       ^--  evident
        elif isNotNull (ListConsPatNavigator.GetByTailPattern(pattern)) then
            true else

        //  v-- not evident
        // [x; y; z]
        //     ^--^-- evident
        let listOrListPat = ArrayOrListPatNavigator.GetByPattern(pattern)
        if isNotNull listOrListPat then
            match Seq.tryHead listOrListPat.PatternsEnumerable with
            | None -> false
            | Some head -> head <> pattern
        else
            false

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, (context))
            | _ -> ()

    override x.VisitLocalReferencePat(localRefPat, (consumer, inlayHintsMode)) =
        let pat = localRefPat.IgnoreParentParens()
        if isNotNull (TypedPatNavigator.GetByPattern(pat)) then () else

        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && isNotNull binding.ReturnTypeInfo then () else

        let variableName = localRefPat.SourceName
        if variableName = SharedImplUtil.MISSING_DECLARATION_NAME then () else

        if isTypeOfPatternEvident pat then () else

        let symbolUse = localRefPat.GetFSharpSymbolUse()
        if isNull symbolUse then () else

        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when not (isEvidentFromVariableName mfv.FullType variableName) ->
            let typeNameStr = symbolUse.DisplayContext.WithShortTypeNames(true) |> mfv.FullType.Format
            let range = localRefPat.GetNavigationRange().EndOffsetRange()

            // todo: TypeNameHintHighlighting can be used when RIDER-39605 is resolved
            consumer.AddHighlighting(TypeHintHighlighting(typeNameStr, range, inlayHintsMode))
        | _ -> ()

type InferredTypeHintHighlightingProcess
        (fsFile, settings: IContextBoundSettingsStore, highlightingContext: FSharpTypeNameHintHighlightingContext,
         namingManager: NamingManager, nameParser: NameParser, daemonProcess, isEnabled) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let namingPolicyProvider = namingManager.Policy.GetPolicyProvider(fsFile.Language, fsFile.GetSourceFile())
    let hideHintsForEvidentTypes = highlightingContext.HideTypeNameHintsForImplicitlyTypedVariablesWhenTypeIsEvident

    let visitor = LocalReferencePatternVisitor(fsFile, highlightingContext, namingPolicyProvider, nameParser)

    let visitLetBindings (letBindings: ILetBindings) consumer =
        if highlightingContext.ShowTypeNameHintsForImplicitlyTypedVariables = InlayHintsMode.Never then () else

        for binding in letBindings.Bindings do
            if hideHintsForEvidentTypes && isTypeEvident binding.Expression then () else

            match binding.HeadPattern with
            | null -> ()
            | headPat -> headPat.Accept(visitor, consumer)

    override x.Execute(committer) =
        if not isEnabled then
            committer.Invoke(DaemonStageResult(EmptyArray.Instance, 0))
            committer.Invoke(DaemonStageResult(EmptyArray.Instance, 1))
        else
            let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
            fsFile.ProcessThisAndDescendants(Processor(x, consumer))
            committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitLetBindingsDeclaration(moduleDecl, consumer) =
        visitLetBindings moduleDecl (consumer, InlayHintsMode.Default)

    override x.VisitLetOrUseExpr(letOrUseExpr, consumer) =
        visitLetBindings letOrUseExpr (consumer, InlayHintsMode.Default)

    override x.VisitParametersPatternDeclaration(paramDecl, consumer) =
        let inlayHintsMode = highlightingContext.ShowTypeNameHintsForImplicitlyTypedVariables
        if inlayHintsMode <> InlayHintsMode.Never then
            match paramDecl.Pattern with
            | null -> ()
            | pattern -> pattern.Accept(visitor, (consumer, inlayHintsMode))

    override x.VisitMatchClause(matchClause, consumer) =
        let inlayHintsMode = highlightingContext.ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions
        if inlayHintsMode <> InlayHintsMode.Never then
            match matchClause.Pattern with
            | null -> ()
            | pattern -> pattern.Accept(visitor, (consumer, inlayHintsMode))

    override x.VisitLambdaExpr(lambdaExpr, consumer) =
        let inlayHintsMode = highlightingContext.ShowTypeNameHintsForLambdaExpressionParameters
        if inlayHintsMode <> InlayHintsMode.Never then
            for pattern in lambdaExpr.Patterns do
                if isNotNull pattern then
                    pattern.Accept(visitor, (consumer, inlayHintsMode))


[<DaemonStage(StagesBefore = [| typeof<FSharpErrorsStage> |])>]
type InferredTypeHintStage(namingManager: NamingManager, nameParser: NameParser) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        let context = FSharpTypeNameHintHighlightingContext(settings)

        let isEnabled =
            context.ShowTypeNameHintsForImplicitlyTypedVariables <> InlayHintsMode.Never ||
            context.ShowTypeNameHintsForLambdaExpressionParameters <> InlayHintsMode.Never ||
            context.ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions <> InlayHintsMode.Never

        InferredTypeHintHighlightingProcess(fsFile, settings, context, namingManager, nameParser, daemonProcess, isEnabled) :> _
