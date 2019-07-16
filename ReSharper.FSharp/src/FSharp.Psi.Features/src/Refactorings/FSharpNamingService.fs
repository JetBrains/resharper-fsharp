namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open System
open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Naming.Impl
open JetBrains.ReSharper.Psi.Naming.Interfaces
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpNamingService(language: FSharpLanguage) =
    inherit ClrNamingLanguageServiceBase(language)

    let notAllowedInTypes =
        // F# 4.1 spec: 3.4 Identifiers and Keywords
        [| '.'; '+'; '$'; '&'; '['; ']'; '/'; '\\'; '*'; '\"'; '`' |]

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
        
        | :? IIdentExpr as identExpr ->
            x.SuggestRoots(identExpr.IdentifierToken, useExpectedTypes, policyProvider)

        | :? ILongIdentExpr as longIdentExpr ->
            x.SuggestRoots(longIdentExpr.LongIdentifier, useExpectedTypes, policyProvider)

        | :? IDotGetExpr as dotGetExpr ->
            x.SuggestRoots(dotGetExpr.LongIdentifier, useExpectedTypes, policyProvider)

        | :? IDotIndexedGetExpr as dotIndexedGetExpr ->
            let roots = x.SuggestRoots(dotIndexedGetExpr.LeftExpr, useExpectedTypes, policyProvider)
            FSharpNamingService.PluralToSingle(roots)

        | :? ICastExpr as castExpr ->
            x.SuggestRoots(castExpr.Expression, useExpectedTypes, policyProvider)

        | :? IIfThenElseExpr as ifThenElseExpr ->
            let thenRoots = x.SuggestRoots(ifThenElseExpr.ThenExpr, useExpectedTypes, policyProvider)
            let elseRoots = x.SuggestRoots(ifThenElseExpr.ElseExpr, useExpectedTypes, policyProvider)
            Seq.append thenRoots elseRoots

        | :? FSharpIdentifierToken as idToken ->
            x.SuggestRoots(idToken.Reference, idToken.QualifierReference, policyProvider)

        | :? ILongIdentifier as longIdentifier ->
            x.SuggestRoots(longIdentifier.IdentifierToken, useExpectedTypes, policyProvider)

        | _ -> EmptyList.Instance :> _
