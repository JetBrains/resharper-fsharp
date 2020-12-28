namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming.Elements
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Naming.Extentions
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.Util

module Traverse =
    type TraverseStep =
        | TupleItem of item: int

    let makeTuplePatPath pat =
        let rec tryMakePatPath path (IgnoreParenPat fsPattern: IFSharpPattern) =
            match fsPattern.Parent with
            | :? ITuplePat as tuplePat ->
                let item = tuplePat.Patterns.IndexOf(fsPattern)
                Assertion.Assert(item <> -1, "item <> -1")
                tryMakePatPath (TupleItem(item) :: path) tuplePat
            | _ -> fsPattern, path

        tryMakePatPath [] pat

    let rec tryTraverseExprPath (path: TraverseStep list) (IgnoreInnerParenExpr expr: IFSharpExpression) =
        match path with
        | [] -> expr
        | step :: rest ->

        match expr, step with
        | :? ITupleExpr as tupleExpr, TupleItem(n) ->
            let tupleItems = tupleExpr.Expressions
            if tupleItems.Count <= n then null else
            tryTraverseExprPath rest tupleItems.[n]

        | _ -> null

[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit ClrNamingLanguageServiceBase(language)

    let notAllowedInTypes =
        // F# 4.1 spec: 3.4 Identifiers and Keywords
        [| '.'; '+'; '$'; '&'; '['; ']'; '/'; '\\'; '*'; '\"'; '`' |]

    let abbreviationsMap =
        [| "object", "o"
           "char", "c"
           "int32", "i"
           "string", "s"
           "bool", "b"
           "byte", "b"
           "int16", "s"
           "uint32", "u"
           "double", "d"
           "single", "s"
           "long", "l"
           "namespace", "ns"
           "list", "l" |]
        |> dict

    let pipeRightOperatorNames =
        [| "|>"; "||>"; "|||>" |] |> HashSet

    let pipeLeftOperatorNames =
        [| "<|"; "<||"; "<|||" |] |> HashSet
    
    let withWords words (nameRoot: NameRoot) =
        NameRoot(Array.ofList words, nameRoot.PluralityKind, nameRoot.IsFinalPresentation)

    let withSuffix (AsList suffix) (nameRoot: NameRoot) =
        let words = List.ofSeq nameRoot.Words
        let words = words @ suffix
        withWords words nameRoot

    let (|Word|_|) word (nameElement: NameInnerElement) =
        match nameElement.As<NameWord>() with
        | null -> None
        | nameWord -> if nameWord.Text = word then someUnit else None

    let (|FSharpNameRoot|_|) (root: NameRoot) =
        match List.ofSeq root.Words with
        | Word "F" :: Word "Sharp" :: rest
        | Word "I" :: Word "F" :: Word "Sharp" :: rest -> Some (withWords rest root)
        | _ -> None

    let dropFSharpWords root =
        match root with
        | FSharpNameRoot root -> root
        | _ -> root

    let isFSharpTypeLike (element: IDeclaredElement) =
        element :? ITypeElement && startsWith "FSharp" element.ShortName

    let addSingleParamSuggestions =
        [| "Some", fsOptionTypeName
           "ValueSome", fsValueOptionTypeName
           "Ok", fsResultTypeName |]
        |> dict

    override x.MangleNameIfNecessary(name, _) =
        Keywords.QuoteIdentifierIfNeeded name

    override x.SuggestRoots(typ: IType, policyProvider: INamingPolicyProvider) =
        let roots = base.SuggestRoots(typ, policyProvider)

        match typ.As<IDeclaredType>() with
        | null -> roots
        | declaredType ->

        let typeElement = declaredType.GetTypeElement()
        if not (isFSharpTypeLike typeElement) then roots else
        if typeElement.GetClrName().Equals(fsListTypeName) then roots else

        let typeParameters = typeElement.TypeParameters
        if typeParameters.IsEmpty() then roots else

        let psiServices = typeElement.GetPsiServices()
        match psiServices.Naming.Parsing.GetName(typeElement, "unknown", policyProvider).GetRoot() with
        | FSharpNameRoot root ->
            let typeArg = declaredType.GetSubstitution().[typeParameters.[0]]
            let typeArgRoots = x.SuggestRoots(typeArg, policyProvider) |> List.ofSeq
            let newRoots = typeArgRoots |> List.map (withSuffix root.Words)
            seq {
                yield! Seq.map dropFSharpWords roots
                yield! newRoots
            }

        | _ -> roots

    override x.SuggestRoots(element: IDeclaredElement, policyProvider: INamingPolicyProvider) =
        let roots = base.SuggestRoots(element, policyProvider)
        seq {
            if isFSharpTypeLike element then
                yield! Seq.map dropFSharpWords roots
            else
                yield! roots

            if element :? ISelfId then
                yield NameRoot([| NameWord("this", "this") |], PluralityKinds.Single, true)
        }

    override x.IsSameNestedNameAllowedForMembers = true

    member x.IsValidName(element: IDeclaredElement, name: string) =
        let isValidCaseStart char =
            // F# 4.1 spec: 8.5 Union Type Definitions
            Char.IsUpper(char) && not (Char.IsLower(char))

        let isTypeLike (element: IDeclaredElement) =
            element :? ITypeElement || element :? IUnionCase || element :? INamespace

        let isUnionCaseLike (element: IDeclaredElement) =
            match element with
            | :? IUnionCase
            | :? IActivePatternCase -> true
            | :? ITypeElement as typeElement -> typeElement.IsException()
            | _ -> false

        if name.IsEmpty() then false else
        if isUnionCaseLike element && not (isValidCaseStart name.[0]) then false else
        if isTypeLike element && name.IndexOfAny(notAllowedInTypes) <> -1 then false else

        not (startsWith "`" name || endsWith "`" name || name.ContainsNewLine() || name.Contains("``"))

    override x.SuggestRoots(treeNode: ITreeNode, useExpectedTypes, policyProvider) =
        match treeNode with
        | :? IParenExpr as parenExpr ->
            x.SuggestRoots(parenExpr.InnerExpression, useExpectedTypes, policyProvider)

        | :? ITypedExpr as typedExpr ->
            x.SuggestRoots(typedExpr.Expression, useExpectedTypes, policyProvider)
        
        | :? IReferenceExpr as referenceExpr ->
            match referenceExpr.Qualifier.IgnoreInnerParens() with
            | :? IReferenceExpr as qualifierExpr ->
                x.SuggestRoots(referenceExpr.Reference, qualifierExpr.Reference, policyProvider)
            | _ -> x.SuggestRoots(referenceExpr.Reference, null, policyProvider)

        | :? IIndexerExpr as dotIndexedGetExpr ->
            let roots = x.SuggestRoots(dotIndexedGetExpr.Qualifier, useExpectedTypes, policyProvider)
            FSharpNamingService.PluralToSingle(roots)

        | :? ICastExpr as castExpr ->
            // todo: suggest type
            x.SuggestRoots(castExpr.Expression, useExpectedTypes, policyProvider)

        | :? IIfThenElseExpr as ifThenElseExpr ->
            let thenRoots = x.SuggestRoots(ifThenElseExpr.ThenExpr, useExpectedTypes, policyProvider)
            let elseRoots = x.SuggestRoots(ifThenElseExpr.ElseExpr, useExpectedTypes, policyProvider)
            Seq.append thenRoots elseRoots

        | :? IBinaryAppExpr as binaryApp ->
            let refExpr = binaryApp.Operator
            if isNull refExpr then EmptyList.Instance :> _ else

            let name = refExpr.Reference.GetName()
            if pipeRightOperatorNames.Contains(name) && isNotNull binaryApp.RightArgument then
                x.SuggestRoots(binaryApp.RightArgument, useExpectedTypes, policyProvider) else

            if pipeLeftOperatorNames.Contains(name) && isNotNull binaryApp.LeftArgument then
                x.SuggestRoots(binaryApp.LeftArgument, useExpectedTypes, policyProvider) else

            EmptyList.Instance :> _

        // todo: partially applied functions?
        | :? IPrefixAppExpr as appExpr ->
            let invokedFunctionReference = appExpr.InvokedFunctionReference
            if isNotNull invokedFunctionReference then
                x.SuggestRoots(invokedFunctionReference, null, policyProvider)

            else EmptyList.Instance :> _

//        | :? IExpression as expr ->
//            x.SuggestRoots(expr.Type(), policyProvider)

        | _ -> EmptyList.Instance :> _

    member x.AddExtraNames(namesCollection: INamesCollection, fsPattern: IFSharpPattern) =
        let pat, path = Traverse.makeTuplePatPath fsPattern

        let entryOptions =
            EntryOptions(subrootPolicy = SubrootPolicy.Decompose, emphasis = Emphasis.Good,
                         prefixPolicy = PredefinedPrefixPolicy.Remove)

        let addNamesForExpr expr =
            match Traverse.tryTraverseExprPath path expr with
            | null -> ()
            | expr -> namesCollection.Add(expr, entryOptions)

        match pat.Parent with
        | :? IBinding as binding when binding.HeadPattern == pat ->
            match binding.Expression with
            | null -> ()
            | expr -> addNamesForExpr expr

        | :? IMatchClause as matchClause when matchClause.Pattern == pat ->
            match MatchExprNavigator.GetByClause(matchClause) with
            | null -> ()
            | matchExpr ->

            match matchExpr.Expression with
            | null -> ()
            | expr -> addNamesForExpr expr

        | :? IForEachExpr as forEachExpr when forEachExpr.Pattern == pat ->
            let expr = forEachExpr.InExpression
            if expr :? IRangeSequenceExpr then () else

            let naming = pat.GetPsiServices().Naming
            let collection =
                naming.Suggestion.CreateEmptyCollection(
                    PluralityKinds.Plural, pat.Language, namesCollection.PolicyProvider)

            collection.Add(expr, entryOptions)
            for nameRoot in collection.GetRoots() do
                let single = NamingUtil.TryPluralToSingle(nameRoot)
                if isNotNull single then
                    namesCollection.Add(single, entryOptions)

        | _ -> ()

        match fsPattern with
        | :? INamedPat as namedPat ->
            let parametersOwner = ParametersOwnerPatNavigator.GetByParameter(namedPat.IgnoreParentParens())
            if isNull parametersOwner || parametersOwner.Parameters.Count <> 1 then () else

            let typeName = addSingleParamSuggestions.TryGetValue(parametersOwner.ReferenceName.ShortName)
            if isNull typeName then () else

            let reference = parametersOwner.ReferenceName.GetFirstClassReferences().FirstOrDefault()
            if isNull reference then () else

            let declaredElement = reference.Resolve().DeclaredElement.As<IAttributesOwner>()
            if isNull declaredElement || not (isCompiledUnionCase declaredElement) then () else
            if declaredElement.GetContainingType().GetClrName() <> typeName then () else 

            x.AddExtraNames(namesCollection, parametersOwner)            
        | _ -> ()

    override x.GetAbbreviation(root) =
        if root.Words.Count <> 1 then null else

        let mutable value = Unchecked.defaultof<_>
        if not (abbreviationsMap.TryGetValue(root.FirstWord.Text.ToLower(), &value)) then null else

        NameRoot.FromWords(root.Emphasis, false, value)

    override x.GetNamedElementKind(element) =
        let field = element.As<IField>()
        if isNotNull field && field.IsConstant then base.GetNamedElementKind(element) else

        let declarations = element.GetDeclarations()
        if declarations |> Seq.exists (fun decl -> decl :? IFSharpPattern) then
            NamedElementKinds.Locals
        else
            base.GetNamedElementKind(element)

module FSharpNamingService =
    let getUsedNamesUsages
            (contextExpr: IFSharpExpression) (usages: IList<ITreeNode>) (containingTypeElement: ITypeElement)
            checkFcsSymbols =

        let usages = HashSet(usages)
        let usedNames = OneToListMap<string, ITreeNode>()

        let addUsedNames names =
            for name in names do
                usedNames.Add(name, null)

        // Type element is not null when checking names for declaration in a module/class.
        // Consider all member names used.
        // todo: type private let binding allow shadowing 
        if isNotNull containingTypeElement then
            addUsedNames containingTypeElement.MemberNames

        let scopes = Stack()
        let scopedNames = Dictionary<string, int>()

        let addScopeForPatterns (patterns: IFSharpPattern seq) (scopeExpr: IFSharpExpression) =
            if isNull scopeExpr || Seq.isEmpty patterns then () else 

            let newNames = List()
            for fsPattern in patterns do
                for decl in fsPattern.Declarations do
                    if isNull decl.DeclaredElement then () else

                    let name = decl.DeclaredName
                    if name <> SharedImplUtil.MISSING_DECLARATION_NAME then
                        newNames.Add(name)

            if not (newNames.IsEmpty()) then
                scopes.Push({| Expr = scopeExpr; Names = newNames :> IList<_> |})

        let processor =
            { new IRecursiveElementProcessor with
                member x.ProcessingIsFinished = false

                member x.InteriorShouldBeProcessed(treeNode) =
                    not (usages.Contains(treeNode))

                member x.ProcessBeforeInterior(treeNode) =
                    if not (scopes.IsEmpty()) then
                        let scope = scopes.Peek()
                        let fsExpr = treeNode.As<IFSharpExpression>()
                        if scope.Expr == fsExpr then
                            for name in scope.Names do
                                scopedNames.[name] <-
                                    let mutable count = Unchecked.defaultof<_>
                                    if scopedNames.TryGetValue(name, &count) then count + 1 else 1

                    match treeNode with
                    | :? IReferenceExpr as refExpr ->
                        if usages.Contains(refExpr) then
                            // Scoped names can't be used when their scope contains usage expressions
                            // in Introduce Var since the introduced name would get shadowed.
                            addUsedNames scopedNames.Keys
                            scopedNames.Clear() else

                        if isNotNull refExpr.Qualifier then () else

                        let name = refExpr.ShortName
                        if name = SharedImplUtil.MISSING_DECLARATION_NAME ||
                                scopedNames.ContainsKey(name) || usedNames.ContainsKey(name) then () else

                        if not checkFcsSymbols || refExpr.Reference.HasFcsSymbol then
                            usedNames.Add(name, refExpr)

                    | :? ILetOrUseExpr as letExpr ->
                        let patterns = letExpr.BindingsEnumerable |> Seq.map (fun b -> b.HeadPattern)
                        addScopeForPatterns patterns letExpr.InExpression

                    | :? IBinding as binding ->
                        let headPattern = binding.HeadPattern
                        if isNull headPattern then () else

                        let bindingExpression = binding.Expression
                        if isNull bindingExpression then () else

                        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
                        if isNull letExpr then () else

                        let patterns =
                            let parameters = binding.ParametersPatternsEnumerable
                            if parameters.IsEmpty() then [| binding.HeadPattern |] :> IFSharpPattern seq else

                            let parameters = parameters |> Seq.map (fun paramDecl -> paramDecl.Pattern)
                            if letExpr.IsRecursive then
                                Seq.append [| headPattern |] parameters
                            else
                                parameters

                        addScopeForPatterns patterns bindingExpression

                    | :? ILambdaExpr as lambdaExpr ->
                        addScopeForPatterns lambdaExpr.PatternsEnumerable lambdaExpr.Expression

                    | :? IMatchClause as matchClause ->
                        addScopeForPatterns [| matchClause.Pattern |] matchClause.Expression

                    | :? IForEachExpr as forEachExpr ->
                        addScopeForPatterns [| forEachExpr.Pattern |] forEachExpr.DoExpression

                    | :? IForExpr as forExpr ->
                        if isNull forExpr.DoExpression then () else

                        let name = forExpr.Identifier.GetSourceName()
                        if name <> SharedImplUtil.MISSING_DECLARATION_NAME then
                            scopes.Push({| Expr = forExpr.DoExpression; Names = [| name |] |})

                    | _ -> ()

                member x.ProcessAfterInterior(treeNode) =
                    let fsExpr = treeNode.As<IFSharpExpression>()
                    if scopes.IsEmpty() then () else

                    let scope = scopes.Peek()
                    if scope.Expr == fsExpr then
                        for name in scope.Names do
                            let mutable count = Unchecked.defaultof<_>
                            if scopedNames.TryGetValue(name, &count) then
                                match count with
                                | 1 -> scopedNames.Remove(name) |> ignore
                                | count -> scopedNames.[name] <- count - 1

                        scopes.Pop() |> ignore
            }

        match SequentialExprNavigator.GetByExpression(contextExpr) with
        | null -> contextExpr.ProcessThisAndDescendants(processor)
        | seqExpr ->
            seqExpr.ExpressionsEnumerable
            |> Seq.skipWhile ((!=) contextExpr)
            |> Seq.iter (fun expr -> expr.ProcessThisAndDescendants(processor))

        usedNames

    let getUsedNames contextExpr usages containingTypeElement checkFcsSymbols: ISet<string> =
        let usedNames = getUsedNamesUsages contextExpr usages containingTypeElement checkFcsSymbols
        HashSet(usedNames.Keys) :> _

    let createEmptyNamesCollection (fsTreeNode: IFSharpTreeNode) =
        let sourceFile = fsTreeNode.GetSourceFile()
        let namingManager = sourceFile.GetSolution().GetPsiServices().Naming
        namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, fsTreeNode.Language, true, sourceFile)

    let getEntryOptions () =
        EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, PredefinedPrefixPolicy.Remove)

    let addNamesForType (t: IType) (namesCollection: INamesCollection) =
        namesCollection.Add(t, getEntryOptions ())
        namesCollection

    let addNamesForExpression (expr: IFSharpExpression) (namesCollection: INamesCollection) =
        namesCollection.Add(expr, getEntryOptions ())
        namesCollection

    let prepareNamesCollection
            (usedNames: ISet<string>) (fsTreeNode: IFSharpTreeNode) (namesCollection: INamesCollection) =

        let sourceFile = fsTreeNode.GetSourceFile()
        let namingManager = namesCollection.Solution.GetPsiServices().Naming

        let settingsStore = fsTreeNode.GetSettingsStoreWithEditorConfig()
        let elementKind = NamedElementKinds.Locals
        let descriptor = ElementKindOfElementType.LOCAL_VARIABLE
        let namingRule =
            namingManager.Policy.GetDefaultRule(sourceFile, fsTreeNode.Language, settingsStore, elementKind, descriptor)

        let usedNamesFilter = Func<_,_>(usedNames.Contains >> not)
        let suggestionOptions = SuggestionOptions(null, DefaultName = "foo", UsedNamesFilter = usedNamesFilter)
        namesCollection.Prepare(namingRule, ScopeKind.Common, suggestionOptions).AllNames()
