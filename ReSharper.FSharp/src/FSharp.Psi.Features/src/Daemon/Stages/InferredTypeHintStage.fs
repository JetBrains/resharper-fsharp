namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Feature.Services.TypeNameHints
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

type LocalReferencePatternVisitor
        (fsFile: IFSharpFile, highlightingContext: TypeNameHintHighlightingContext, namingPolicyProvider, nameParser) =
    inherit TreeNodeVisitor<IHighlightingConsumer>()

    let isTypeEvidentFromVariableNamePrefix (typ: IType) (variableNameParts: string[]) =
        if not (typ.IsBool()) then false else

        if variableNameParts.Length > 0 then
            let prefix = variableNameParts.[0].ToLowerInvariant()
            prefix = "has" || prefix = "is"
        else
            false

    let isEvidentFromVariableName (fsType: FSharpType) variableName =
        if not highlightingContext.HideTypeNameHintsWhenTypeNameIsEvidentFromVariableName then false else

        let typ = fsType.MapType(EmptyList.InstanceList, fsFile.GetPsiModule())
        if not (typ.IsValid()) then false else

        let variableNameParts = NamesHelper.GetParts(nameParser, namingPolicyProvider, variableName)
        if isTypeEvidentFromVariableNamePrefix typ variableNameParts then true else

        match typ.GetTypeElement() with
        | null -> false
        | typeElement ->

        let typeName = typeElement.ShortName
        String.Equals(typeName, variableName, StringComparison.OrdinalIgnoreCase) ||
        not (String.IsNullOrEmpty(typeName)) &&
        not (String.IsNullOrEmpty(variableName)) &&
        NamesHelper.IsLike(variableNameParts, NamesHelper.GetParts(nameParser, namingPolicyProvider, typeName))

    let isTypeOfPatternEvident (pattern: IFSharpPattern) =

        // v-- not evident
        // x::y::z
        //    ^--^-- evident
        let tuplePat = TuplePatNavigator.GetByPattern pattern
        if isNotNull tuplePat then
            let consPat = ConsPatNavigator.GetByPattern1 tuplePat
            if isNull consPat then false else

            // Are we nested inside another ConsPat?
            let parentTuplePat = TuplePatNavigator.GetByPattern consPat
            if isNull parentTuplePat then tuplePat.Patterns.IndexOf pattern <> 0 else
            isNotNull (ConsPatNavigator.GetByPattern1 parentTuplePat)

        else

        //  v-- not evident
        // [x; y; z]
        //     ^--^-- evident
        let listOrListPat = ArrayOrListPatNavigator.GetByPattern pattern
        if isNotNull listOrListPat then
            match Seq.tryHead listOrListPat.PatternsEnumerable with
            | None -> false
            | Some head -> head <> pattern

        else

        false

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    override x.VisitLocalReferencePat(localRefPat, consumer) =
        let pat = localRefPat.IgnoreParentParens()
        if isNotNull (TypedPatNavigator.GetByPattern(pat)) then () else

        let binding = BindingNavigator.GetByHeadPattern(pat)
        if isNotNull binding && isNotNull binding.ReturnTypeInfo then () else

        let variableName = localRefPat.SourceName
        if variableName = SharedImplUtil.MISSING_DECLARATION_NAME then () else

        if isTypeOfPatternEvident pat then () else

        match box (localRefPat.GetFSharpSymbolUse()) with
        | null -> ()
        | symbolUse ->

        let symbolUse = symbolUse :?> FSharpSymbolUse
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when not (isEvidentFromVariableName mfv.FullType variableName) ->
            let typeNameStr =
                symbolUse.DisplayContext.WithShortTypeNames(true)
                |> mfv.FullType.Format

            let range = localRefPat.GetNavigationRange().EndOffsetRange()

            // todo: TypeNameHintHighlighting can be used when RIDER-39605 is resolved
            consumer.AddHighlighting(TypeHintHighlighting(typeNameStr, range))
        | _ -> ()

type InferredTypeHintHighlightingProcess
        (fsFile, settings: IContextBoundSettingsStore, highlightingContext: TypeNameHintHighlightingContext,
         namingManager: NamingManager, nameParser: NameParser, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let namingPolicyProvider = namingManager.Policy.GetPolicyProvider(fsFile.Language, fsFile.GetSourceFile())
    let hideHintsForEvidentTypes = highlightingContext.HideTypeNameHintsForImplicitlyTypedVariablesWhenTypeIsEvident

    let visitor = LocalReferencePatternVisitor(fsFile, highlightingContext, namingPolicyProvider, nameParser)

    let visitLetBindings (letBindings: ILetBindings) consumer =
        if not highlightingContext.ShowTypeNameHintsForImplicitlyTypedVariables then () else

        for binding in letBindings.Bindings do
            if hideHintsForEvidentTypes && isTypeEvident binding.Expression then () else

            match binding.HeadPattern with
            | null -> ()
            | headPat -> headPat.Accept(visitor, consumer)

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

    override x.VisitLetModuleDecl(moduleDecl, consumer) =
        visitLetBindings moduleDecl consumer

    override x.VisitLetOrUseExpr(letOrUseExpr, consumer) =
        visitLetBindings letOrUseExpr consumer

    override x.VisitMemberParamsDeclaration(paramDecl, consumer) =
        if highlightingContext.ShowTypeNameHintsForImplicitlyTypedVariables then
            match paramDecl.Pattern with
            | null -> ()
            | pattern -> pattern.Accept(visitor, consumer)

    override x.VisitMatchClause(matchClause, consumer) =
        if highlightingContext.ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions then
            match matchClause.Pattern with
            | null -> ()
            | pattern -> pattern.Accept(visitor, consumer)

    override x.VisitLambdaExpr(lambdaExpr, consumer) =
        if not highlightingContext.ShowTypeNameHintsForLambdaExpressionParameters then () else

        for pattern in lambdaExpr.Patterns do
            if isNotNull pattern then
                pattern.Accept(visitor, consumer)

[<DaemonStage(StagesBefore = [| typeof<FSharpErrorsStage> |])>]
type InferredTypeHintStage(namingManager: NamingManager, nameParser: NameParser) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        let context = TypeNameHintHighlightingContext(settings)

        let isEnabled =
            context.ShowTypeNameHintsForImplicitlyTypedVariables ||
            context.ShowTypeNameHintsForLambdaExpressionParameters ||
            context.ShowTypeNameHintsForVarDeclarationsInPatternMatchingExpressions

        if not isEnabled then null else
        InferredTypeHintHighlightingProcess(fsFile, settings, context, namingManager, nameParser, daemonProcess) :> _
