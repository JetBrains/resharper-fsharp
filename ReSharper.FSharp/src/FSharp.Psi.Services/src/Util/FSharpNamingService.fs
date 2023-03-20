namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Naming
open JetBrains.ReSharper.Psi.Naming.Elements
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Naming.Extentions
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.Util


module FSharpNamingService =
    let getUsedNamesUsages
            (contextExprs: IFSharpExpression list) (usages: IList<ITreeNode>) (containingTypeElement: ITypeElement)
            checkFcsSymbols =

        let usages = HashSet(usages)
        let usedNames = OneToListMap<string, ITreeNode>()

        let addUsedNames (names: string seq) =
            for name in names do
                usedNames.Add(name.RemoveBackticks(), null)

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
                                scopedNames[name] <-
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
                            usedNames.Add(name.RemoveBackticks(), refExpr)

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
                            let parameters = binding.ParametersDeclarationsEnumerable
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

                        let name = forExpr.Identifier.SourceName
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
                                | count -> scopedNames[name] <- count - 1

                        scopes.Pop() |> ignore
            }

        let processExpr expr =
            match SequentialExprNavigator.GetByExpression(expr) with
            | null ->
                match expr with
                | null -> ()
                | expr -> expr.ProcessThisAndDescendants(processor)
            | seqExpr ->
                seqExpr.ExpressionsEnumerable
                |> Seq.skipWhile ((!=) expr)
                |> Seq.iter (fun expr -> expr.ProcessThisAndDescendants(processor))

        List.iter processExpr contextExprs

        usedNames

    let getContainingType (moduleMember: IModuleMember) =
        if isNull moduleMember then null else

        let typeDeclaration = moduleMember.GetContainingTypeDeclaration()
        if isNull typeDeclaration then null else typeDeclaration.DeclaredElement

    let getPatternContainingType (pattern: IFSharpPattern) =
        let binding = BindingNavigator.GetByHeadPattern(pattern)
        let letDecl = LetBindingsDeclarationNavigator.GetByBinding(binding)
        getContainingType letDecl

    let getPatternsNames (skipPattern: IFSharpPattern) (pats: IEnumerable<IFSharpPattern>) =
        let pats =
            if isNotNull skipPattern then
                pats |> Seq.filter ((!=) skipPattern)
            else
                pats 

        pats
        |> Seq.collect (fun pat -> pat.NestedPatterns)
        |> Seq.choose (fun pat ->
            match pat with
            | :? IReferencePat as refPat -> Some(refPat.SourceName)
            | _ -> None)

    // todo: use in Rename
    let getPatternContextUsedNames (contextPattern: IFSharpPattern) =
        let names = HashSet()

        let rec loop (pat: IFSharpPattern) =
            if isNull pat then () else

            let addNames (pats: IEnumerable<IFSharpPattern>) =
                getPatternsNames pat pats |> names.AddRange
            
            match pat.Parent with
            | :? ITuplePat as tuplePat -> addNames tuplePat.PatternsEnumerable
            | :? IAndsPat as andsPat -> addNames andsPat.PatternsEnumerable
            | :? IArrayOrListPat as asPat -> addNames asPat.PatternsEnumerable
            | :? IRecordPat as recordPat -> addNames (Seq.cast recordPat.FieldPatternsEnumerable)
            | :? IListConsPat as listConsPat -> addNames [listConsPat.HeadPattern; listConsPat.TailPattern]
            | :? IAsPat as asPat -> addNames [asPat.LeftPattern; asPat.RightPattern]
            | :? IParametersOwnerPat as parametersOwnerPat -> addNames parametersOwnerPat.ParametersEnumerable

            | :? IReferencePat as refPat ->
                names.Add(refPat.SourceName) |> ignore
                loop refPat

            | :? IFSharpPattern as fsPat -> loop fsPat
            | _ -> ()

        loop contextPattern
        names

    let getUsedNames (contextExprs: IFSharpExpression list) usages containingTypeElement checkFcsSymbols: ISet<string> =
        let usedNames = getUsedNamesUsages contextExprs usages containingTypeElement checkFcsSymbols
        HashSet(usedNames.Keys) :> _

    let createEmptyNamesCollection (context: ITreeNode) =
        let sourceFile = context.GetSourceFile()
        let namingManager = sourceFile.GetSolution().GetPsiServices().Naming
        namingManager.Suggestion.CreateEmptyCollection(PluralityKinds.Unknown, context.Language, true, sourceFile)

    let getEntryOptions () =
        EntryOptions(PluralityKinds.Unknown, SubrootPolicy.Decompose, PredefinedPrefixPolicy.Remove)

    let addNames (name: string) (context: ITreeNode) (namesCollection: INamesCollection) =
        if isNotNull name then
            let namingRule = NamingRule(NamingStyleKind = NamingStyleKinds.aaBb)
            let nameParser = context.GetPsiServices().Naming.Parsing
            let root = nameParser.Parse(name, namingRule, context.Language, context.GetSourceFile()).GetRoot()
            namesCollection.Add(root, getEntryOptions ())
        namesCollection

    let addNamesForType (t: IType) (namesCollection: INamesCollection) =
        namesCollection.Add(t, getEntryOptions ())
        namesCollection

    let addNamesForExpression (overrideType: IType option) (expr: IFSharpExpression) (namesCollection: INamesCollection) =
        let options = getEntryOptions ()
        match overrideType with
        | None -> namesCollection.Add(expr, options)
        | Some overrideType ->
            let namingService = NamingManager.GetNamingLanguageService(expr.Language).As<ClrNamingLanguageServiceBase>()
            let provider = namesCollection.PolicyProvider

            (namingService.SuggestRoots(expr, false, provider), namingService.SuggestRoots(overrideType, provider))
            ||> Seq.append
            |> Seq.iter (fun root -> namesCollection.Add(root, options))
        namesCollection

    let prepareNamesCollection (usedNames: ISet<string>) (context: ITreeNode) (namesCollection: INamesCollection) =
        let sourceFile = context.GetSourceFile()
        let namingManager = namesCollection.Solution.GetPsiServices().Naming

        let settingsStore = context.GetSettingsStoreWithEditorConfig()
        let elementKind = NamedElementKinds.Locals
        let descriptor = ElementKindOfElementType.LOCAL_VARIABLE
        let namingRule =
            namingManager.Policy.GetDefaultRule(sourceFile, context.Language, settingsStore, elementKind, descriptor)

        let usedNamesFilter = Func<_,_>(usedNames.Contains >> not)
        let suggestionOptions = SuggestionOptions(null, DefaultName = "foo", UsedNamesFilter = usedNamesFilter)
        namesCollection.Prepare(namingRule, ScopeKind.Common, suggestionOptions).AllNames()

    let mangleNameIfNecessary name =
        match name with
        | "``sig``" -> name
        | "sig" -> "``sig``"
        | _ -> PrettyNaming.NormalizeIdentifierBackticks name

[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit ClrNamingLanguageServiceBase(language, EmptyArray.Instance)

    let notAllowedInTypes =
        // F# 4.1 spec: 3.4 Identifiers and Keywords
        [| '.'; '+'; '$'; '&'; '['; ']'; '/'; '\\'; '*'; '\"'; '`' |]

    let abbreviationsMap =
        [| "object", "o"
           "char", "c"
           "int32", "i"
           "string", "s"
           "boolean", "b"
           "byte", "b"
           "int16", "s"
           "uint32", "u"
           "double", "f"
           "single", "f"
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
        FSharpNamingService.mangleNameIfNecessary name

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
            let typeArg = declaredType.GetSubstitution().[typeParameters[0]]
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
        let isUppercase char =
            // F# 4.1 spec: 8.5 Union Type Definitions
            Char.IsUpper(char) && not (Char.IsLower(char))

        let isTypeLike (element: IDeclaredElement) =
            element :? ITypeElement || element :? IUnionCase || element :? INamespace

        let requiresUppercaseStart (element: IDeclaredElement) =
            match element with
            | :? IUnionCase as uc ->
                FSharpLanguageLevel.ofPsiModuleNoCache uc.Module < FSharpLanguageLevel.FSharp70 ||
                not (hasRequireQualifiedAccessAttribute uc.ContainingType)

            | :? IActivePatternCase -> true
            | :? ITypeElement as typeElement -> typeElement.IsException()
            | _ -> false

        let name = name.RemoveBackticks()

        if name.IsEmpty() then false else
        if requiresUppercaseStart element && not (isUppercase name[0]) then false else
        if isTypeLike element && name.IndexOfAny(notAllowedInTypes) <> -1 then false else

        not (name.ContainsNewLine() || name.Contains("``") || endsWith "`" name)

    override x.SuggestRoots(treeNode: ITreeNode, useExpectedTypes, policyProvider) =
        match treeNode with
        | :? IParenOrBeginEndExpr as parenExpr ->
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
            let name = binaryApp.ShortName
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
        let pat, path = FSharpPatternUtil.ParentTraversal.makeTuplePatPath fsPattern

        let entryOptions =
            EntryOptions(subrootPolicy = SubrootPolicy.Decompose, emphasis = Emphasis.Good,
                         prefixPolicy = PredefinedPrefixPolicy.Remove)

        let addNamesForExpr expr =
            match FSharpPatternUtil.ParentTraversal.tryTraverseExprPath path expr with
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
            if expr :? IRangeLikeExpr then () else

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

        let getParameterOwnerPat (pattern: IFSharpPattern) =
            let pattern = FSharpPatternUtil.ignoreParentAsPatsFromRight pattern
            let parametersOwner = ParametersOwnerPatNavigator.GetByParameter(pattern.IgnoreParentParens())
            if isNotNull parametersOwner then parametersOwner else

            let tuplePat = TuplePatNavigator.GetByPattern(fsPattern.IgnoreParentParens())
            ParametersOwnerPatNavigator.GetByParameter(tuplePat.IgnoreParentParens())

        let getUnionCaseFieldIndex (pattern: IFSharpPattern) (parametersOwnerPat: IParametersOwnerPat) =
            let pattern = FSharpPatternUtil.ignoreParentAsPatsFromRight pattern
            let pattern = pattern.IgnoreParentParens()

            let singleParam = parametersOwnerPat.ParametersEnumerable.SingleItem
            if singleParam == pattern then 0 else

            let tuplePat = singleParam.IgnoreInnerParens().As<ITuplePat>()
            if isNull tuplePat then -1 else

            tuplePat.Patterns.IndexOf(pattern)

        let addUnionCaseFieldName () =
            let parametersOwner = getParameterOwnerPat fsPattern
            if isNull parametersOwner then () else

            let referenceName = parametersOwner.ReferenceName
            if isNull referenceName then () else

            let reference = referenceName.Reference
            if isNull reference then () else

            match reference.GetFcsSymbol() with
            | :? FSharpUnionCase as fcsUnionCase ->
                let indexOf = getUnionCaseFieldIndex fsPattern parametersOwner
                let fcsFields = fcsUnionCase.Fields

                let isSingle = fcsFields.Count = 1
                let defaultItemName = if isSingle then "Item" else $"Item{indexOf + 1}"

                if indexOf >= 0 && indexOf < fcsFields.Count then
                    let fcsField = fcsFields[indexOf]
                    let name = fcsField.Name
                    if name <> defaultItemName then
                        FSharpNamingService.addNames name fsPattern namesCollection |> ignore
            | _ -> ()

        addUnionCaseFieldName ()

        match fsPattern with
        | :? IReferencePat as refPat ->
            let pat = FSharpPatternUtil.ignoreParentAsPatsFromRight refPat
            let parametersOwner = ParametersOwnerPatNavigator.GetByParameter(pat.IgnoreParentParens())
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
