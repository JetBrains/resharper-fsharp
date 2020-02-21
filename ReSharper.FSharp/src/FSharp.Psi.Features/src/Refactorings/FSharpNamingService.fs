namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System
open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Naming.Extentions
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.ReSharper.Psi.Naming.Settings
open JetBrains.Util

[<AutoOpen>]
module Traverse =
    type TraverseStep =
        | TupleItem of item: int

    let makeTuplePatPath pat =
        let rec tryMakePatPath path (IgnoreParenPat pat: ISynPat) =
            match pat.Parent with
            | :? ITuplePat as tuplePat ->
                let item = tuplePat.Patterns.IndexOf(pat)
                Assertion.Assert(item <> -1, "item <> -1")
                tryMakePatPath (TupleItem(item) :: path) tuplePat
            | _ -> pat, path

        tryMakePatPath [] pat

    let rec tryTraverseExprPath (path: TraverseStep list) (IgnoreInnerParenExpr expr: ISynExpr) =
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

    member x.AddExtraNames(namesCollection: INamesCollection, declaredElementPat: ISynPat) =
        let pat, path = makeTuplePatPath declaredElementPat

        let entryOptions =
            EntryOptions(subrootPolicy = SubrootPolicy.Decompose, emphasis = Emphasis.Good,
                         prefixPolicy = PredefinedPrefixPolicy.Remove)

        let addNamesForExpr expr =
            match tryTraverseExprPath path expr with
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

        | :? ILetOrUseBangExpr as letOrUseBangExpr when letOrUseBangExpr.Binding.HeadPattern == pat ->
            match letOrUseBangExpr.Binding.Expression with
            | null -> ()
            | expr -> addNamesForExpr expr

        | :? IForEachExpr as forEachExpr when forEachExpr.Pattern == pat ->
            match forEachExpr.InClause.As<ISynExpr>() with
            | null -> ()
            | expr ->

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

        match declaredElementPat with
        | :? INamedPat as namedPat ->
            let parametersOwner = ParametersOwnerPatNavigator.GetByParameter(namedPat.IgnoreParentParens())
            if isNull parametersOwner || parametersOwner.Parameters.Count <> 1 then () else

            let typeName = addSingleParamSuggestions.TryGetValue(parametersOwner.SourceName)
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
        if declarations |> Seq.exists (fun decl -> decl :? ISynPat) then
            NamedElementKinds.Locals
        else
            base.GetNamedElementKind(element)
