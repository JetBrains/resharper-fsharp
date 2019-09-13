namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System
open System.Collections.Generic
open FSharp.Compiler.Ast
open FSharp.Compiler.Range
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.TreeBuilder

[<AbstractClass>]
type FSharpTreeBuilderBase(lexer: ILexer, document: IDocument, lifetime: Lifetime, projectedOffset) =
    inherit TreeBuilderBase(lifetime, lexer)

    let lineOffsets =
        let lineCount = document.GetLineCount()
        Array.init (int lineCount) (fun line -> document.GetLineStartOffset(docLine line))

    let getLineOffset line =
        lineOffsets.[line - 1]

    new (sourceFile, lexer, lifetime) =
        FSharpTreeBuilderBase(sourceFile, lexer, lifetime, 0)

    abstract member CreateFSharpFile: unit -> IFSharpFile

    member x.GetOffset(pos: pos) = getLineOffset pos.Line + pos.Column
    member x.GetStartOffset(range: range) = getLineOffset range.StartLine + range.StartColumn
    member x.GetEndOffset(range: range) = getLineOffset range.EndLine + range.EndColumn
    member x.GetStartOffset(IdentRange range) = x.GetStartOffset(range)

    member x.Eof = x.Builder.Eof()
    member x.CurrentOffset = x.Builder.GetTokenOffset() + projectedOffset

    override x.SkipWhitespaces() = ()

    member x.AdvanceLexer() = x.Builder.AdvanceLexer() |> ignore
    
    member x.AdvanceToStart(range: range) =
        x.AdvanceToOffset(x.GetStartOffset(range))

    member x.AdvanceToEnd(range: range) =
        x.AdvanceToOffset(x.GetEndOffset(range))

    member x.AdvanceTo(pos: pos) =
        x.AdvanceToOffset(x.GetOffset(pos))

    member x.Mark(range: range) =
        x.AdvanceToStart(range)
        x.Mark()

    member x.Mark(pos: pos) =
        x.AdvanceTo(pos)
        x.Mark()
    
    member x.Mark(offset: int) =
        x.AdvanceToOffset(offset)
        x.Mark()
    
    member x.Done(range, mark, elementType) =
        x.AdvanceToEnd(range)
        x.Done(mark, elementType)

    member x.Done(range, mark, elementType, data) =
        x.AdvanceToEnd(range)
        x.Builder.Done(mark, elementType, data)

    member x.MarkAndDone(range: range, elementType) =
        let mark = x.Mark(range)
        x.Done(range, mark, elementType)

    member x.MarkToken(elementType) =
        let caseMark = x.Mark()
        x.AdvanceLexer()
        x.Done(caseMark, elementType)

    member x.MarkTokenOrRange(tokenType, range: range) =
        let rangeStart = x.GetStartOffset(range)
        x.AdvanceToTokenOrOffset(tokenType, rangeStart, range)
        x.Mark()

    member x.AdvanceToOffset(offset) =
//        Assertion.Assert(x.CurrentOffset <= offset, "currentOffset: {0}, maxOffset: {1}", x.CurrentOffset, offset)

        while x.CurrentOffset < offset && not x.Eof do
            x.AdvanceLexer()

    member x.AdvanceToTokenOrRangeStart(tokenType: TokenNodeType, range: range) =
        x.AdvanceToTokenOrOffset(tokenType, x.GetStartOffset(range), range)
    
    member x.AdvanceToTokenOrRangeEnd(tokenType: TokenNodeType, range: range) =
        x.AdvanceToTokenOrOffset(tokenType, x.GetEndOffset(range), range)

    member x.AdvanceToTokenOrOffset(tokenType: TokenNodeType, maxOffset: int, range: range) =
        Assertion.Assert(isNotNull tokenType, "isNotNull tokenType")

//        let offset = x.CurrentOffset
//        Assertion.Assert(offset <= maxOffset, "tokenType: {0}, currentOffset: {1}, maxOffset: {2}, outer range: {3}",
//                         tokenType, offset, maxOffset, range)

        while x.CurrentOffset < maxOffset && x.Builder.GetTokenType() != tokenType do
            x.AdvanceLexer()

    member x.ProcessLongIdentifier(lid: Ident list) =
        match lid with
        | [] -> ()
        | head :: _ ->

        let mark = x.Mark(head.idRange)
        let last = List.last lid
        x.Done(last.idRange, mark, ElementType.LONG_IDENTIFIER)

    member x.ProcessReferenceName(lid: Ident list) =
        let marks = Stack()

        for _ in lid do
            marks.Push(x.Mark())

        for IdentRange id in lid do
            x.Done(id, marks.Pop(), ElementType.EXPRESSION_REFERENCE_NAME)

    member x.ProcessNamedTypeReference(lid: Ident list) =
        x.ProcessNamedTypeReference(lid, [], None, None, false)
    
    member x.ProcessNamedTypeReference(lid: Ident list, typeArgs: SynType list, ltOption, gtOption, isPostfixApp) =
        // todo: revise checking empty lid/args?
        if not isPostfixApp && lid.IsEmpty || isPostfixApp && typeArgs.IsEmpty then () else

        let marks = Stack()

        let head = if isPostfixApp then typeArgs.Head.Range else lid.Head.idRange
        x.AdvanceToStart(head)

        for _ in lid do
            marks.Push(x.Mark())
            if isPostfixApp && marks.Count = 1 then
                x.ProcessTypeArgs(typeArgs, ltOption, gtOption, ElementType.POSTFIX_APP_TYPE_ARGUMENT_LIST)

        for IdentRange id in lid do
            if not isPostfixApp && marks.Count = 1 then
                x.ProcessTypeArgs(typeArgs, ltOption, gtOption, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
            x.Done(id, marks.Pop(), ElementType.TYPE_REFERENCE_NAME)

    member x.GetTreeNode() =
        x.GetTree() :> ITreeNode
    
    member x.FinishFile(mark, fileType) =
        while not x.Eof do
            x.AdvanceLexer()

        x.Done(mark, fileType)
        x.GetTreeNode() :?> IFSharpFile

    member x.StartTopLevelDeclaration(lid: LongIdent, attrs: SynAttributes, moduleKind, range) =
        match lid with
        | IdentRange idRange as id :: _ ->
            let mark =
                match moduleKind with
                | AnonModule ->
                    x.Mark()

                | _ when attrs.IsEmpty ->
                    // Ast namespace range starts after its identifier,
                    // we try to locate the keyword followed by access modifiers.
                    let keywordTokenType =
                        match moduleKind with
                        | NamedModule -> FSharpTokenType.MODULE
                        | DeclaredNamespace -> FSharpTokenType.NAMESPACE
                        | _ -> null
                    let startOffset = x.GetStartOffset(idRange)
                    x.AdvanceToTokenOrOffset(keywordTokenType, startOffset, range)
                    x.Mark()

                | _ ->
                    x.MarkAttributesOrIdOrRange(attrs, Some id, range)

            if moduleKind = NamedModule then
                x.ProcessModifiersBeforeOffset(x.GetStartOffset(idRange))

            if moduleKind <> AnonModule then
                x.ProcessLongIdentifier(lid)

            let elementType =
                match moduleKind with
                | NamedModule -> ElementType.NAMED_MODULE_DECLARATION
                | AnonModule -> ElementType.ANON_MODULE_DECLARATION
                | _ -> ElementType.NAMED_NAMESPACE_DECLARATION

            Some mark, elementType

        | _ ->

        match moduleKind with
        | GlobalNamespace ->
            let mark = x.Mark(range)
            Some mark, ElementType.GLOBAL_NAMESPACE_DECLARATION
        | _ -> None, null

    member x.FinishTopLevelDeclaration(mark: int option, range, elementType) =
        x.AdvanceToEnd(range)
        if mark.IsSome then
            x.Done(mark.Value, elementType)

    member x.MarkAttributesOrIdOrRange(attrs: SynAttributes, id: Ident option, range: range) =
        match attrs with
        | head :: _ ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, head.Range)
            x.ProcessAttributes(attrs)
            mark

        | _ ->
            let rangeStart = x.GetStartOffset(range)
            let startOffset = if id.IsSome then Math.Min(x.GetStartOffset id.Value.idRange, rangeStart) else rangeStart
            x.Mark(startOffset)

    member x.StartNestedModule (attrs: SynAttributes) (lid: LongIdent) (range: range) =
        let mark = x.MarkAttributesOrIdOrRange(attrs, List.tryHead lid, range)
        if not lid.IsEmpty then
            x.ProcessModifiersBeforeOffset(x.GetStartOffset(lid.Head))
        mark

    member x.StartException(SynExceptionDefnRepr(_, UnionCase(_, id, unionCaseType, _, _, _), _, _, _, range)) =
        let mark = x.Mark(range)
        x.ProcessModifiersBeforeOffset(x.GetStartOffset id)
        x.ProcessUnionCaseType(unionCaseType, ElementType.EXCEPTION_FIELD_DECLARATION) |> ignore
        mark

    member x.ProcessModifiersBeforeOffset(endOffset: int) =
        let mark = x.Mark()
        x.AdvanceToOffset(endOffset)
        x.Done(mark, ElementType.ACCESS_MODIFIERS)

    member x.StartType attrs typeParams (lid: LongIdent) range =
        let mark = x.MarkAttributesOrIdOrRange(attrs, List.tryHead lid, range)
        if not lid.IsEmpty then
            let id = lid.Head
            let idOffset = x.GetStartOffset id

            let typeParamsOffset =
                match typeParams with
                | TyparDecl(_, (Typar(id, _, _))) :: _ -> x.GetStartOffset id
                | [] -> idOffset

            x.ProcessModifiersBeforeOffset (min idOffset typeParamsOffset)

            let paramsInBraces = idOffset < typeParamsOffset
            x.ProcessTypeParametersOfType typeParams range paramsInBraces

            // Needs to advance past id range due to implicit ctor range in class includes id.
            x.AdvanceToEnd(id.idRange)
        mark

    member x.ProcessTypeParametersOfType typeParams range paramsInBraces =
        match typeParams with
        | TyparDecl(_, (Typar(IdentRange idRange, _, _))) :: _ ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.LESS, idRange)
            for p in typeParams do
                x.ProcessTypeParameter(p, ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION)
            if paramsInBraces then
                let endOffset = x.GetEndOffset(range)
                x.AdvanceToTokenOrOffset(FSharpTokenType.GREATER, endOffset, range)
                if x.Builder.GetTokenType() == FSharpTokenType.GREATER then
                    x.AdvanceLexer()
            x.Done(mark, ElementType.TYPE_PARAMETER_OF_TYPE_LIST)
        | [] -> ()

    member x.ProcessTypeParameter(TyparDecl(_, (Typar(IdentRange range, _, _))), elementType) =
        x.MarkAndDone(range, elementType)

    member x.ProcessUnionCaseType(caseType, fieldElementType) =
        match caseType with
        | UnionCaseFields(fields) ->
            for f in fields do x.ProcessField f fieldElementType
            not fields.IsEmpty

        | UnionCaseFullType(_) ->
            true // todo: used in FSharp.Core only, otherwise warning

    member x.ProcessUnionCases(cases, range: range) =
        let casesListMark = x.Mark(range)
        for case in cases do
            x.ProcessUnionCase(case)
        x.Done(range, casesListMark, ElementType.UNION_CASES_LIST)

    member x.ProcessUnionCase(UnionCase(attrs, _, caseType, _, _, range)) =
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)
        x.ProcessAttributes(attrs)
        let hasFields = x.ProcessUnionCaseType(caseType, ElementType.UNION_CASE_FIELD_DECLARATION)
        let elementType = if hasFields then ElementType.NESTED_TYPE_UNION_CASE_DECLARATION
                                       else ElementType.SINGLETON_CASE_DECLARATION
        x.Done(range, mark, elementType)

    member x.ProcessAttributes(attrs) =
        for attr in attrs do
            x.ProcessAttribute(attr)

    member x.ProcessAttribute(attr: SynAttribute) =
        let mark =
            match attr.Target with
            | Some (IdentRange targetRange) ->
                let attrMark = x.Mark(targetRange)
                let targetMark = x.Mark()

                x.AdvanceToTokenOrRangeStart(FSharpTokenType.COLON, attr.Range)
                x.Done(targetRange, targetMark, ElementType.ATTRIBUTE_TARGET)
                attrMark

            | _ -> x.Mark(attr.Range)

        let lidWithDots = attr.TypeName
        x.ProcessNamedTypeReference(lidWithDots.Lid)

        let (ExprRange argRange as argExpr) = attr.ArgExpr
        if lidWithDots.Range <> argRange then
            // Arg range is the same when fake SynExpr.Const is added
            x.MarkChameleonExpression(argExpr)


        x.Done(attr.Range, mark, ElementType.F_SHARP_ATTRIBUTE)

    member x.ProcessEnumCase(EnumCase(_, _, _, _, range)) =
        x.MarkAndDone(range, ElementType.ENUM_MEMBER_DECLARATION)

    member x.ProcessField(Field(_, _, id, synType, _, _, _, range)) elementType =
        let mark =
            match id with
            | Some id ->
                x.AdvanceToOffset(min (x.GetStartOffset id) (x.GetStartOffset range))
                x.Mark()
            | None ->
                x.Mark(range)

        x.ProcessType(synType)
        x.Done(range, mark, elementType)

    member x.ProcessLocalId(IdentRange range) =
        x.MarkAndDone(range, ElementType.LOCAL_DECLARATION)

    member x.ProcessSimplePattern(pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id, _, isCompilerGenerated, _, _, _) ->
            if not isCompilerGenerated then
                x.ProcessLocalId(id)

        | SynSimplePat.Typed(SynSimplePat.Id(id, _, isCompilerGenerated, _, _, _), synType, _) ->
            if not isCompilerGenerated then
                x.ProcessLocalId(id)
            x.ProcessType(synType)

        | _ -> ()

    member x.ProcessImplicitCtorParam(pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id, _, _, _, _, range) ->
            let mark = x.Mark(range)
            x.ProcessLocalId(id)
            x.Done(range, mark,ElementType.MEMBER_PARAM)

        | SynSimplePat.Typed(SynSimplePat.Id(id, _, _, _, _, _), synType, range) ->
            let mark = x.Mark(range)
            x.ProcessLocalId(id)
            x.ProcessType(synType)
            x.Done(range, mark,ElementType.MEMBER_PARAM)

        | _ -> ()

    member x.ProcessTypeMemberTypeParams(SynValTyparDecls(typeParams, _, _)) =
        for param in typeParams do
            x.ProcessTypeParameter(param, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)

    member x.ProcessActivePatternId(IdentRange range, isLocal: bool) =
        let idMark = x.Mark(range)
        let endOffset = x.GetEndOffset(range)

        while x.CurrentOffset < endOffset do
            let caseElementType =
                let tokenType = x.Builder.GetTokenType()
                if tokenType == FSharpTokenType.IDENTIFIER then
                    if isLocal then ElementType.LOCAL_ACTIVE_PATTERN_CASE_DECLARATION
                    else ElementType.TOP_ACTIVE_PATTERN_CASE_DECLARATION

                elif tokenType == FSharpTokenType.UNDERSCORE then ElementType.ACTIVE_PATTERN_WILD_CASE else
                null

            if isNotNull caseElementType then
                x.MarkToken(caseElementType)

            x.AdvanceLexer()

        x.Done(idMark, ElementType.ACTIVE_PATTERN_ID)

    member x.ProcessSimplePatterns(pats: SynSimplePats) =
        match pats with
        | SynSimplePats.SimplePats(pats, _) ->
            for pat in pats do
                x.ProcessSimplePattern(pat)

        | SynSimplePats.Typed(pats, synType, _) ->
            x.ProcessSimplePatterns(pats)
            x.ProcessType(synType)

    member x.ProcessTypeArgs(typeArgs, ltRange: range option, gtRange: range option, elementType) =
        match ltRange, typeArgs with
        | Some head, _
        | _, TypeRange head :: _ ->
            let mark = x.Mark(head)

            for synType in typeArgs do
                x.ProcessType(synType)

            match gtRange with
            | Some range -> x.Done(range, mark, elementType)
            | _ -> x.Done(mark, elementType)

        | _ -> () // todo: failwith?

    member x.ProcessTypeArgsInReferenceExpr(synExpr) =
        match synExpr with
        | SynExpr.TypeApp(expr, ltRange, typeArgs, _, gtRangeOpt, _, _) ->
            // Get existing psi builder markers.
            let productions = x.Builder.myProduction

            // The last one is an already processed reference expr from this type app (see ProcessExpression).
            let exprEndIndex = productions.Count - 1
            let mutable exprEndMarker = productions.[exprEndIndex]

            let expectedType = ElementType.REFERENCE_EXPR :> NodeType
            Assertion.Assert(exprEndMarker.ElementType == expectedType, "exprEnd.ElementType <> refExpr; {0}", expr)

            // Get reference expr start marker.
            let exprStart = exprEndIndex + exprEndMarker.OppositeMarker
            let mutable exprStartMarker = productions.[exprStart]

            // Remove the Done marker, reset start marker so it's considered unfinished.
            productions.RemoveAt(exprEndIndex)
            exprStartMarker.OppositeMarker <- Marker.InvalidPointer
            productions.[exprStart] <- exprStartMarker

            // Process type args as part of reference expr.
            match ltRange, typeArgs with
            | head, _
            | _, TypeRange head :: _ ->
                let mark = x.Mark(head)

                for synType in typeArgs do
                    x.ProcessType(synType)

                match gtRangeOpt with
                | Some range -> x.Done(range, mark, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
                | _ -> x.Done(mark, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)

            x.Done(exprStart, ElementType.REFERENCE_EXPR)
        | _ -> failwithf "Expecting typeApp, got: %A" synExpr

    member x.ProcessTypeAsTypeReference(synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            x.ProcessNamedTypeReference(lid.Lid)

        | SynType.App(SynType.LongIdent(lid), ltRange, typeArgs, _, gtRange, isPostfix, _) ->
            x.ProcessNamedTypeReference(lid.Lid, typeArgs, ltRange, gtRange, isPostfix)

        | _ -> failwithf "unexpected type: %O" synType

    member x.ProcessType(TypeRange range as synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            let mark = x.Mark(range)
            x.ProcessNamedTypeReference(lid.Lid)
            x.Done(range, mark, ElementType.NAMED_TYPE)

        | SynType.App(typeName, ltRange, typeArgs, _, gtRange, isPostfix, _) ->
            let mark = x.Mark(range)
            let lid =
                match typeName with
                | SynType.LongIdent(lid) -> lid.Lid
                | _ -> failwithf "unexpected type: %O" typeName

            // todo: fix isPostfix
            x.ProcessNamedTypeReference(lid, typeArgs, ltRange, gtRange, isPostfix)
            x.Done(range, mark, ElementType.NAMED_TYPE)

        | SynType.LongIdentApp(_, _, ltRange, typeArgs, _, gtRange, _) ->
            // todo: mark types
            let mark = x.Mark(range)
            x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
            x.Done(range, mark, ElementType.NAMED_TYPE)

        | SynType.Tuple (_, types, _) ->
            let mark = x.Mark(range)
            for _, synType in types do
                x.ProcessType(synType)
            x.Done(range, mark, ElementType.TUPLE_TYPE)

        // todo: struct keyword?
        | SynType.AnonRecd(_, fields, _) ->
            let mark = x.Mark(range)
            for IdentRange range, synType in fields do
                let mark = x.Mark(range)
                x.ProcessType(synType)
                x.Done(range, mark, ElementType.ANON_RECORD_FIELD)
            x.Done(range, mark, ElementType.ANON_RECORD_TYPE)

        | SynType.StaticConstantNamed(synType1, synType2, _)
        | SynType.MeasureDivide(synType1, synType2, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType1)
            x.ProcessType(synType2)
            x.Done(range, mark, ElementType.OTHER_TYPE)

        | SynType.Fun(synType1, synType2, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType1)
            x.ProcessType(synType2)
            x.Done(range, mark, ElementType.FUN_TYPE)

        | SynType.WithGlobalConstraints(synType, _, _)
        | SynType.HashConstraint(synType, _)
        | SynType.MeasurePower(synType, _, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.OTHER_TYPE)

        | SynType.Array(_, synType, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.ARRAY_TYPE)

        | SynType.Var _ ->
            x.MarkAndDone(range, ElementType.VAR_TYPE)

        // todo: mark expressions
        | SynType.StaticConstantExpr _
        | SynType.StaticConstant _ ->
            x.MarkAndDone(range, ElementType.OTHER_TYPE)

        | SynType.Anon _ ->
            x.MarkAndDone(range, ElementType.ANON_TYPE)

    member x.FixExpresion(expr: SynExpr) =
        // A fake SynExpr.Typed node is added for binding with return type specification like in the following
        // member x.Prop: int = 1
        // where 1 is replaced with `1: int`. 
        // These fake nodes have original type specification ranges that are out of the actual expression ranges.
        match expr with
        | SynExpr.Typed(inner, synType, range) when not (rangeContainsRange range synType.Range) -> inner
        | _ -> expr

    member x.MarkChameleonExpression(expr: SynExpr) =
        let (ExprRange range as expr) = x.FixExpresion(expr)
        let mark = x.Mark(range)

        // Replace all tokens with single chameleon token.
        let tokenMark = x.Mark(range)
        x.AdvanceToEnd(range)
        x.Builder.AlterToken(tokenMark, FSharpTokenType.CHAMELEON)

        x.Done(range, mark, ChameleonExpressionNodeType.Instance, expr)