namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open System
open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Tree
open JetBrains.ReSharper.Psi.Caches.SymbolCache
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.Util

module internal ExpectoTestFunctions =
    let testFunctionTypeNames =
        [| clrTypeName "Expecto.Tests"
           clrTypeName "Expecto.ExpectoFsCheckModule"
           clrTypeName "Expecto.CSharp.Runner" |]

    let testsAttrTypeNames =
        [| clrTypeName "Expecto.TestsAttribute"
           clrTypeName "Expecto.FTestsAttribute"
           clrTypeName "Expecto.PTestsAttribute" |]

    let testTypeName = clrTypeName "Expecto.Test"

    let isTestFunction (method: IMethod) =
        if isNull method then false else

        let containingType = method.GetContainingType()
        isNotNull containingType && Array.contains (containingType.GetClrName()) testFunctionTypeNames

    let hasTestsAttribute (attributesOwner: IAttributesOwner) =
        testsAttrTypeNames |> Array.exists (fun typeName -> attributesOwner.HasAttributeInstance(typeName, false))

    let isTestType (t: IType) =
        match t with
        | :? IDeclaredType as declaredType -> declaredType.GetClrName().Equals(testTypeName)
        | _ -> false

    let isOfTestType (typeMember: ITypeMember) =
        match typeMember with
        | :? IMethod as method -> isTestType method.ReturnType
        | :? ITypeOwner as typeOwner -> isTestType typeOwner.Type
        | _ -> false

    let isExported (typeMember: ITypeMember) =
        let accessibilityDomain = SharedImplUtil.CalcAccessibilityDomain(typeMember)
        accessibilityDomain.DomainType = AccessibilityDomain.AccessibilityDomainType.PUBLIC

    let isNameParameter (parameter: IParameter) =
        parameter.ShortName = "name" && parameter.ContainingParametersOwner.ShortName <> "testSequencedGroup"

    let hasNameParameter (testFunction: IMethod) =
        testFunction.Parameters |> Seq.exists isNameParameter

    let rec isTestOrTestCollectionType (parameterType: IType) =
        if isTestType parameterType then true else

        match parameterType with
        | :? IArrayType as arrayType -> isTestOrTestCollectionType arrayType.ElementType
        | :? IDeclaredType as declaredType ->
            let elementTypes = CollectionTypeUtil.GetElementTypesForGenericEnumerable(declaredType, false)
            elementTypes |> Seq.exists isTestOrTestCollectionType
        | _ -> false

    let hasTestParameter (testFunction: IMethod) =
        testFunction.Parameters |> Seq.exists (fun parameter -> isTestOrTestCollectionType parameter.Type)

    let isTheoryCasesParameter (parameter: IParameter) =
        parameter.ShortName = "cases"

    let createsGroup (testFunction: IMethod) =
        hasTestParameter testFunction || testFunction.Parameters |> Seq.exists isTheoryCasesParameter

    let isTestCaseFunction (testFunction: IMethod) =
        hasNameParameter testFunction && not (createsGroup testFunction)

    let getTestFunctionNames (psiModule: IPsiModule) =
        let symbolScope = getSymbolScope psiModule false
        let names = HashSet<string>()

        for typeName in testFunctionTypeNames do
            for typeElement in symbolScope.GetTypeElementsByCLRName(typeName) do
                for testFunction in typeElement.Methods do
                    if hasNameParameter testFunction || hasTestParameter testFunction then
                        names.Add(testFunction.ShortName) |> ignore
                        names.Add(testFunction.GetSourceName()) |> ignore

        names


type internal ExpectoTestPath =
    { Root: ITypeMemberDeclaration
      Names: string[]
      IsFull: bool
      ExistsOnce: bool }

    static member ComputedName: string = null

    member this.IsLastNameComputed = isNull (Array.last this.Names)
    member this.LiteralParentNames = this.Names |> Seq.take (this.Names.Length - 1) |> Seq.filter isNotNull
    member this.HasLiteralParentName = not (Seq.isEmpty this.LiteralParentNames)
    member this.ComputedNameCount = this.Names |> Seq.filter isNull |> Seq.length
    member this.HasAllLiteralNames = this.IsFull && this.ComputedNameCount = 0
    member this.IsKnownFromSource = this.HasAllLiteralNames && this.ExistsOnce
    member this.Append(name: string) = { this with Names = Array.append this.Names [| name |] }


[<NoComparison; NoEquality>]
type internal ExpectoNamedTest =
    { Path: ExpectoTestPath
      IsGroup: bool
      NameNode: ITreeNode
      TestNode: ITreeNode }


module internal ExpectoNamedTests =
    let rec private isSubsequence (names: string list) (otherNames: string list) =
        match names, otherNames with
        | [], _ -> true
        | _, [] -> false
        | name :: restNames, otherName :: restOtherNames ->

        if name = otherName then isSubsequence restNames restOtherNames else isSubsequence names restOtherNames

    let private isSameOrComputed (name: string) (elementName: string) =
        isNull name || name = elementName

    let private matchesFullPath (names: string[]) (elementPath: string[]) =
        names.Length = elementPath.Length && Array.forall2 isSameOrComputed names elementPath

    let private matchesPartialPath (path: ExpectoTestPath) (elementPath: string[]) =
        let literalParentNames = List.ofSeq path.LiteralParentNames
        let elementParentNames = elementPath |> Seq.take (elementPath.Length - 1) |> List.ofSeq
        let hasLiteralName = not path.IsLastNameComputed || path.HasLiteralParentName

        hasLiteralName && isSubsequence literalParentNames elementParentNames

    let private matches (elementPath: string[]) (isTestContainer: bool) (namedTest: ExpectoNamedTest) =
        let path = namedTest.Path
        if not (isSameOrComputed (Array.last path.Names) (Array.last elementPath)) then false else
        if path.IsLastNameComputed && namedTest.IsGroup <> isTestContainer then false else
        if path.Names.Length > elementPath.Length then false else

        if path.IsFull then matchesFullPath path.Names elementPath else matchesPartialPath path elementPath

    let private getInexactness (namedTest: ExpectoNamedTest) =
        let path = namedTest.Path
        let isOnlyLastNameKnown = not path.IsFull && not path.HasLiteralParentName
        isOnlyLastNameKnown, path.IsLastNameComputed, not path.IsFull, path.ComputedNameCount, -path.Names.Length

    let getMostExactMatches (elementPath: string[]) (isTestContainer: bool) (namedTests: seq<ExpectoNamedTest>) =
        let matchingTests = namedTests |> Seq.filter (matches elementPath isTestContainer) |> Array.ofSeq
        if matchingTests.Length = 0 then matchingTests else

        let lowestInexactness = matchingTests |> Seq.map getInexactness |> Seq.min
        matchingTests |> Array.filter (fun namedTest -> getInexactness namedTest = lowestInexactness)


[<AbstractClass; AllowNullLiteral>]
type internal ExpectoTestSyntax() =
    abstract GetDeclarations: file: IFile -> seq<ITypeMemberDeclaration>
    abstract GetSourceName: declaration: ITypeMemberDeclaration -> string
    abstract TryGetMemberDeclaration: declaration: IDeclaration -> ITypeMemberDeclaration
    abstract KeepsFullPath: node: ITreeNode -> bool
    abstract IsLoopOrCondition: node: ITreeNode -> bool
    abstract AddsArgsToTestList: argsOwner: IArgumentsOwner -> bool
    abstract GetArgNodes: argsOwner: IArgumentsOwner -> seq<ITreeNode>
    abstract TryGetInvokedReference: argsOwner: IArgumentsOwner -> IReference
    abstract GetArgsWithParameters: argsOwner: IArgumentsOwner * testFunction: IMethod -> (IArgument * IParameter)[]
    abstract TryGetReference: node: ITreeNode -> IReference


type internal ExpectoFSharpTestSyntax() =
    inherit ExpectoTestSyntax()

    override this.GetDeclarations(file) =
        CachedDeclarationsCollector.Run<ITypeMemberDeclaration>(file)
        |> Seq.filter (fun declaration -> declaration :? ITopBinding || declaration :? IMemberDeclaration)

    override this.GetSourceName(declaration) =
        let fsDeclaration = declaration :?> IFSharpDeclaration
        fsDeclaration.SourceName

    override this.TryGetMemberDeclaration(declaration) =
        match declaration.GetContainingNode<ITopBinding>(true) with
        | null -> declaration.As<IMemberDeclaration>() :> ITypeMemberDeclaration
        | binding -> binding

    override this.KeepsFullPath(node) =
        let qualifiedExpr = ReferenceExprNavigator.GetByQualifier(node.As<IFSharpExpression>())
        if isNotNull qualifiedExpr then false else

        node :? IChameleonExpression || node :? IParenExpr || node :? ITypedExpr || node :? IArrayOrListExpr ||
        node :? ISequentialExpr || node :? ILetOrUseExpr || node :? IYieldOrReturnExpr || node :? IBinaryAppExpr ||
        node :? IPrefixAppExpr || node :? IReferenceExpr

    override this.IsLoopOrCondition(node) =
        node :? IForLikeExpr || node :? IWhileExpr || node :? IIfExpr || node :? IMatchExpr || node :? IMatchClause

    override this.AddsArgsToTestList(argsOwner) =
        let binaryApp = argsOwner.As<IBinaryAppExpr>()
        if isNull binaryApp then false else

        binaryApp.ShortName = "::" || isPredefinedInfixOpApp "@" binaryApp

    override this.GetArgNodes(argsOwner) =
        match argsOwner with
        | :? IAppExpr as appExpr -> getAppFunctionAndArgs appExpr |> snd |> Seq.cast
        | _ -> Seq.cast argsOwner.Arguments

    override this.TryGetInvokedReference(argsOwner) =
        match argsOwner with
        | :? IAppExpr as appExpr -> this.TryGetReference(getInvokedReferenceExpr appExpr)
        | _ -> null

    override this.GetArgsWithParameters(argsOwner, testFunction) =
        let fsArgsOwner = argsOwner :?> IFSharpArgumentsOwner
        let args = fsArgsOwner.ParameterArguments
        let parameters = testFunction.Parameters

        [| for i in 0 .. min args.Count parameters.Count - 1 do
             let arg = args[i]
             if isNotNull arg then
                 arg, parameters[i] |]

    override this.TryGetReference(node) =
        let refExpr = node.As<IReferenceExpr>()
        if isNull refExpr then null else refExpr.Reference


type internal ExpectoCSharpTestSyntax() =
    inherit ExpectoTestSyntax()

    override this.GetDeclarations(file) =
        CachedDeclarationsCollector.Run<ITypeMemberDeclaration>(file)
        |> Seq.filter (fun declaration -> not (declaration :? ITypeDeclaration))

    override this.GetSourceName(declaration) = declaration.DeclaredName

    override this.TryGetMemberDeclaration(declaration) =
        if declaration :? ITypeDeclaration then null else declaration.As<ITypeMemberDeclaration>()

    override this.KeepsFullPath(node) =
        let qualifiedExpr = ReferenceExpressionNavigator.GetByQualifierExpression(node.As<ICSharpExpression>())
        if isNotNull qualifiedExpr then false else

        node :? IAccessorDeclaration || node :? IArrowExpressionClause || node :? IBlock || node :? IReturnStatement ||
        node :? IExpressionInitializer || node :? IArrayCreationExpression || node :? IArrayInitializer ||
        node :? ICollectionExpression || node :? ICollectionExpressionElement || node :? IObjectCreationExpression ||
        node :? ICollectionInitializer || node :? ICollectionElementInitializer || node :? IArgumentList ||
        node :? ICSharpArgument || node :? IInvocationExpression || node :? IReferenceExpression ||
        node :? IParenthesizedExpression || node :? ICastExpression

    override this.IsLoopOrCondition(node) =
        node :? IConditionalTernaryExpression || node :? IIfStatement || node :? IForeachStatement ||
        node :? IForStatement || node :? IWhileStatement

    override this.AddsArgsToTestList(argsOwner) = argsOwner :? ICollectionElementInitializer

    override this.GetArgNodes(argsOwner) = Seq.cast argsOwner.Arguments

    override this.TryGetInvokedReference(argsOwner) =
        let invocation = argsOwner.As<IInvocationExpression>()
        if isNull invocation then null else this.TryGetReference(invocation.InvokedExpression)

    override this.GetArgsWithParameters(argsOwner, _) =
        [| for arg in argsOwner.Arguments do
             let parameter = arg.MatchingParameter
             if isNotNull parameter then
                 arg, parameter.Element |]

    override this.TryGetReference(node) =
        let refExpr = node.As<IReferenceExpression>()
        if isNull refExpr then null else refExpr.Reference


type internal ExpectoTestDeclarationWalker(file: IFile, interrupted: Func<bool>, followsReferencesToOtherFiles: bool) =
    static let fsharpSyntax = ExpectoFSharpTestSyntax() :> ExpectoTestSyntax
    static let csharpSyntax = ExpectoCSharpTestSyntax() :> ExpectoTestSyntax

    static let tryGetSyntax (file: IFile) =
        match file with
        | :? IFSharpFile -> fsharpSyntax
        | :? ICSharpFile -> csharpSyntax
        | _ -> null

    let fileSyntax = tryGetSyntax file
    let sourceFile = file.GetSourceFile()

    let declarations = if isNull fileSyntax then [||] else Array.ofSeq (fileSyntax.GetDeclarations(file))
    let declarationNames = HashSet<string>(declarations |> Seq.map fileSyntax.GetSourceName)
    let declarationsInProgress = HashSet<ITypeMemberDeclaration>()
    let walkedDeclarations = HashSet<ITypeMemberDeclaration * ExpectoTestPath>(HashIdentity.Structural)
    let argsWithUnknownPath = HashSet<ITreeNode>()
    let testFunctionNamesByModule = Dictionary<IPsiModule, HashSet<string>>()
    let isTestByParameter = Dictionary<IParameter, bool>()
    let namedTests = List<ExpectoNamedTest>()

    let isRoot (declaration: ITypeMemberDeclaration) =
        let typeMember = declaration.DeclaredElement
        isNotNull typeMember && typeMember.IsStatic && ExpectoTestFunctions.isExported typeMember &&
        ExpectoTestFunctions.hasTestsAttribute typeMember && ExpectoTestFunctions.isOfTestType typeMember

    let roots, nonRootDeclarations = Array.partition isRoot declarations

    let tryResolveTestFunction (reference: IReference): IMethod =
        if isNull reference then null else

        let psiModule = reference.GetTreeNode().GetPsiModule()
        let testFunctionNames =
            testFunctionNamesByModule.GetOrCreateValue(psiModule, ExpectoTestFunctions.getTestFunctionNames)

        if not (testFunctionNames.Contains(reference.GetName())) then null else

        let method = reference.Resolve().DeclaredElement.As<IMethod>()
        if ExpectoTestFunctions.isTestFunction method then method else null

    let isTestParameter (parameter: IParameter) =
        isTestByParameter.GetOrCreateValue(parameter, fun (parameter: IParameter) ->
            ExpectoTestFunctions.isTestOrTestCollectionType parameter.Type)

    let tryGetMemberDeclaration (declaration: IDeclaration): ITypeMemberDeclaration =
        let syntax = tryGetSyntax (declaration.GetContainingFile())
        if isNull syntax then null else syntax.TryGetMemberDeclaration(declaration)

    let getDeclarations (typeMember: ITypeMember): seq<IDeclaration> =
        if followsReferencesToOtherFiles then typeMember.GetDeclarations() else typeMember.GetDeclarationsIn(sourceFile)

    let getReferencedDeclarations (reference: IReference): seq<ITypeMemberDeclaration> =
        let followsReference = followsReferencesToOtherFiles || declarationNames.Contains(reference.GetName())
        if not followsReference then Seq.empty else

        match reference.Resolve().DeclaredElement with
        | :? ITypeElement -> Seq.empty
        | :? ITypeMember as typeMember ->
            getDeclarations typeMember |> Seq.map tryGetMemberDeclaration |> Seq.filter isNotNull
        | _ -> Seq.empty

    let rec walkNode (syntax: ExpectoTestSyntax) (parentPath: ExpectoTestPath) (node: ITreeNode) =
        if isNull node then () else
        if interrupted.Invoke() then raise (OperationCanceledException()) else

        let parentPath = if argsWithUnknownPath.Contains(node) then { parentPath with IsFull = false } else parentPath

        let argsOwner = node.As<IArgumentsOwner>()
        let invokedReference = if isNull argsOwner then null else syntax.TryGetInvokedReference(argsOwner)
        let invokedTestFunction = tryResolveTestFunction invokedReference
        if isNotNull invokedTestFunction then
            walkTestFunctionArgs syntax parentPath argsOwner invokedTestFunction else

        let parentPath =
            if syntax.KeepsFullPath(node) then parentPath
            elif syntax.IsLoopOrCondition(node) then { parentPath with ExistsOnce = false }
            else { parentPath with IsFull = false }

        let reference = syntax.TryGetReference(node)
        let testFunctionUsedAsValue = tryResolveTestFunction reference
        if isNotNull testFunctionUsedAsValue && ExpectoTestFunctions.isTestCaseFunction testFunctionUsedAsValue then
            let path = parentPath.Append(ExpectoTestPath.ComputedName)
            namedTests.Add({ Path = path; IsGroup = false; NameNode = node; TestNode = node })

        if isNotNull reference then
            for declaration in getReferencedDeclarations reference do
                walkDeclaration parentPath declaration

        let invokesUnknownFunction = isNotNull argsOwner && not (syntax.AddsArgsToTestList(argsOwner))
        if parentPath.IsFull && invokesUnknownFunction then
            for argNode in syntax.GetArgNodes(argsOwner) do
                argsWithUnknownPath.Add(argNode) |> ignore

        for child in node.Children() do
            walkNode syntax parentPath child

    and walkTestFunctionArgs (syntax: ExpectoTestSyntax) (parentPath: ExpectoTestPath) (argsOwner: IArgumentsOwner)
            (testFunction: IMethod) =
        let argsWithParameters = syntax.GetArgsWithParameters(argsOwner, testFunction)
        if not (ExpectoTestFunctions.hasNameParameter testFunction) then
            walkTestArgs syntax parentPath argsWithParameters else

        match argsWithParameters |> Array.tryFind (snd >> ExpectoTestFunctions.isNameParameter) with
        | None -> ()
        | Some(nameArg, _) ->

        let nameExpr = nameArg.Expression
        let name = if isNull nameExpr then ExpectoTestPath.ComputedName else nameExpr.ConstantValue.AsString()
        let path = parentPath.Append(name)

        let testNode = argsOwner :> ITreeNode
        let isGroup = ExpectoTestFunctions.createsGroup testFunction
        let isGroupWithComputedName = isGroup && path.IsLastNameComputed
        if isNotNull nameExpr && not isGroupWithComputedName then
            namedTests.Add({ Path = path; IsGroup = isGroup; NameNode = nameExpr; TestNode = testNode })

        for arg, parameter in argsWithParameters do
            let casesExpr = arg.Expression
            if ExpectoTestFunctions.isTheoryCasesParameter parameter && isNotNull casesExpr then
                let casePath = path.Append(ExpectoTestPath.ComputedName)
                namedTests.Add({ Path = casePath; IsGroup = false; NameNode = casesExpr; TestNode = testNode })

        walkTestArgs syntax path argsWithParameters

    and walkTestArgs (syntax: ExpectoTestSyntax) (parentPath: ExpectoTestPath)
            (argsWithParameters: (IArgument * IParameter)[]) =
        for arg, parameter in argsWithParameters do
            if isTestParameter parameter then
                walkNode syntax parentPath arg

    and walkDeclaration (parentPath: ExpectoTestPath) (declaration: ITypeMemberDeclaration) =
        if declarationsInProgress.Contains(declaration) then () else
        if not (walkedDeclarations.Add((declaration, parentPath))) then () else

        declarationsInProgress.Add(declaration) |> ignore

        let syntax = tryGetSyntax (declaration.GetContainingFile())
        for child in declaration.Children() do
            walkNode syntax parentPath child

        declarationsInProgress.Remove(declaration) |> ignore

    member this.NamedTests: IReadOnlyList<ExpectoNamedTest> = namedTests

    member this.WalkRoots() =
        for root in roots do
            walkDeclaration { Root = root; Names = [||]; IsFull = true; ExistsOnce = true } root

    member this.WalkNonRootDeclarations() =
        for declaration in nonRootDeclarations do
            walkDeclaration { Root = null; Names = [||]; IsFull = false; ExistsOnce = true } declaration
