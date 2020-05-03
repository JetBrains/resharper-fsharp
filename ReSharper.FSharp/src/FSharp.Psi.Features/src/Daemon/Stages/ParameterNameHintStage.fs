namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Feature.Services.ParameterNameHints
open JetBrains.ReSharper.Feature.Services.ParameterNameHints.BlackList
open JetBrains.ReSharper.Feature.Services.ParameterNameHints.ManagedLanguage
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

[<Language(typeof<FSharpLanguage>)>]
type FSharpParameterNameHintsOptionStore(optionsStore: ManagedLanguageParameterNameHintsOptionsStore) =
    interface ILanguageSpecificParameterNameHintsOptionsStore with
        // todo: should we have our own cache key?
        override x.GetBlackListCacheKey() = optionsStore.GetBlackListCacheKey()
        override x.GetCategory() = optionsStore.GetCategory()
        override x.GetLanguageSpecificSettingsEntries(context) = optionsStore.GetLanguageSpecificSettingsEntries(context)
        override x.GetGlobalSettingsEntries(context) = optionsStore.GetGlobalSettingsEntries(context)
        override x.GetBlackList(settingsStore) = optionsStore.GetBlackList(settingsStore)
        override x.PersistBlackList(settingsStore, value) = optionsStore.PersistBlackList(settingsStore, value)

[<Language(typeof<FSharpLanguage>)>]
type ParameterNameHintsHighlightingStrategy() =
    inherit ManagedLanguageParameterNameHintsHighlightingStrategy()

    override x.GetShortDescription(parameter, argument) = RichText parameter.ShortName

    override x.IsShouldBeIgnored(argument: IArgument) =
        match argument with
        | :? IBinaryAppExpr as binaryAppExpr ->
            // todo: do we need to resolve the left arg as an IParameter to be sure?
            binaryAppExpr.Operator.Reference.GetName() = "="
        | _ ->
            false

    override x.IsShouldBeIgnored(expression: IExpression) = false
    override x.IsLast(parameter) = parameter.IsParameterArray || parameter.IsVarArg
    override x.CanBeConsideredAsLiteral(argument, expression) =
        match expression with
        | :? ILiteralExpr -> true
        | _ -> isNull argument.MatchingParameter

    override x.IsLambdaExpression(context, expression) =
        context.ShowForLambdaExpressions && x.IsLambdaExpression expression
    override x.IsLambdaExpression(expression) =
        match expression with
        | :? ILambdaExpr -> true
        | _ -> false

    override x.IsConstOrEnumMemberReference(context, parameter, expression) =
        if not context.ShowForConstantsAndEnumMembers then false else

        let refExpr = expression.As<IReferenceExpr>()
        if isNull refExpr then false else

        let field = refExpr.Reference.Resolve().DeclaredElement.As<IField>()
        if isNull field then false else

        field.IsStatic && field.IsReadonly || field.IsConstant || field.IsEnumMember

    override x.IsUnclearCreationExpression(context, expression) = false
    override x.IsMethodInvocation(context, expression) =
        match expression with
        | :? IAppExpr -> true
        | _ -> false

    override x.IsIntentionOfArgumentClearFromReferencedElement(context, parameter, uniqueParameterParts, expression) =
        if not context.HideIfIntentionOfArgumentIsClearFromUsage then false else

        match expression with
        | :? IReferenceExpr as refExpr ->
            let namingRule = x.GetTypeNamingRule(context, refExpr.GetSourceFile())
            NamesHelper.IsLike(context.NameParser, context.NamingPolicyProvider, namingRule, parameter, uniqueParameterParts, refExpr.ShortName)
        | _ -> false

    override x.IsDefaultExpression(expression) = false

    override x.GetExpression(expression, getThroughInvocation, getThroughCast) =
        let expr = expression.As<ISynExpr>()
        if isNull expr then null else

        match expr.IgnoreInnerParens() with
        | :? ICastExpr as castExpr when getThroughCast ->
            x.GetExpression(castExpr.Expression, getThroughInvocation, getThroughCast)
        | :? IAppExpr as appExpr ->
            x.GetExpression(appExpr.FunctionExpression, getThroughInvocation, getThroughCast)
        | expr ->
            // todo: unary ops
            expr :> _

    override x.IsIntentionOfArgumentClearFromInvocation(context, parameter, uniqueParameterParts, expression) =
        if not context.HideIfIntentionOfArgumentIsClearFromUsage then false else

        // todo: nameof
        // todo: invocation e.g. foo.|GetBar|() ?

        match expression with
        | :? IReferenceExpr as refExpr when FSharpExpressionUtil.isPredefinedFunctionRef "typeof" refExpr ->
            let namingRule = x.GetTypeNamingRule(context, refExpr.GetSourceFile())
            NamesHelper.IsLike(context.NameParser, context.NamingPolicyProvider, namingRule, parameter, uniqueParameterParts, "type")
        | _ -> false

    override x.GetTypeNamingRule(context, sourceFile) =
        context.NamingRulesCache.GetNamingRule(NamedElementKinds.TypesAndNamespaces, FSharpLanguage.Instance, sourceFile)

    override x.GetParametersNamingRule(context, sourceFile) =
        context.NamingRulesCache.GetNamingRule(NamedElementKinds.Parameters, FSharpLanguage.Instance, sourceFile)

    override x.GetParameterOwnerNamingRule(context, sourceFile) =
        context.NamingRulesCache.GetNamingRule(NamedElementKinds.Method, FSharpLanguage.Instance, sourceFile)

    override x.IsIntentionOfArgumentClearFromExplicitConversion(context, parameter, uniqueParameterParts, expression) =
        if not context.HideIfIntentionOfArgumentIsClearFromUsage then false else

        match expression with
        | :? ICastExpr as castExpr when isNotNull castExpr.TypeUsage ->
            // todo: check type cast expr
            false
        | _ -> false

type ParameterNameHintHighlightingProcess
        (fsFile, settings, daemonProcess, namingManager: NamingManager, nameParser: NameParser,
         blackListMatcher: IParameterNameHintsBlackListMatcher,
         strategy: ManagedLanguageParameterNameHintsHighlightingStrategy,
         parameterNameHintsHighlightingProvider: IManagedLanguageParameterNameHintsHighlightingProvider,
         customProviders: ICustomManagedLanguageParameterNameHintsHighlightingProvider seq) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let policyProvider = fsFile.GetPsiServices().Naming.Policy.GetPolicyProvider(FSharpLanguage.Instance, fsFile.GetSourceFile(), settings)
    let context = ParameterNameHintsHighlightingContext(settings, namingManager, nameParser, policyProvider, blackListMatcher, strategy, customProviders)

    let resolveParamOwner (reference: FSharpSymbolReference) =
        use compilationContextCookie = CompilationContextCookie.OverrideOrCreate(fsFile.GetResolveContext())
        reference.Resolve().DeclaredElement.As<IParametersOwner>()

    let resolveMethod (appExpr: IAppExpr) =
        let refExpr = appExpr.FunctionExpression.As<IReferenceExpr>()
        if isNull refExpr then null else resolveParamOwner refExpr.Reference

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitPrefixAppExpr(prefixAppExpr, consumer) =
        let method = resolveMethod prefixAppExpr
        // todo: offset 1 for extension methods
        let parametersOffset = 0
        let argumentsOwner = prefixAppExpr :> IArgumentsOwner

        let highlightingRange = prefixAppExpr.ArgumentExpression.GetHighlightingRange()
        let highlightings =
            // todo: is this necessary/does it make a difference?
            match method with
            | :? IConstructor as constructor ->
                parameterNameHintsHighlightingProvider.GetHighlightingsForConstructor(context, argumentsOwner, constructor, highlightingRange, Action x.CheckForInterrupt)
            | _ ->
                parameterNameHintsHighlightingProvider.GetHighlightingsForMethod(context, argumentsOwner, method, parametersOffset, highlightingRange, Action x.CheckForInterrupt)

        for highlighting in highlightings do
            consumer.AddHighlighting(highlighting)

    override x.VisitAttribute(attribute, consumer) =
        let constructor = resolveParamOwner(attribute.Reference).As<IConstructor>()
        if isNull constructor then () else

        let argumentsOwner = attribute :> IArgumentsOwner

        let highlightingRange = attribute.ArgExpression.GetHighlightingRange()
        let highlightings = parameterNameHintsHighlightingProvider.GetHighlightingsForConstructor(context, argumentsOwner, constructor, highlightingRange, Action x.CheckForInterrupt)

        for highlighting in highlightings do
            consumer.AddHighlighting(highlighting)

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type ParameterNameHintStage
        (languageManager: ILanguageManager, namingManager: NamingManager, nameParser: NameParser,
         parameterNameHintsOptionsStore: IParameterNameHintsOptionsStore,
         blackListManager: IParameterNameHintsBlackListManager,
         parameterNameHintsHighlightingProvider: IManagedLanguageParameterNameHintsHighlightingProvider,
         customProviders: ICustomManagedLanguageParameterNameHintsHighlightingProvider seq) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        if not (parameterNameHintsOptionsStore.IsEnabled settings) then null else

        let blackListMatcher = blackListManager.GetMatcher(FSharpLanguage.Instance, settings)
        let strategy = languageManager.GetService<ManagedLanguageParameterNameHintsHighlightingStrategy>(FSharpLanguage.Instance)
        ParameterNameHintHighlightingProcess(fsFile, settings, daemonProcess, namingManager, nameParser, blackListMatcher, strategy, parameterNameHintsHighlightingProvider, customProviders) :> _
