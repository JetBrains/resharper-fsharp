namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System
open System.Collections.Generic
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Range
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.TreeBuilder

[<AbstractClass>]
type FSharpTreeBuilderBase(lexer, document: IDocument, lifetime, projectedOffset, lineShift) =
    inherit TreeBuilderBase(lifetime, lexer)

    // FCS ranges are 1-based.
    let lineShift = lineShift - 1

    let lineOffsets =
        let lineCount = document.GetLineCount()
        Array.init (int lineCount) (fun line -> document.GetLineStartOffset(docLine line))

    let getLineOffset line =
        lineOffsets.[line + lineShift]

    new (sourceFile, lexer, lifetime) =
        FSharpTreeBuilderBase(sourceFile, lexer, lifetime, 0, 0)

    abstract member CreateFSharpFile: unit -> IFSharpFile

    member x.GetOffset(pos: pos) = getLineOffset pos.Line + pos.Column
    member x.GetStartOffset(range: range) = getLineOffset range.StartLine + range.StartColumn
    member x.GetEndOffset(range: range) = getLineOffset range.EndLine + range.EndColumn
    member x.GetStartOffset(IdentRange range) = x.GetStartOffset(range)

    member x.Eof = x.Builder.Eof()
    member x.CurrentOffset = x.Builder.GetTokenOffset() + projectedOffset
    member x.TokenType = x.Builder.GetTokenType()

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
    
    member x.Mark() =
        /// The base member is protected and cannot be used in closures.
        base.Mark()

    member x.Done(mark, elementType) =
        base.Done(mark, elementType)

    member x.Done(range, mark, elementType) =
        x.AdvanceToEnd(range)
        x.Done(mark, elementType)

    member x.Done(range, mark, elementType, data) =
        x.AdvanceToEnd(range)
        x.Builder.Done(mark, elementType, data)

    member x.MarkAndDone(range: range, elementType) =
        let mark = x.Mark(range)
        x.Done(range, mark, elementType)

    member x.MarkAndDone(range: range, elementType1, elementType2) =
        let mark1 = x.Mark(range)
        let mark2 = x.Mark()
        x.Done(range, mark2, elementType2)
        x.Done(mark1, elementType1)

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

    member x.AdvanceToTokenOrOffset(tokenType: TokenNodeType, maxOffset: int, _: range) =
        Assertion.Assert(isNotNull tokenType, "isNotNull tokenType")

//        let offset = x.CurrentOffset
//        Assertion.Assert(offset <= maxOffset, "tokenType: {0}, currentOffset: {1}, maxOffset: {2}, outer range: {3}",
//                         tokenType, offset, maxOffset, range)

        while x.CurrentOffset < maxOffset && x.TokenType != tokenType do
            x.AdvanceLexer()

    /// Should only be used when expected token is known to exist.
    member x.AdvanceToToken(tokenType: TokenNodeType) =
        while not x.Eof && x.TokenType != tokenType do
            x.AdvanceLexer()
    
    /// Should only be used when expected token is known to exist.
    member x.AdvanceToTokenAndSkip(tokenType: TokenNodeType) =
        x.AdvanceToToken(tokenType)
        if x.TokenType == tokenType then
            x.Advance()

    member x.ProcessReferenceName(lid: Ident list) =
        if lid.IsEmpty then () else

        let marks = Stack()
        x.AdvanceToStart(lid.Head.idRange)
        for _ in lid do
            marks.Push(x.Mark())

        for IdentRange id in lid do
            x.Done(id, marks.Pop(), ElementType.EXPRESSION_REFERENCE_NAME)

    member x.ProcessReferenceNameSkipLast(lid: Ident list) =
        match lid with
        | [] -> ()
        | [IdentRange idRange] -> x.AdvanceToEnd(idRange)
        | _ ->

        let marks = Stack()

        x.AdvanceToStart(lid.Head.idRange)
        for id in lid do
            marks.Push(struct {| Mark = x.Mark(); Range = id.idRange |})

        let lastIdRangeAndMark = marks.Pop()
        x.Builder.Drop(lastIdRangeAndMark.Mark)

        for IdentRange idRange in lid do
            if marks.Count > 0 then
                x.Done(idRange, marks.Pop().Mark, ElementType.EXPRESSION_REFERENCE_NAME)

        x.AdvanceToEnd(lastIdRangeAndMark.Range)

    member x.ProcessNamedTypeReference(lid: Ident list) =
        match lid with
        | [] -> ()
        | [IdentRange idRange] -> x.MarkAndDone(idRange, ElementType.TYPE_REFERENCE_NAME)
        | IdentRange idRange :: _ ->

        let mark = x.Mark(idRange)
        x.MarkTypeReferenceQualifierNames(lid)
        x.Done(mark, ElementType.TYPE_REFERENCE_NAME)

    /// Marks simple type reference name qualifiers and advance past the last id.
    /// Does not mark resulting type reference name.
    member x.MarkTypeReferenceQualifierNames(lid: Ident list) =
        let marks = Stack()

        let rec markNamesLid (marks: Stack<_>) lid =
            match lid with
            | [] | [_] -> ()
            | _ :: rest ->
                marks.Push(x.Mark())
                markNamesLid marks rest

        let rec doneNamesLid (marks: Stack<_>) lid =
            match lid with
            | [] -> ()
            | [IdentRange range] ->
                Assertion.Assert(marks.Count = 0, "marks.Count = 0")
                x.AdvanceToEnd(range)

            | IdentRange range :: rest ->
                x.Done(range, marks.Pop(), ElementType.TYPE_REFERENCE_NAME)
                doneNamesLid marks rest

        markNamesLid marks lid
        doneNamesLid marks lid

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
                    x.MarkAndProcessAttributesOrIdOrRange(attrs, Some id, range)

            if moduleKind <> AnonModule then
                x.ProcessReferenceNameSkipLast(lid)

            let elementType =
                match moduleKind with
                | NamedModule -> ElementType.NAMED_MODULE_DECLARATION
                | AnonModule -> ElementType.ANON_MODULE_DECLARATION
                | _ -> ElementType.NAMED_NAMESPACE_DECLARATION

            Some mark, elementType

        | _ ->

        match moduleKind with
        | GlobalNamespace ->
            x.AdvanceToTokenOrRangeStart(FSharpTokenType.NAMESPACE, range)
            let mark = x.Mark()
            Some mark, ElementType.GLOBAL_NAMESPACE_DECLARATION
        | _ -> None, null

    member x.FinishTopLevelDeclaration(mark: int option, range, elementType) =
        x.AdvanceToEnd(range)
        if mark.IsSome then
            x.Done(mark.Value, elementType)

    member x.MarkAndProcessAttributesOrIdOrRange(outerAttrs: SynAttributes, id: Ident option, range: range) =
        match outerAttrs with
        | attrList :: _ ->
            let mark = x.Mark(attrList.Range)
            x.ProcessAttributeLists(outerAttrs)
            mark

        | _ ->
            let rangeStart = x.GetStartOffset(range)
            let startOffset = if id.IsSome then Math.Min(x.GetStartOffset id.Value.idRange, rangeStart) else rangeStart
            x.Mark(startOffset)

    member x.MarkAttributesOrIdOrRangeStart(outerAttrs: SynAttributes, id: Ident option, range: range) =
        match outerAttrs with
        | attrList :: _ -> x.Mark(attrList.Range)
        | _ ->

        let rangeStart = x.GetStartOffset(range)
        let startOffset = if id.IsSome then Math.Min(x.GetStartOffset id.Value.idRange, rangeStart) else rangeStart
        x.Mark(startOffset)
    
    member x.ProcessOpenDeclTarget(openDeclTarget, range) =
        let mark = x.MarkTokenOrRange(FSharpTokenType.OPEN, range)
        match openDeclTarget with
        | SynOpenDeclTarget.ModuleOrNamespace(lid, _) ->
            x.ProcessNamedTypeReference(lid)
        | SynOpenDeclTarget.Type(typeName, _) ->
            x.ProcessType(typeName)
        x.Done(range, mark, ElementType.OPEN_STATEMENT)

    member x.StartException(SynExceptionDefnRepr(_, UnionCase(caseType = unionCaseType), _, _, _, range)) =
        let mark = x.Mark(range)
        x.ProcessUnionCaseType(unionCaseType, ElementType.EXCEPTION_FIELD_DECLARATION)
        mark

    member x.StartType(attrs, typeParams, constraints, lid: LongIdent, range, typeTokenType) =
        let startRange =
            match attrs with
            | attrList :: _ -> attrList.Range
            | _ -> range

        let mark = x.MarkTokenOrRange(typeTokenType, startRange)
        x.ProcessAttributeLists(attrs)

        if not lid.IsEmpty then
            let id = lid.Head
            let idOffset = x.GetStartOffset id

            let typeParamsOffset =
                match typeParams with
                | TyparDecl(_, Typar(id, _, _)) :: _ -> x.GetStartOffset id
                | [] -> idOffset

            let paramsInBraces = idOffset < typeParamsOffset
            x.ProcessTypeParametersOfType typeParams constraints range paramsInBraces

            // Needs to advance past id range due to implicit ctor range in class includes id.
            x.AdvanceToEnd(id.idRange)
        mark

    member x.ProcessTypeParametersOfType typeParams constraints range paramsInBraces =
        match typeParams with
        | TyparDecl(_, Typar(IdentRange idRange, _, _)) :: _ ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.LESS, idRange)
            for p in typeParams do
                x.ProcessTypeParameter(p, ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION)
            for typeConstraint in constraints do
                x.ProcessTypeConstraint(typeConstraint)
            if paramsInBraces then
                let endOffset = x.GetEndOffset(range)
                x.AdvanceToTokenOrOffset(FSharpTokenType.GREATER, endOffset, range)
                if x.Builder.GetTokenType() == FSharpTokenType.GREATER then
                    x.AdvanceLexer()
            x.Done(mark, ElementType.TYPE_PARAMETER_OF_TYPE_LIST)
        | [] -> ()

    member x.ProcessTypeParameter(TyparDecl(_, Typar(IdentRange range, _, _)), elementType) =
        x.MarkAndDone(range, elementType)

    member x.ProcessUnionCaseType(caseType, fieldElementType) =
        match caseType with
        | UnionCaseFields(fields) ->
            match fields with
            | field :: _ ->
                let fieldListMark = x.Mark(field.StartPos)
                for f in fields do
                    x.ProcessField f fieldElementType
                x.Done(fieldListMark, ElementType.UNION_CASE_FIELD_DECLARATION_LIST)
            | _ -> ()

        // todo: used in FSharp.Core only, otherwise warning
        | UnionCaseFullType _ -> ()

    member x.AddObjectModelTypeReprNode(kind: SynTypeDefnKind) =
        match kind with
        | SynTypeDefnKind.TyconClass
        | SynTypeDefnKind.TyconStruct
        | SynTypeDefnKind.TyconInterface -> true
        | _ -> false
    
    member x.GetObjectModelTypeReprElementType(reprKind: SynTypeDefnKind) =
        match reprKind with
        | TyconClass -> ElementType.CLASS_REPRESENTATION
        | TyconInterface -> ElementType.INTERFACE_REPRESENTATION
        | TyconStruct -> ElementType.STRUCT_REPRESENTATION
        | _ -> failwithf "Unexpected type representation kind: %A" reprKind

    member x.ProcessSimpleTypeRepresentation(repr) =
        match repr with
        | SynTypeDefnSimpleRepr.Record(_, fields, range) ->
            let mark = x.Mark(range)

            if not fields.IsEmpty then
                let (Field(range = firstFieldRange)) = fields.Head
                let (Field(range = lastFieldRange)) = List.last fields

                let fieldListMark = x.Mark(firstFieldRange)
                for field in fields do
                    x.ProcessField field ElementType.RECORD_FIELD_DECLARATION
                x.Done(lastFieldRange, fieldListMark, ElementType.RECORD_FIELD_DECLARATION_LIST)

            x.Done(range, mark, ElementType.RECORD_REPRESENTATION)

        | SynTypeDefnSimpleRepr.Enum(cases, range) ->
            let representationMark = x.Mark(range)
            if not cases.IsEmpty then
                let firstCaseRange = cases.Head.Range
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.BAR, firstCaseRange)

            let casesListMark = x.Mark(range)
            for case in cases do
                x.ProcessEnumCase case
            x.Done(range, casesListMark, ElementType.ENUM_CASE_LIST)
            x.Done(range, representationMark, ElementType.ENUM_REPRESENTATION)

        | SynTypeDefnSimpleRepr.Union(_, cases, range) ->
            let representationMark = x.Mark(range)
            if not cases.IsEmpty then
                let firstCaseRange = cases.Head.Range
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.BAR, firstCaseRange)
 
            let caseListMark = x.Mark()
            for case in cases do
                x.ProcessUnionCase(case)
            x.Done(range, caseListMark, ElementType.UNION_CASE_LIST)
            x.Done(representationMark, ElementType.UNION_REPRESENTATION)

        | SynTypeDefnSimpleRepr.TypeAbbrev(_, (TypeRange range as synType), _) ->
            let representationMark = x.Mark(range)
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(mark, ElementType.TYPE_USAGE_OR_UNION_CASE_DECLARATION)
            x.Done(representationMark, ElementType.TYPE_ABBREVIATION_REPRESENTATION)

        // Empty type `type T`
        | SynTypeDefnSimpleRepr.None _ -> ()

        | _ -> failwithf "Unexpected simple type representation: %A" repr

    member x.ProcessUnionCase(UnionCase(attrs, _, caseType, _, _, range)) =
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)
        x.ProcessAttributeLists(attrs)
        x.ProcessUnionCaseType(caseType, ElementType.UNION_CASE_FIELD_DECLARATION)
        x.Done(range, mark, ElementType.UNION_CASE_DECLARATION)

    member x.ProcessOuterAttrs(attrs: SynAttributeList list, range: range) =
        match attrs with
        | { Range = r } as attributeList :: rest when posLt r.End range.Start ->
            x.ProcessAttributeList(attributeList)
            x.ProcessOuterAttrs(rest, range)

        | _ -> ()

    member x.ProcessAttributeLists(attributeLists) =
        for attributeList in attributeLists do
            x.ProcessAttributeList(attributeList)

    member x.ProcessAttributeList(attributeList) =
        let range = attributeList.Range
        let mark = x.Mark(range)
        for attribute in attributeList.Attributes do
            x.ProcessAttribute(attribute)
        x.Done(range, mark, ElementType.ATTRIBUTE_LIST)

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

        let ExprRange argRange as argExpr = attr.ArgExpr
        if lidWithDots.Range <> argRange then
            // Arg range is the same when fake SynExpr.Const is added
            x.MarkChameleonExpression(argExpr)


        x.Done(attr.Range, mark, ElementType.ATTRIBUTE)

    member x.ProcessEnumCase(EnumCase(attrs, _, _, _, range)) =
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)
        x.ProcessAttributeLists(attrs)
        x.Done(range, mark, ElementType.ENUM_MEMBER_DECLARATION)

    member x.ProcessField(Field(attrs, _, id, synType, _, _, _, range)) elementType =
        let mark =
            match attrs with
            | attrList :: _ ->
                let mark = x.Mark(attrList.Range)
                x.ProcessAttributeLists(attrs)
                mark

            | _ ->
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

    member x.ProcessImplicitCtorSimplePats(pats: SynSimplePats) =
        let range = pats.Range
        let paramMark = x.Mark(range)

        match pats with
        | SynSimplePats.SimplePats([], _) ->
            x.MarkAndDone(range, ElementType.UNIT_PAT)
            x.Done(range, paramMark, ElementType.PARAMETERS_PATTERN_DECLARATION)

        | _ ->
        
        let parenPatMark = x.Mark()

        match pats with
        | SynSimplePats.SimplePats([pat], _) ->
            x.ProcessImplicitCtorParam(pat)

        | SynSimplePats.SimplePats(headPat :: _ as pats, _) ->
            let tupleMark = x.Mark(headPat.Range)
            for pat in pats do
                x.ProcessImplicitCtorParam(pat)
            x.Done(tupleMark, ElementType.TUPLE_PAT)
            x.AdvanceToTokenAndSkip(FSharpTokenType.RPAREN)

        | _ -> failwithf $"Unexpected simple pats: {pats}"

        x.Done(range, parenPatMark, ElementType.PAREN_PAT)
        x.Done(range, paramMark, ElementType.PARAMETERS_PATTERN_DECLARATION)

    member x.ProcessImplicitCtorParam(pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(ident = IdentRange range) ->
            x.MarkAndDone(range, ElementType.LOCAL_REFERENCE_PAT, ElementType.EXPRESSION_REFERENCE_NAME)

        | SynSimplePat.Typed(pat, synType, range) ->
            let mark = x.Mark(range)
            x.ProcessImplicitCtorParam(pat)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.TYPED_PAT)

        | SynSimplePat.Attrib(pat, attrs, range) ->
            let mark = x.Mark(range)
            x.ProcessAttributeLists(attrs)
            x.ProcessImplicitCtorParam(pat)
            x.Done(range, mark, ElementType.ATTRIB_PAT)

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

    member x.ProcessTypeAsTypeReferenceName(synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            x.ProcessNamedTypeReference(lid.Lid)

        | SynType.App(typeName, ltRange, typeArgs, _, gtRange, isPostfix, range) ->
            let lid =
                match typeName with
                | SynType.LongIdent(lid) -> lid.Lid
                | SynType.MeasurePower(SynType.LongIdent(lid), _, _) -> lid.Lid
                | SynType.MeasureDivide(SynType.LongIdent(lid), _, _) -> lid.Lid
                | _ -> failwithf "unexpected type: %A" typeName

            let mark = x.Mark(range)
            if isPostfix then
                x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.POSTFIX_APP_TYPE_ARGUMENT_LIST)
                x.MarkTypeReferenceQualifierNames(lid)
            else
                x.MarkTypeReferenceQualifierNames(lid)
                x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)

            x.Done(mark, ElementType.TYPE_REFERENCE_NAME)

        | SynType.LongIdentApp(typeName, lid, ltRange, typeArgs, _, gtRange, range) ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(typeName)
            x.MarkTypeReferenceQualifierNames(lid.Lid)
            x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
            x.Done(mark, ElementType.TYPE_REFERENCE_NAME)

        | SynType.Var(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)

        | SynType.Anon _ ->
            // Produced on error
            ()

        | _ -> x.AdvanceToEnd(synType.Range) // todo: mark error types

    member x.ProcessType(TypeRange range as synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            let mark = x.Mark(range)
            x.ProcessNamedTypeReference(lid.Lid)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.App _ ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.LongIdentApp _ ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.Tuple (_, types, _) ->
            let mark = x.Mark(range)
            for _, synType in types do
                x.ProcessType(synType)
            x.Done(range, mark, ElementType.TUPLE_TYPE_USAGE)

        // todo: struct keyword?
        | SynType.AnonRecd(_, fields, _) ->
            let mark = x.Mark(range)
            for IdentRange range, synType in fields do
                let mark = x.Mark(range)
                x.MarkAndDone(range, ElementType.EXPRESSION_REFERENCE_NAME)
                x.ProcessType(synType)
                x.Done(range, mark, ElementType.ANON_RECORD_FIELD)
            x.Done(range, mark, ElementType.ANON_RECORD_TYPE_USAGE)

        | SynType.StaticConstantNamed(synType1, synType2, _)
        | SynType.MeasureDivide(synType1, synType2, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType1)
            x.ProcessType(synType2)
            x.Done(range, mark, ElementType.UNSUPPORTED_TYPE_USAGE)

        | SynType.Fun(synType1, synType2, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType1)
            x.ProcessType(synType2)
            x.Done(range, mark, ElementType.FUNCTION_TYPE_USAGE)

        | SynType.WithGlobalConstraints(synType, constraints, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            for typeConstraint in constraints do
                x.ProcessTypeConstraint(typeConstraint)
            x.Done(range, mark, ElementType.UNSUPPORTED_TYPE_USAGE)

        | SynType.HashConstraint(synType, _)
        | SynType.MeasurePower(synType, _, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.UNSUPPORTED_TYPE_USAGE)

        | SynType.Array(_, synType, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.ARRAY_TYPE_USAGE)

        | SynType.Var _ ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        // todo: mark expressions
        | SynType.StaticConstantExpr _
        | SynType.StaticConstant _ ->
            x.MarkAndDone(range, ElementType.UNSUPPORTED_TYPE_USAGE)

        | SynType.Anon _ ->
            x.MarkAndDone(range, ElementType.ANON_TYPE_USAGE)

        | SynType.Paren(innerType, range) ->
            let mark = x.Mark(range)
            x.ProcessType(innerType)
            x.Done(range, mark, ElementType.PAREN_TYPE_USAGE)

    member x.ProcessTypeConstraint(typeConstraint: SynTypeConstraint) =
        match typeConstraint with
        | WhereTyparIsValueType(typeParameter, _)
        | WhereTyparIsReferenceType(typeParameter, _)
        | WhereTyparIsUnmanaged(typeParameter, _)
        | WhereTyparSupportsNull(typeParameter, _)
        | WhereTyparIsComparable(typeParameter, _)
        | WhereTyparIsEquatable(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)

        | WhereTyparDefaultsToType(typeParameter, synType, _)
        | WhereTyparSubtypeOfType(typeParameter, synType, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.ProcessType(synType)

        | WhereTyparSupportsMember(typeParameterTypes, memberSig, _) ->
            for synType in typeParameterTypes do
                x.ProcessType(synType)
            match memberSig with
            | SynMemberSig.Member(ValSpfn(synType = synType), _, _) ->
                x.ProcessType(synType)
            | _ -> ()

        | WhereTyparIsEnum(typeParameter, synTypes, _)
        | WhereTyparIsDelegate(typeParameter, synTypes, _) ->
            x.ProcessTypeParameter(typeParameter)
            for synType in synTypes do
                x.ProcessType(synType)

    member x.ProcessTypeParameter(Typar(IdentRange range, _, _)) =
        let mark = x.Mark(range)
        x.MarkAndDone(range, ElementType.TYPE_PARAMETER_ID)
        x.Done(range, mark, ElementType.TYPE_REFERENCE_NAME)

    member x.FixExpresion(expr: SynExpr) =
        // A fake SynExpr.Typed node is added for binding with return type specification like in the following
        // member x.Prop: int = 1
        // where 1 is replaced with `1: int`. 
        // These fake nodes have original type specification ranges that are out of the actual expression ranges.
        match expr with
        | SynExpr.Typed(inner, synType, range) when not (rangeContainsRange range synType.Range) -> inner
        | _ -> expr

    member x.RemoveDoExpr(expr: SynExpr) =
        match expr with
        | SynExpr.Do(expr, _) -> expr
        | _ -> expr

    member x.MarkChameleonExpression(expr: SynExpr) =
        let ExprRange range as expr = x.FixExpresion(expr)

        let startOffset = x.GetStartOffset(range)
        let mark = x.Mark(startOffset)
        Assertion.Assert(x.CurrentOffset = startOffset, "x.CurrentOffset = startOffset")

        // Replace all tokens with single chameleon token.
        let tokenMark = x.Mark(range)
        x.AdvanceToEnd(range)
        x.Builder.AlterToken(tokenMark, FSharpTokenType.CHAMELEON)

        let lineStart = lineOffsets.[range.StartLine - 1]
        let data = expr, startOffset, lineStart
        x.Done(range, mark, ChameleonExpressionNodeType.Instance, data)

    member x.ProcessHashDirective(ParsedHashDirective(id, _, range)) =
        let mark = x.Mark(range)
        let elementType =
            match id with
            | "l" | "load" -> ElementType.LOAD_DIRECTIVE
            | "r" | "reference" -> ElementType.REFERENCE_DIRECTIVE
            | "I" -> ElementType.I_DIRECTIVE
            | _ -> ElementType.OTHER_DIRECTIVE
        x.Done(range, mark, elementType)
