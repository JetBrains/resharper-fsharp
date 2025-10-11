module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpResolveUtil

open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Searching
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl.Reflection2
open JetBrains.ReSharper.Psi.Tree

/// Workaround for case where unqualified resolve may return module with implicit suffix instead of type.
let private resolvesToAssociatedModule (declaredElement: IDeclaredElement) (unqualifiedElement: IDeclaredElement) (reference: FSharpSymbolReference) =
    let unqualifiedTypeElement = unqualifiedElement.As<CompiledTypeElement>()
    if isNull unqualifiedTypeElement then false else

    let shortName = reference.GetName()
    if not (unqualifiedTypeElement.ShortName.HasModuleSuffix() && not (shortName.HasModuleSuffix())) then false else
    if not (unqualifiedTypeElement :? FSharpCompiledModule) then false else

    let typeElement = FSharpImplUtil.TryGetAssociatedType(unqualifiedTypeElement, shortName)
    declaredElement.Equals(typeElement)

let private resolvesTo (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) qualified resolveExpr opName =
    reference.ResolveWithFcs(opName, resolveExpr, qualified)
    |> Seq.exists (fun symbolUse ->
        let referenceOwner = reference.GetElement()
        let unqualifiedElement = symbolUse.Symbol.GetDeclaredElement(referenceOwner.GetPsiModule(), referenceOwner)
        if declaredElement.Equals(unqualifiedElement) then true else

        resolvesToAssociatedModule declaredElement unqualifiedElement reference
    )

let resolvesToUnqualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) resolveExpr opName =
    resolvesTo declaredElement reference false resolveExpr opName

let resolvesToQualified (declaredElement: IDeclaredElement) (reference: FSharpSymbolReference) resolveExpr opName =
    resolvesTo declaredElement reference true resolveExpr opName

let resolvesToFcsSymbol (fcsSymbol: FSharpSymbol) (reference: FSharpSymbolReference) qualified resolveExpr opName =
    match reference.ResolveWithFcs(opName, resolveExpr, qualified) with
    | [] -> false
    | symbolUse :: _ ->

    let resolvedFcsSymbol = symbolUse.Symbol
    if resolvedFcsSymbol.IsEffectivelySameAs(fcsSymbol) then true else

    if not (resolvedFcsSymbol :? FSharpEntity) then false else

    let referenceOwner = reference.GetElement()
    let psiModule = referenceOwner.GetPsiModule()

    let declaredElement = fcsSymbol.GetDeclaredElement(psiModule, referenceOwner)
    let resolvedElement = resolvedFcsSymbol.GetDeclaredElement(psiModule, referenceOwner)

    resolvesToAssociatedModule declaredElement resolvedElement reference

/// Workaround check for compiler issue with delegates not fully shadowing other types, see dotnet/fsharp#10228.
let mayShadowPartially (newExpr: ITreeNode) (data: ElementProblemAnalyzerData) (fcsSymbol: FSharpSymbol) =
    let fcsEntity = fcsSymbol.As<FSharpEntity>()
    if isNull fcsEntity || not fcsEntity.IsDelegate || not fcsEntity.IsFSharp then false else

    let typeElement = fcsEntity.GetTypeElement(data.SourceFile.PsiModule)
    if isNull typeElement then false else

    let sourceName = typeElement.GetSourceName()
    let symbolScope = getSymbolScope data.SourceFile.PsiModule false
    let typeElements =
        symbolScope.GetElementsByShortName(sourceName) // todo: find by source name
        |> Array.filter (fun e -> e :? ITypeElement && not (e.Equals(typeElement)))

    if Array.isEmpty typeElements then false else

    let opens = data.GetData(openedModulesProvider).OpenedModuleScopes

    typeElements
    |> Seq.cast<ITypeElement>
    |> Seq.map getContainingEntity
    |> Seq.collect (fun element -> opens.GetValuesSafe(element.GetSourceName()))
    |> Seq.toArray
    |> OpenScope.inAnyScope newExpr

let resolvesToPredefinedFunction (context: ITreeNode) name opName =
    let checkerService = context.GetContainingFile().As<IFSharpFile>().CheckerService
    match checkerService.ResolveNameAtLocation(context, [name], false, opName) with
    | symbolUse :: _ ->
        match symbolUse.Symbol with
        | :? FSharpMemberOrFunctionOrValue as symbol ->
            match predefinedFunctionTypes.TryGetValue(name), symbol.DeclaringEntity with
            | (true, typeName), Some entity -> typeName.FullName = entity.FullName
            | _ -> false
        | _ -> false
    | [] -> false

let getAllMethods (reference: FSharpSymbolReference) shiftEndColumn opName =
    let referenceOwner = reference.GetElement()
    match referenceOwner.FSharpFile.GetParseAndCheckResults(true, opName) with
    | None -> None
    | Some results ->

    let names = 
        match referenceOwner with
        | :? IFSharpQualifiableReferenceOwner as referenceOwner -> List.ofSeq referenceOwner.Names
        | _ -> [reference.GetName()]

    let identifier = referenceOwner.NameIdentifier
    if isNull identifier then None else

    let endCoords = identifier.GetDocumentEndOffset().ToDocumentCoords()
    let endLine = int endCoords.Line + 1
    let endColumn = int endCoords.Column + if shiftEndColumn then 1 else 0

    let checkResults = results.CheckResults
    Some (checkResults, checkResults.GetMethodsAsSymbols(endLine, endColumn, "", names))

// TODO: check if field can be resolved without qualifier
// TODO: support generics
let findRequiredQualifierForRecordField (fieldBinding: IRecordFieldBinding) =
    let rec findRequiredQualifier
        (context: IFSharpTreeNode)
        (declaringEntity: FSharpEntity option)
        (field: FSharpField) qualifiedField =

        match declaringEntity with
        | None -> None
        | Some declaringEntity ->

        let qualifiedField = declaringEntity.DisplayName :: qualifiedField
        match context.CheckerService.ResolveNameAtLocation(context, qualifiedField, true, "findRequiredQualifierForRecordField") with
        | symbolUse :: _ when symbolUse.Symbol.IsEffectivelySameAs(field) ->
            qualifiedField
            |> List.take (qualifiedField.Length - 1)
            |> Some
        | _ -> findRequiredQualifier context declaringEntity.DeclaringEntity field qualifiedField

    let field = fieldBinding.ReferenceName.Reference.GetFcsSymbol().As<FSharpField>()
    findRequiredQualifier fieldBinding field.DeclaringEntity field [field.DisplayName]

let isInvocation (mfv: FSharpMemberOrFunctionOrValue) (refExpr: IReferenceExpr) =
    isNotNull (BinaryAppExprNavigator.GetByOperator(refExpr)) ||

    let argsCount = getPrefixAppExprArgs refExpr |> Seq.length
    let fcsType = getAbbreviatedType mfv.FullType
    let mfvTypeParamCount = FcsTypeUtil.getFunctionTypeArgs false fcsType |> Seq.length
    argsCount >= mfvTypeParamCount

let isRecursiveApplication (declaredElement: IDeclaredElement) (refExpr: IReferenceExpr) =
    let decl = refExpr.GetContainingNode<IParameterOwnerMemberDeclaration>()
    isNotNull decl && declaredElement = decl.DeclaredElement

let isInTailRecursivePosition (declaredElement: IDeclaredElement) (expr: IFSharpExpression) =
    let outermostExpr =
        let rec loop (expr: IFSharpExpression) =
            let ifExpr = IfExprNavigator.GetByBranchExpression(expr)
            if isNotNull ifExpr then
                loop ifExpr else

            expr

        let expr = expr.GetOutermostParentExpressionFromItsReturn()
        loop expr

    let decl = ParameterOwnerMemberDeclarationNavigator.GetByExpression(outermostExpr.IgnoreParentParens())
    isNotNull decl && decl.DeclaredElement = declaredElement

let isPreceding (context: ITreeNode) (declaredElement: IDeclaredElement) =
    let sourceFile = context.GetSourceFile().NotNull()
    declaredElement.GetDeclarationsIn(sourceFile)
    |> Seq.exists (fun decl -> decl.GetDocumentEndOffset().Offset < context.GetDocumentStartOffset().Offset)

let canReference (reference: FSharpSymbolReference) (typeElement: ITypeElement) =
    let referenceOwner = reference.GetElement()
    referenceOwner.GetPsiModule() <> typeElement.Module ||

    let sourceFile = referenceOwner.GetSourceFile()
    isNotNull sourceFile &&
    FSharpSearchFilter.CanContainReference(typeElement, sourceFile, referenceOwner.CheckerService.FcsProjectProvider) &&
    (not (typeElement.GetSourceFiles().Contains(sourceFile)) || isPreceding referenceOwner typeElement)
