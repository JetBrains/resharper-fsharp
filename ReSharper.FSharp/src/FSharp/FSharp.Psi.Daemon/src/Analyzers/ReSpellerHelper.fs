namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open System.Collections.Generic
open JetBrains.ReSharper.Features.ReSpeller.Analyzers
open JetBrains.ReSharper.Features.ReSpeller.Analyzers.TextModificationContext
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type ReSpellerPsiHelper() =
    inherit PsiHelperBase()

    let isStringConcatenation (binaryApp: IBinaryAppExpr) =
        isNotNull binaryApp && binaryApp.ShortName = "+"

    let rec getOutermostStringConcatenation (expr: IFSharpExpression) =
        let expr = expr.IgnoreParentParens()
        let binaryApp = BinaryAppExprNavigator.GetByArgument(expr)
        if isStringConcatenation binaryApp then getOutermostStringConcatenation binaryApp else expr

    let rec tryCollectStringTokens (stringTokens: List<ITreeNode>) (expr: IFSharpExpression) =
        match expr.IgnoreInnerParens() with
        | :? ILiteralExpr as literalExpr when (literalExpr.Literal :? FSharpString) ->
            stringTokens.Add(literalExpr.Literal)
            true

        | :? IBinaryAppExpr as binaryApp when isStringConcatenation binaryApp ->
            tryCollectStringTokens stringTokens binaryApp.LeftArgument &&
            tryCollectStringTokens stringTokens binaryApp.RightArgument

        | _ -> false

    override x.ShouldSkipDeclaration(declaration) =
        match declaration with
        | :? IFSharpPattern as fsPattern -> not fsPattern.IsDeclaration
        | :? IFSharpDeclaration as fsDeclaration -> fsDeclaration.NameIdentifier :? IActivePatternId
        | _ -> true

    override x.GetDeclarationOnIdentifier(node) =
        match node with
        | :? IFSharpIdentifier as fsIdentifier -> ReferencePatNavigator.GetByIdentifier(fsIdentifier) :> _
        | _ -> null

    override x.ShouldSkipInheritedMember(node, _) =
        let memberDeclaration = node.As<IMemberDeclaration>()
        if isNull memberDeclaration then false else

        memberDeclaration.IsOverride ||
        isNotNull (InterfaceImplementationNavigator.GetByTypeMember(memberDeclaration))

    override x.TryAppendLiteral(textToCheck, node) =
        match node with
        | :? FSharpString as stringToken ->
            let contentRangeTranslator = RangeTranslator()
            let value = stringToken.GetStringValue(contentRangeTranslator)
            let struct (startBorderLength, _) = stringToken.GetBorderLengths()
            let context = DefaultTextModificationContext(stringToken)
            textToCheck.AppendTextWithRangeTranslation(stringToken, value, contentRangeTranslator, startBorderLength,
                context)
            true

        | _ -> base.TryAppendLiteral(textToCheck, node)

    override x.GetConcatenatedLiterals(literal) =
        let literalExpr = literal.Parent.As<ILiteralExpr>()
        if isNull literalExpr then base.GetConcatenatedLiterals(literal) else

        let stringTokens = List<ITreeNode>()
        let concatenation = getOutermostStringConcatenation literalExpr
        let hasOnlyStringLiterals = tryCollectStringTokens stringTokens concatenation
        if not hasOnlyStringLiterals then base.GetConcatenatedLiterals(literal) else

        stringTokens.ToArray()

    override this.GetNamespaceIdentifiers(identifier, declaration) =
        let decl = declaration.As<INamedNamespaceDeclaration>()
        if isNull decl then base.GetNamespaceIdentifiers(identifier, declaration) else

        let rec loop acc (referenceName: IReferenceName) =
            if isNull referenceName then List.rev acc else

            let acc = struct (referenceName.Identifier :> ITreeNode, referenceName.ShortName) :: acc
            loop acc referenceName.Qualifier

        loop [identifier, identifier.Name] decl.QualifierReferenceName |> Array.ofList
