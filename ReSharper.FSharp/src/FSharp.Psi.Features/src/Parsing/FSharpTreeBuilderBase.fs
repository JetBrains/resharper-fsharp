namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System.Collections.Generic
open FSharp.Compiler.Syntax
open FSharp.Compiler.Syntax.PrettyNaming
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text
open FSharp.Compiler.Xml
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing.FcsSyntaxTreeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type FSharpTreeBuilderBase(lexer, document: IDocument, lifetime, path: VirtualFileSystemPath, projectedOffset, lineShift) =
    inherit TreeBuilderBase(lifetime, lexer)

    // FCS ranges are 1-based.
    let lineShift = lineShift - 1

    let lineOffsets =
        let lineCount = document.GetLineCount()
        Array.init (int lineCount) (fun line -> document.GetLineStartOffset(docLine line))

    let getLineOffset line =
        lineOffsets[line + lineShift]

    new (sourceFile, lexer, lifetime, path) =
        FSharpTreeBuilderBase(sourceFile, lexer, lifetime, path, 0, 0)

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
        // The base member is protected and cannot be used in closures.
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
        x.AdvanceToTokenOrPos(tokenType, range.Start)
        x.Mark()

    member x.AdvanceToOffset(offset) =
        while x.CurrentOffset < offset && not x.Eof do
            x.AdvanceLexer()

    member x.AdvanceToTokenOrRangeStart(tokenType: TokenNodeType, range: range) =
        x.AdvanceToTokenOrPos(tokenType, range.Start)

    member x.AdvanceToTokenOrRangeEnd(tokenType: TokenNodeType, range: range) =
        x.AdvanceToTokenOrPos(tokenType, range.End)

    member x.AdvanceToTokenOrPos(tokenType: TokenNodeType, pos: pos) =
        let maxOffset = x.GetOffset(pos)
        while x.CurrentOffset < maxOffset && x.TokenType != tokenType && not x.Eof do
            x.AdvanceLexer()

    member x.MarkXmlDocOwner(xmlDoc: XmlDoc, expectedType: TokenNodeType, declarationRange: range) =
        let mark = x.MarkTokenOrRange(expectedType, declarationRange)
        if xmlDoc.HasDeclaration then
            x.MarkAndDone(xmlDoc.Range, XmlDocBlockNodeType.Instance)
        mark

    member x.ProcessReferenceName(lid: Ident list) =
        if lid.IsEmpty then () else

        let marks = Stack()
        x.AdvanceToStart(lid.Head.idRange)
        for _ in lid do
            marks.Push(x.Mark())

        for id in lid do
            x.Done(id.idRange, marks.Pop(), ElementType.EXPRESSION_REFERENCE_NAME)

    member x.ProcessReferenceName(lid: SynIdent list) =
        if lid.IsEmpty then () else

        let marks = Stack()
        let (SynIdentRange idRange) = lid.Head

        x.AdvanceToStart(idRange)
        for _ in lid do
            marks.Push(x.Mark())

        for SynIdentRange idRange as SynIdent(id, _) in lid do
            let isLastId = marks.Count = 1
            if isLastId && IsActivePatternName id.idText then 
                x.ProcessActivePatternId(idRange, ElementType.ACTIVE_PATTERN_NAMED_CASE_REFERENCE_NAME) // todo
            x.Done(idRange, marks.Pop(), ElementType.EXPRESSION_REFERENCE_NAME)

    member x.ProcessReferenceName(lid: (Ident * IdentTrivia option) list) =
        if lid.IsEmpty then () else

        let marks = Stack()
        let (SynIdentWithTriviaRange idRange) = lid.Head

        x.AdvanceToStart(idRange)
        for _ in lid do
            marks.Push(x.Mark())

        for SynIdentWithTriviaRange idRange as (id, _) in lid do
            let isLastId = marks.Count = 1
            if isLastId && IsActivePatternName id.idText then 
                x.ProcessActivePatternId(idRange, ElementType.ACTIVE_PATTERN_NAMED_CASE_REFERENCE_NAME) // todo
            x.Done(idRange, marks.Pop(), ElementType.EXPRESSION_REFERENCE_NAME)

    member x.ProcessTypeReferenceNameSkipLast(lid: Ident list) =
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
                x.Done(idRange, marks.Pop().Mark, ElementType.TYPE_REFERENCE_NAME)

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

    member x.StartTopLevelDeclaration(lid: LongIdent, attrs: SynAttributes, moduleKind, xmlDoc: XmlDoc, range) =
        match lid with
        | [] ->
            match moduleKind with
            | SynModuleOrNamespaceKind.GlobalNamespace ->
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.NAMESPACE, range)
                let mark = x.Mark()
                Some mark, ElementType.GLOBAL_NAMESPACE_DECLARATION
            | _ -> None, null

        | _ ->

        let mark =
            match moduleKind with
            | SynModuleOrNamespaceKind.AnonModule ->
                x.Mark()

            | _ ->
                x.MarkAndProcessIntro(attrs, xmlDoc, null, range)

        if moduleKind <> SynModuleOrNamespaceKind.AnonModule then
            x.ProcessTypeReferenceNameSkipLast(lid)

        let elementType =
            match moduleKind with
            | SynModuleOrNamespaceKind.NamedModule -> ElementType.NAMED_MODULE_DECLARATION
            | SynModuleOrNamespaceKind.AnonModule -> ElementType.ANON_MODULE_DECLARATION
            | _ -> ElementType.NAMED_NAMESPACE_DECLARATION

        Some mark, elementType

    member x.FinishTopLevelDeclaration(mark: int option, range, elementType) =
        x.AdvanceToEnd(range)
        if mark.IsSome then
            x.Done(mark.Value, elementType)

    /// Process xmlDoc and attributes
    member x.MarkAndProcessIntro(attrs: SynAttributes, xmlDoc: XmlDoc, tokenType: TokenNodeType, range: range) =
        let mark = x.MarkXmlDocOwner(xmlDoc, tokenType, range)
        x.ProcessAttributeLists(attrs)
        mark

    member x.ProcessOpenDeclTarget(openDeclTarget, range) =
        let mark = x.MarkTokenOrRange(FSharpTokenType.OPEN, range)
        match openDeclTarget with
        | SynOpenDeclTarget.ModuleOrNamespace(lid, _) ->
            x.ProcessNamedTypeReference(lid.LongIdent)
        | SynOpenDeclTarget.Type(typeName, _) ->
            x.ProcessTypeAsTypeReferenceName(typeName)
        x.Done(range, mark, ElementType.OPEN_STATEMENT)

    member x.StartException(SynExceptionDefnRepr(attributeLists, unionCase, _, XmlDoc xmlDoc, _, _), exnRange) =
        let (SynUnionCase(caseType = unionCaseType)) = unionCase
        let mark = x.MarkXmlDocOwner(xmlDoc, null, exnRange)
        x.ProcessAttributeLists(attributeLists)
        x.ProcessUnionCaseType(unionCaseType, ElementType.EXCEPTION_FIELD_DECLARATION)
        mark

    member x.StartType(attrs: SynAttributes, xmlDoc, typeParams: SynTyparDecls option, constraints, lid: LongIdent, range, typeTokenType) =
        let mark = x.MarkAndProcessIntro(attrs, xmlDoc, typeTokenType, range)

        if not lid.IsEmpty then
            let id = lid.Head

            match typeParams with
            | Some(typeParams) -> x.ProcessTypeParameters(typeParams, true)
            | _ -> ()

            x.ProcessConstraintsClause(constraints)

            // Needs to advance past id range due to implicit ctor range in class includes id.
            x.AdvanceToEnd(id.idRange)
        mark

    member x.ProcessTypeParameters(typeParams: SynTyparDecls, isForType) =
        let range = typeParams.Range
        let mark = x.Mark(range)

        let typeParameterElementType =
            if isForType then
                ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION
            else
                ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION

        for p in typeParams.TyparDecls do
            x.ProcessTypeParameter(p, typeParameterElementType)
        x.ProcessConstraintsClause(typeParams.Constraints)

        let typeParameterListElementType =
            match typeParams with
            | SynTyparDecls.PostfixList _ -> ElementType.POSTFIX_TYPE_PARAMETER_DECLARATION_LIST
            | _ -> ElementType.PREFIX_TYPE_PARAMETER_DECLARATION_LIST

        x.Done(range, mark, typeParameterListElementType)

    member x.ProcessConstraintsClause(constraints: SynTypeConstraint list) =
        match constraints with
        | [] -> ()
        | typeConstraint :: _ ->

        let mark = x.MarkTokenOrRange(FSharpTokenType.WHEN, typeConstraint.Range)
        for typeConstraint in constraints do
            x.ProcessTypeConstraint(typeConstraint)
        x.Done(mark, ElementType.TYPE_CONSTRAINTS_CLAUSE)

    member x.ProcessTypeParameter(SynTyparDecl(attrs, SynTypar(IdentRange range, _, _)), elementType) =
        let range = 
            match attrs with
            | [] -> range
            | attrList :: _ -> Range.unionRanges attrList.Range range

        let mark = x.MarkAndProcessIntro(attrs, XmlDoc.Empty, null, range)
        x.Done(range, mark, elementType)

    member x.ProcessUnionCaseType(caseType, fieldElementType) =
        match caseType with
        | SynUnionCaseKind.Fields(fields) ->
            match fields with
            | SynField(range = range) :: _ ->
                let fieldListMark = x.Mark(range)
                for f in fields do
                    x.ProcessField f fieldElementType
                x.Done(fieldListMark, ElementType.UNION_CASE_FIELD_DECLARATION_LIST)
            | _ -> ()

        | SynUnionCaseKind.FullType(fullType, fullTypeInfo) ->
            x.ProcessSignatureType(fullTypeInfo, fullType)

    member x.AddObjectModelTypeReprNode(kind: SynTypeDefnKind) =
        match kind with
        | SynTypeDefnKind.Class
        | SynTypeDefnKind.Struct
        | SynTypeDefnKind.Interface -> true
        | _ -> false

    member x.GetObjectModelTypeReprElementType(reprKind: SynTypeDefnKind) =
        match reprKind with
        | SynTypeDefnKind.Class -> ElementType.CLASS_REPRESENTATION
        | SynTypeDefnKind.Interface -> ElementType.INTERFACE_REPRESENTATION
        | SynTypeDefnKind.Struct -> ElementType.STRUCT_REPRESENTATION
        | _ -> failwithf "Unexpected type representation kind: %A" reprKind

    member x.ProcessSimpleTypeRepresentation(repr) =
        match repr with
        | SynTypeDefnSimpleRepr.Record(_, fields, range) ->
            let representationMark = x.Mark(range)

            if not fields.IsEmpty then
                let (SynField(range = firstFieldRange)) as firstField = fields.Head
                let (SynField(range = lastFieldRange)) = List.last fields

                let fieldListMark = x.Mark(firstFieldRange)

                for field in fields do
                    x.ProcessField field ElementType.RECORD_FIELD_DECLARATION
                x.Done(lastFieldRange, fieldListMark, ElementType.RECORD_FIELD_DECLARATION_LIST)

            x.Done(range, representationMark, ElementType.RECORD_REPRESENTATION)

        | SynTypeDefnSimpleRepr.Enum(cases, range) ->
            let representationMark = x.Mark(range)
            for case in cases do
                x.ProcessEnumCase(case)
            x.Done(representationMark, ElementType.ENUM_REPRESENTATION)

        | SynTypeDefnSimpleRepr.Union(_, cases, range) ->
            let representationMark = x.Mark(range)

            for case in cases do
                x.ProcessUnionCase(case)
            x.Done(representationMark, ElementType.UNION_REPRESENTATION)

        | SynTypeDefnSimpleRepr.TypeAbbrev(_, (TypeRange range as synType), _) ->
            let representationMark = x.Mark(range)
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.Done(mark, ElementType.TYPE_USAGE_OR_UNION_CASE_DECLARATION)
            x.Done(representationMark, ElementType.TYPE_ABBREVIATION_REPRESENTATION)

        // Empty type `type T`
        | SynTypeDefnSimpleRepr.None _ -> ()

        | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(_, range) ->
            x.MarkAndDone(range, ElementType.IL_ASSEMBLY_REPRESENTATION)

        | _ -> failwithf "Unexpected simple type representation: %A" repr

    member x.ProcessUnionCase(SynUnionCase(attrs, _, caseType, XmlDoc xmlDoc, _, range, _)) =
        let mark = x.MarkXmlDocOwner(xmlDoc, FSharpTokenType.BAR, range)
        x.ProcessAttributeLists(attrs)
        x.ProcessUnionCaseType(caseType, ElementType.UNION_CASE_FIELD_DECLARATION)
        x.Done(range, mark, ElementType.UNION_CASE_DECLARATION)

    member x.ProcessOuterAttrs(attrs: SynAttributeList list, range: range) =
        match attrs with
        | { Range = r } as attributeList :: rest when Position.posLt r.End range.Start ->
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
        x.ProcessNamedTypeReference(lidWithDots.LongIdent)

        let ExprRange argRange as argExpr = attr.ArgExpr
        if lidWithDots.Range <> argRange then
            // Arg range is the same when fake SynExpr.Const is added
            x.MarkChameleonExpression(argExpr)


        x.Done(attr.Range, mark, ElementType.ATTRIBUTE)

    member x.ProcessEnumCase(SynEnumCase(attrs, _, expr, XmlDoc xmlDoc, range, _)) =
        let mark = x.MarkXmlDocOwner(xmlDoc, FSharpTokenType.BAR, range)
        x.ProcessAttributeLists(attrs)
        x.MarkChameleonExpression(expr)
        x.Done(range, mark, ElementType.ENUM_CASE_DECLARATION)

    member x.ProcessField(SynField(attrs, _, _, synType, _, XmlDoc xmlDoc, _, range, _)) elementType =
        let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)
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

        | SynSimplePats.SimplePats(headPat :: _ as pats, range) ->
            let tupleMark = x.Mark(headPat.Range)
            for pat in pats do
                x.ProcessImplicitCtorParam(pat)
            x.Done(tupleMark, ElementType.TUPLE_PAT)
            x.AdvanceToTokenOrRangeEnd(FSharpTokenType.RPAREN, range)
            if x.TokenType == FSharpTokenType.RPAREN then
                x.Advance()

        | _ -> failwithf $"Unexpected simple pats: {pats}"

        x.Done(range, parenPatMark, ElementType.PAREN_PAT)
        x.Done(range, paramMark, ElementType.PARAMETERS_PATTERN_DECLARATION)

    member x.ProcessReturnTypeInfo(valInfo: SynValInfo, synType: SynType) =
        let (SynValInfo(_, SynArgInfo(returnAttrs, _, _))) = valInfo

        let returnInfoStart =
            match returnAttrs with
            | { Range = attrsRange } :: _ -> attrsRange
            | _ -> synType.Range

        let returnInfoStart = x.MarkTokenOrRange(FSharpTokenType.COLON, returnInfoStart)
        x.ProcessSignatureType(valInfo, synType)
        x.Done(returnInfoStart, ElementType.RETURN_TYPE_INFO)

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

    member x.ProcessActivePatternId(range: range, caseElementType) =
        let idMark = x.Mark(range)

        let endOffset = x.GetEndOffset(range)

        while x.CurrentOffset < endOffset do
            let caseElementType =
                let tokenType = x.Builder.GetTokenType()
                if tokenType == FSharpTokenType.IDENTIFIER then caseElementType else
                if tokenType == FSharpTokenType.UNDERSCORE then ElementType.WILD_ACTIVE_PATTERN_WILD_CASE else
                null

            if isNotNull caseElementType then
                x.MarkToken(caseElementType)

            x.AdvanceLexer()

        x.Done(idMark, ElementType.ACTIVE_PATTERN_ID)

    member x.ProcessActivePatternDecl(id, isLocal) =
        let caseElementType =
            if isLocal then
                ElementType.LOCAL_ACTIVE_PATTERN_CASE_DECLARATION
            else
                ElementType.TOP_ACTIVE_PATTERN_CASE_DECLARATION

        x.ProcessActivePatternId(id, caseElementType)

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
            let mutable exprEndMarker = productions[exprEndIndex]

            let expectedType = ElementType.REFERENCE_EXPR :> NodeType
            Assertion.Assert(exprEndMarker.ElementType == expectedType, "exprEnd.ElementType <> refExpr; {0}", expr)

            // Get reference expr start marker.
            let exprStart = exprEndIndex + exprEndMarker.OppositeMarker
            let mutable exprStartMarker = productions[exprStart]

            // Remove the Done marker, reset start marker so it's considered unfinished.
            productions.RemoveAt(exprEndIndex)
            exprStartMarker.OppositeMarker <- Marker.InvalidPointer
            productions[exprStart] <- exprStartMarker

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
            x.ProcessNamedTypeReference(lid.LongIdent)

        | SynType.App(SynType.Var(_, varRange), ltRange, typeArgs, _, gtRange, isPostfix, range) ->
            let mark = x.Mark(range)
            if isPostfix then
                x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.POSTFIX_APP_TYPE_ARGUMENT_LIST)
                x.MarkAndDone(varRange, ElementType.TYPE_PARAMETER_ID)
            else
                x.MarkAndDone(varRange, ElementType.TYPE_PARAMETER_ID)
                x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
            x.Done(range, mark, ElementType.TYPE_REFERENCE_NAME)

        | SynType.App(typeName, ltRange, typeArgs, _, gtRange, isPostfix, range) ->
            let lid =
                match typeName with
                | SynType.LongIdent(lid) -> lid.LongIdent
                | SynType.MeasurePower(SynType.LongIdent(lid), _, _) -> lid.LongIdent
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
            x.MarkTypeReferenceQualifierNames(lid.LongIdent)
            x.ProcessTypeArgs(typeArgs, ltRange, gtRange, ElementType.PREFIX_APP_TYPE_ARGUMENT_LIST)
            x.Done(mark, ElementType.TYPE_REFERENCE_NAME)

        | SynType.Var(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)

        | SynType.Anon _ ->
            // Produced on error
            ()

        | _ -> x.AdvanceToEnd(synType.Range) // todo: mark error types

    member x.ProcessSignatureType(SynValInfo(paramGroups, returnInfo), synType: SynType) =
        let processParameterSig (SynArgInfo(attrs, isOptional, id)) (TypeRange range as synType) =
            let mark =
                match attrs with
                | attrList :: _ -> x.Mark(attrList.Range)
                | _ ->

                let range = id |> Option.map (fun id -> id.idRange) |> Option.defaultValue range
                if isOptional then
                    x.MarkTokenOrRange(FSharpTokenType.QMARK, range)
                else
                    x.Mark(range)

            x.ProcessAttributeLists(attrs)
            x.ProcessType(synType)
            x.Done(mark, ElementType.PARAMETER_SIGNATURE_TYPE_USAGE)

        let processParameterGroup paramGroup synType =
            match paramGroup, synType with
            | _, SynType.Tuple(false, TypeTupleSegments types, range) ->
                let mark =
                    match paramGroup with
                    | SynArgInfo(attrList :: _, _, _) :: _ ->
                        x.Mark(attrList.Range)

                    | SynArgInfo(optional = true) :: _ ->
                        x.MarkTokenOrRange(FSharpTokenType.QMARK, range)

                    | SynArgInfo(_, _, Some(IdentRange idRange)) :: _ ->
                        x.Mark(idRange)

                    | _ ->
                        x.Mark(range)

                types |> List.iter2 processParameterSig paramGroup
                x.Done(mark, ElementType.TUPLE_TYPE_USAGE)
            | [ param ], _ ->
                processParameterSig param synType
            | _ -> ()

        let rec loop paramGroups returnInfo synType =
            match paramGroups, synType with
            | group :: rest, SynType.Fun(arg, returnType, range, _) ->
                let mark = x.Mark(range)
                processParameterGroup group arg
                loop rest returnInfo returnType
                x.Done(mark, ElementType.FUNCTION_TYPE_USAGE)
            | _, _ -> processParameterSig returnInfo synType

        match synType with
        | SynType.WithGlobalConstraints(synType, constraints, range) ->
            let mark = x.Mark(range)
            loop paramGroups returnInfo synType
            x.ProcessConstraintsClause(constraints)
            x.Done(mark, ElementType.CONSTRAINED_TYPE_USAGE)
        | _ ->
            loop paramGroups returnInfo synType

    member x.ProcessType(TypeRange range as synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            let mark = x.Mark(range)
            x.ProcessNamedTypeReference(lid.LongIdent)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.App _ ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.LongIdentApp _ ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.NAMED_TYPE_USAGE)

        | SynType.Tuple (_, TypeTupleSegments types, _) ->
            let mark = x.Mark(range)
            for synType in types do
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

        // StaticConstantNamed can be dummy, if the corresponding type parameter is empty
        // i.e. T<int, {caret}>
        | SynType.StaticConstantNamed(ident = SynType.LongIdent(SynLongIdent([ident], [], _))) when ident.idText = "" ->
            ()

        | SynType.StaticConstantNamed(synType1, synType2, _) ->
            x.MarkTypes(synType1, synType2, range, ElementType.NAMED_STATIC_CONSTANT_TYPE_USAGE)

        | SynType.Fun(synType1, synType2, _, _) ->
            x.MarkTypes(synType1, synType2, range, ElementType.FUNCTION_TYPE_USAGE)

        | SynType.WithGlobalConstraints(synType, constraints, _) ->
            let mark = x.Mark(range)
            x.ProcessType(synType)
            x.ProcessConstraintsClause(constraints)
            x.Done(range, mark, ElementType.CONSTRAINED_TYPE_USAGE)

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

        | SynType.StaticConstantExpr(expr, _) ->
            let mark = x.Mark(range)
            x.MarkChameleonExpression(expr)
            x.Done(range, mark, ElementType.EXPR_STATIC_CONSTANT_TYPE_USAGE)

        | SynType.StaticConstant(synConst, constRange) ->
            let mark = x.Mark(range)
            x.MarkChameleonExpression(SynExpr.Const(synConst, constRange))
            x.Done(range, mark, ElementType.EXPR_STATIC_CONSTANT_TYPE_USAGE)

        | SynType.Anon _ ->
            x.MarkAndDone(range, ElementType.ANON_TYPE_USAGE)

        | SynType.Paren(innerType, range) ->
            let mark = x.Mark(range)
            x.ProcessType(innerType)
            x.Done(range, mark, ElementType.PAREN_TYPE_USAGE)

        | SynType.SignatureParameter(_, _, _, synType, _) ->
            x.ProcessType(synType)

        | SynType.Or(lhsType, rhsType, range, _) ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(lhsType)
            x.ProcessTypeAsTypeReferenceName(rhsType)
            x.Done(range, mark, ElementType.OR_TYPE_USAGE)

    member x.MarkTypes(synType1, synType2, range: range, elementType) =
        let mark = x.Mark(range)
        x.ProcessType(synType1)
        x.ProcessType(synType2)
        x.Done(range, mark, elementType)

    member x.ProcessTypeConstraint(typeConstraint: SynTypeConstraint) =
        let range = typeConstraint.Range
        let mark = x.Mark(range)

        match typeConstraint with
        | SynTypeConstraint.WhereTyparIsValueType(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.VALUE_TYPE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsReferenceType(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.REFERENCE_TYPE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsUnmanaged(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.UNMANAGED_TYPE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparSupportsNull(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.NULL_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsComparable(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.COMPARABLE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsEquatable(typeParameter, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.Done(range, mark, ElementType.EQUATABLE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparDefaultsToType(typeParameter, synType, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.DEFAULTS_TO_CONSTRAINT)

        | SynTypeConstraint.WhereTyparSubtypeOfType(typeParameter, synType, _) ->
            x.ProcessTypeParameter(typeParameter)
            x.ProcessType(synType)
            x.Done(range, mark, ElementType.SUBTYPE_CONSTRAINT)

        | SynTypeConstraint.WhereTyparSupportsMember(synType, memberSig, _) ->
            let rec (|OrTypes|) t =
                match t with
                | SynType.Or(lhsType, OrTypes types, _, _) -> lhsType :: types
                | _ -> [t]

            match synType with
            | SynType.Paren(OrTypes types, _) ->
                for synType in types do
                    x.ProcessTypeAsTypeReferenceName(synType)
            | _ -> x.ProcessTypeAsTypeReferenceName(synType)

            match memberSig with
            | SynMemberSig.Member _ ->
                x.ProcessTypeMemberSignature(memberSig)
            | _ -> ()

            x.Done(range, mark, ElementType.MEMBER_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsEnum(typeParameter, synTypes, _) ->
            x.ProcessTypeParameter(typeParameter)
            for synType in synTypes do
                x.ProcessType(synType)
            x.Done(range, mark, ElementType.ENUM_CONSTRAINT)

        | SynTypeConstraint.WhereTyparIsDelegate(typeParameter, synTypes, _) ->
            x.ProcessTypeParameter(typeParameter)
            for synType in synTypes do
                x.ProcessType(synType)
            x.Done(range, mark, ElementType.DELEGATE_CONSTRAINT)

        | SynTypeConstraint.WhereSelfConstrained(synType, range) ->
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(range, mark, ElementType.SELF_CONSTRAINT)

    member x.ProcessTypeParameter(SynTypar(IdentRange range, _, _)) =
        let mark = x.Mark(range)
        x.MarkAndDone(range, ElementType.TYPE_PARAMETER_ID)
        x.Done(range, mark, ElementType.TYPE_REFERENCE_NAME)

    member x.ProcessAccessorsNamesClause(trivia: SynValSigTrivia, memberRange) =
        match trivia.WithKeyword with
        | None -> ()
        | Some withRange ->

        let accessorsMark = x.Mark(withRange)
        x.Done(memberRange, accessorsMark, ElementType.ACCESSORS_NAMES_CLAUSE)

    member x.ProcessTypeMemberSignature(memberSig) =
        match memberSig with
        | SynMemberSig.Member(SynValSig(attrs, _, _, synType, arity, _, _, XmlDoc xmlDoc, _, _, _, trivia), flags, range, _) ->
            let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)
            x.ProcessReturnTypeInfo(arity, synType)
            let elementType =
                if flags.IsDispatchSlot then
                    x.ProcessAccessorsNamesClause(trivia, range)
                    ElementType.ABSTRACT_MEMBER_DECLARATION
                else
                    match flags.MemberKind with
                    | SynMemberKind.Constructor -> ElementType.CONSTRUCTOR_SIGNATURE
                    | _ -> ElementType.MEMBER_SIGNATURE
            x.Done(range, mark, elementType)

        | SynMemberSig.ValField(SynField(attrs, _, id, synType, _, XmlDoc xmlDoc, _, _, _), range) ->
            if id.IsSome then
                let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)
                x.ProcessType(synType)
                x.Done(mark,ElementType.VAL_FIELD_DECLARATION)

        | SynMemberSig.Inherit(synType, range) ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(mark, ElementType.INTERFACE_INHERIT)

        | SynMemberSig.Interface(synType, range) ->
            let mark = x.Mark(range)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.Done(mark, ElementType.INTERFACE_IMPLEMENTATION)

        | _ -> ()

    member x.FixExpression(expr: SynExpr) =
        // A fake SynExpr.Typed node is added for binding with return type specification like in the following
        // member x.Prop: int = 1
        // where 1 is replaced with `1: int`.
        // These fake nodes have original type specification ranges that are out of the actual expression ranges.
        match expr with
        | SynExpr.Typed(inner, synType, range) when not (Range.rangeContainsRange range synType.Range) -> inner
        | _ -> expr

    member x.RemoveDoExpr(expr: SynExpr) =
        match expr with
        | SynExpr.Do(expr, _) -> expr
        | _ -> expr

    member x.GetConstElementType(synConst) =
        match synConst with
        | SynConst.Unit -> ElementType.UNIT_EXPR
        | _ -> ElementType.LITERAL_EXPR
    
    member x.MarkChameleonExpression(expr: SynExpr) =
        let ExprRange range as expr = x.FixExpression(expr)

        let isInternalMode () =
            let productConfigurations = Shell.Instance.GetComponent<RunsProducts.ProductConfigurations>()
            productConfigurations.IsInternalMode()

        let dumpFileContent () =
            if isInternalMode () then $"\nContent: {document.GetText()}" else ""

        let startOffset = x.GetStartOffset(range)
        let mark = x.Mark(startOffset)

        if x.CurrentOffset <> startOffset then
            Assertion.Fail($"Can't convert FCS tree expression in {path} at {range}.{dumpFileContent ()}")

        // Replace all tokens with single chameleon token.
        let tokenMark = x.Mark(range)
        x.AdvanceToEnd(range)
        x.Builder.AlterToken(tokenMark, FSharpTokenType.CHAMELEON)

        let lineStart = lineOffsets[range.StartLine - 1]
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

    member x.ProcessTypeParametersAndConstraints(typeParams, constraints, lid) =
        match typeParams with
        | Some(SynTyparDecls.PrefixList _ | SynTyparDecls.SinglePrefix _ as typeParams) ->
            x.ProcessTypeParameters(typeParams, true)
            x.ProcessTypeReferenceNameSkipLast(lid)
        | Some(typeParams) ->
            x.ProcessTypeReferenceNameSkipLast(lid)
            x.ProcessTypeParameters(typeParams, true)
        | _ ->
            x.ProcessTypeReferenceNameSkipLast(lid)

        for typeConstraint in constraints do
            x.ProcessTypeConstraint(typeConstraint)
