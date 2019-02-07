namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System
open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

[<AbstractClass>]
type FSharpTreeBuilderBase(sourceFile: IPsiSourceFile, lexer: ILexer, lifetime: Lifetime) =
    inherit TreeBuilderBase(lifetime, lexer)

    let document = sourceFile.Document

    let lineOffsets =
        let lineCount = document.GetLineCount()
        Array.init (int lineCount) (fun line -> document.GetLineStartOffset(docLine line))

    let getLineOffset line =
        lineOffsets.[line - 1]

    abstract member CreateFSharpFile: unit -> IFSharpFile

    member x.GetOffset(pos: Range.pos) = getLineOffset pos.Line + pos.Column
    member x.GetStartOffset(range: Range.range) = getLineOffset range.StartLine + range.StartColumn
    member x.GetEndOffset(range: Range.range) = getLineOffset range.EndLine + range.EndColumn
    member x.GetStartOffset(IdentRange range) = x.GetStartOffset(range)

    member x.Eof = x.Builder.Eof()

    override x.SkipWhitespaces() = ()

    member val TypeExtensionsOffsets = OneToListMap<string, int>()

    member x.Advance() = x.Builder.AdvanceLexer() |> ignore
    
    member x.AdvanceToStart(range: Range.range) =
        x.AdvanceToOffset(x.GetStartOffset(range))

    member x.AdvanceToEnd(range: Range.range) =
        x.AdvanceToOffset(x.GetEndOffset(range))

    member x.AdvanceTo(pos: Range.pos) =
        x.AdvanceToOffset(x.GetOffset(pos))

    member x.Mark(range: Range.range) =
        x.AdvanceToStart(range)
        x.Mark()

    member x.Mark(pos: Range.pos) =
        x.AdvanceTo(pos)
        x.Mark()
    
    member x.Done(range, mark, elementType) =
        x.AdvanceToEnd(range)
        x.Done(mark, elementType)

    member x.MarkAndDone(range: Range.range, elementType) =
        let mark = x.Mark(range)
        x.Done(range, mark, elementType)

    member x.MarkToken(elementType) =
        let caseMark = x.Builder.Mark()
        x.Advance()
        x.Done(caseMark, elementType)

    member x.AdvanceToOffset offset =
        while x.Builder.GetTokenOffset() < offset && not x.Eof do x.Advance()

    member x.AdvanceToTokenOrOffset (keywordType: TokenNodeType) (maxOffset: int) =
        while x.Builder.GetTokenOffset() < maxOffset && x.Builder.GetTokenType() != keywordType do
            x.Advance()

    member x.ProcessLongIdentifier(lid: Ident list) =
        match lid with
        | [] -> ()
        | head :: _ ->
            let mark = x.Mark(head.idRange)
            let last = List.last lid
            x.Done(last.idRange, mark, ElementType.LONG_IDENTIFIER)

    member x.FinishFile(mark, fileType) =
        while not x.Eof do x.Advance()
        x.Done(mark, fileType)
        let fsFile = x.GetTree() :> ITreeNode :?> IFSharpFile
        fsFile.TypeExtensionsOffsets <- x.TypeExtensionsOffsets
        fsFile

    member x.StartTopLevelDeclaration (lid: LongIdent) (attrs: SynAttributes) isModule (range: Range.range) =
        match lid.IsEmpty, isModule with
        | false, _ ->
            let firstId = lid.Head
            let idRange = firstId.idRange
            let mark = 
                if attrs.IsEmpty then
                    if idRange.Start <> idRange.End then 
                        // Missing ident may be replaced with file name with range 1,0-1,0.

                        // Ast namespace range starts after its identifier,
                        // try to locate the keyword followed by access modifiers
                        let keywordTokenType = if isModule then FSharpTokenType.MODULE else FSharpTokenType.NAMESPACE
                        x.GetStartOffset firstId |> x.AdvanceToTokenOrOffset keywordTokenType
                    x.Builder.Mark()
                else
                    x.ProcessAttributesAndStartRange attrs (Some firstId) range

            if isModule then x.ProcessModifiersBeforeOffset (x.GetStartOffset firstId)
            x.ProcessLongIdentifier lid
            let elementType =
                if isModule
                then ElementType.TOP_LEVEL_MODULE_DECLARATION
                else ElementType.F_SHARP_NAMESPACE_DECLARATION
            Some mark, elementType
        | _, false ->
            // global namespace or parse error
            x.GetStartOffset range |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.Done(x.Builder.Mark(), ElementType.LONG_IDENTIFIER)
            Some mark, ElementType.F_SHARP_GLOBAL_NAMESPACE_DECLARATION
        | _ -> None, null

    member x.FinishTopLevelDeclaration (mark: int option) range elementType =
        range |> x.GetEndOffset |> x.AdvanceToOffset
        if mark.IsSome then
            x.Done(mark.Value, elementType)

    member x.ProcessAttributesAndStartRange (attrs: SynAttributes) (id: Ident option) (range: Range.range) =
        if attrs.IsEmpty then
            let rangeStartOffset = x.GetStartOffset range
            let startOffset = if id.IsSome then Math.Min(x.GetStartOffset id.Value.idRange, rangeStartOffset) else rangeStartOffset
            startOffset |> x.AdvanceToOffset
            x.Builder.Mark()
        else
            let mark = x.Mark(attrs.Head.Range)
            for attr in attrs do x.ProcessAttribute attr
            mark

    member x.StartNestedModule (attrs: SynAttributes) (lid: LongIdent) (range: Range.range) =
        let mark = x.ProcessAttributesAndStartRange attrs (List.tryHead lid) range
        if not lid.IsEmpty then
            let id = lid.Head
            x.ProcessModifiersBeforeOffset(x.GetStartOffset(id))
        mark

    member x.StartException (SynExceptionDefnRepr(_,UnionCase(_,id,unionCaseType,_,_,_),_,_,_,range)) =
        let mark = x.Mark(range)
        x.ProcessModifiersBeforeOffset(x.GetStartOffset id)
        x.ProcessUnionCaseType(unionCaseType) |> ignore
        mark

    member x.ProcessModifiersBeforeOffset (endOffset: int) =
        let mark = x.Builder.Mark()
        x.AdvanceToOffset endOffset
        x.Done(mark, ElementType.ACCESS_MODIFIERS)

    member x.StartType attrs typeParams (lid: LongIdent) range =
        let mark = x.ProcessAttributesAndStartRange attrs (List.tryHead lid) range
        if not lid.IsEmpty then
            let id = lid.Head
            let idOffset = x.GetStartOffset id

            let typeParamsOffset =
                match typeParams with
                | TyparDecl(_,(Typar(id,_,_))) :: _ -> x.GetStartOffset id
                | [] -> idOffset

            x.ProcessModifiersBeforeOffset (min idOffset typeParamsOffset)

            // Needs to advance past id range due to implicit ctor range in class includes id.
            let paramsInBraces = idOffset < typeParamsOffset
            if paramsInBraces then
                x.AdvanceToEnd(id.idRange)
                x.ProcessTypeParametersOfType typeParams range paramsInBraces
            else
                x.ProcessTypeParametersOfType typeParams range paramsInBraces
                x.AdvanceToEnd(id.idRange)
        mark

    member x.ProcessTypeParametersOfType typeParams range paramsInBraces =
        match typeParams with
        | TyparDecl(_,(Typar(id,_,_))) :: _ ->
            id.idRange |> x.GetStartOffset |> x.AdvanceToTokenOrOffset FSharpTokenType.LESS
            let mark = x.Mark()
            for p in typeParams do
                x.ProcessTypeParameter(p, ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION)
            if paramsInBraces then
                range |> x.GetEndOffset |> x.AdvanceToTokenOrOffset FSharpTokenType.GREATER
                if x.Builder.GetTokenType() == FSharpTokenType.GREATER then
                    x.Advance()
            x.Done(mark, ElementType.TYPE_PARAMETER_OF_TYPE_LIST)
        | [] -> ()

    member x.ProcessTypeParameter(TyparDecl(_,(Typar(IdentRange range,_,_))), elementType) =
        x.MarkAndDone(range, elementType)

    member x.ProcessUnionCaseType caseType =
        match caseType with
        | UnionCaseFields(fields) ->
            for f in fields do x.ProcessField f ElementType.UNION_CASE_FIELD_DECLARATION
            not fields.IsEmpty

        | UnionCaseFullType(_) ->
            true // todo: used in FSharp.Core only, otherwise warning

    member x.ProcessUnionCases(cases, range: Range.range) =
        let casesListMark = x.Mark(range)
        for case in cases do
            x.ProcessUnionCase(case)
        x.Done(range, casesListMark, ElementType.UNION_CASES_LIST)

    member x.ProcessUnionCase (UnionCase(_,id,caseType,_,_,range)) =
        let mark = x.Mark(range)
        let hasFields = x.ProcessUnionCaseType(caseType)
        let elementType = if hasFields then ElementType.NESTED_TYPE_UNION_CASE_DECLARATION
                                       else ElementType.SINGLETON_CASE_DECLARATION
        x.Done(range, mark, elementType)

    member x.ProcessAttributeArg (expr: SynExpr) =
        match expr with
        | SynExpr.LongIdent(_,lid,_,_) -> x.ProcessLongIdentifier lid.Lid
        | SynExpr.Paren(expr,_,_,_) -> x.ProcessAttributeArg expr
        | _ -> () // we need to cover only these cases for now

    member x.ProcessAttribute (attr: SynAttribute) =
        let mark = x.Mark(attr.Range)
        x.ProcessLongIdentifier attr.TypeName.Lid

        let argExpr = attr.ArgExpr
        let argMark = x.Mark(argExpr.Range.StartRange)
        x.ProcessAttributeArg attr.ArgExpr
        x.Done(argExpr.Range.EndRange, argMark, ElementType.ARG_EXPRESSION)

        x.Done(attr.Range, mark, ElementType.F_SHARP_ATTRIBUTE)

    member x.ProcessEnumCase (EnumCase(_,id,_,_,range)) =
        x.MarkAndDone(range, ElementType.ENUM_MEMBER_DECLARATION)

    member x.ProcessField (Field(_,_,id,t,_,_,_,range)) elementType =
        let mark =
            match id with
            | Some id ->
                x.AdvanceToOffset (min (x.GetStartOffset id) (x.GetStartOffset range))
                x.Mark()
            | None ->
                x.Mark(range)

        x.ProcessSynType t
        x.Done(range, mark, elementType)

    member x.ProcessLocalId(IdentRange range) =
        x.MarkAndDone(range, ElementType.LOCAL_DECLARATION)

    member x.ProcessSimplePattern (pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,isCompilerGenerated,_,_,_) ->
            if not isCompilerGenerated then
                x.ProcessLocalId id
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,isCompilerGenerated,_,_,_),t,_) ->
            if not isCompilerGenerated then
                x.ProcessLocalId id
            x.ProcessSynType t
        | _ -> ()

    member x.ProcessImplicitCtorParam (pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,range) ->
            let mark = x.Mark(range)
            x.ProcessLocalId id
            x.Done(range, mark,ElementType.MEMBER_PARAM)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),t,range) ->
            let mark = x.Mark(range)
            x.ProcessLocalId id
            x.ProcessSynType t
            x.Done(range, mark,ElementType.MEMBER_PARAM)
        | _ -> ()

    member x.ProcessTypeMemberTypeParams (SynValTyparDecls(typeParams,_,_)) =
        for param in typeParams do
            x.ProcessTypeParameter(param, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)

    member x.ProcessActivePatternId(IdentRange range, isLocal: bool) =
        let idMark = x.Mark(range)
        let endOffset = x.GetEndOffset(range)

        while x.Builder.GetTokenOffset() < endOffset do
            let caseElementType =
                let tokenType = x.Builder.GetTokenType()
                if tokenType == FSharpTokenType.IDENTIFIER then
                    if isLocal then ElementType.LOCAL_ACTIVE_PATTERN_CASE_DECLARATION
                    else ElementType.TOP_ACTIVE_PATTERN_CASE_DECLARATION

                elif tokenType == FSharpTokenType.UNDERSCORE then ElementType.ACTIVE_PATTERN_WILD_CASE else
                null

            if isNotNull caseElementType then
                x.MarkToken(caseElementType)

            x.Advance()

        x.Done(idMark, ElementType.ACTIVE_PATTERN_ID)

    member x.ProcessSimplePatterns(pats: SynSimplePats) =
        match pats with
        | SynSimplePats.SimplePats(pats,_) ->
            for p in pats do
                x.ProcessSimplePattern(p)

        | SynSimplePats.Typed(pats,t,_) ->
            x.ProcessSimplePatterns(pats)
            x.ProcessSynType(t)

    member x.ProcessTypeArgs(ltRange: Range.range, typeArgs, gtRange) =
        let mark = x.Mark(ltRange)
        for t in typeArgs do x.ProcessSynType t
        x.Done(gtRange, mark, ElementType.TYPE_ARGUMENT_LIST)

    member x.ProcessSynType(TypeRange range as synType) =
        match synType with
        | SynType.LongIdent(lid) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid.Lid)
            x.Done(range, mark, ElementType.NAMED_TYPE_EXPRESSION)

        | SynType.App(typeName,ltRange,typeArgs,_,gtRange,isPostfix,_) ->
            let mark = x.Mark(range)
            match typeName with
            | SynType.LongIdent(lid) -> x.ProcessLongIdentifier(lid.Lid)
            | _ -> ()

            match isPostfix, ltRange, gtRange with
            | false, Some ltRange, Some gtRange -> x.ProcessTypeArgs(ltRange, typeArgs, gtRange)
            | _ -> ()

            x.Done(range, mark, ElementType.NAMED_TYPE_EXPRESSION)

        | SynType.LongIdentApp(_,_,ltRange,typeArgs,_,gtRange,_) ->
            let mark = x.Mark(range)
            match ltRange, gtRange with
            | Some ltRange, Some gtRange -> x.ProcessTypeArgs(ltRange, typeArgs, gtRange)
            | _ -> () // todo: prefix? e.g. int list

            x.Done(range, mark, ElementType.NAMED_TYPE_EXPRESSION)

        | SynType.Tuple (types,_)
        | SynType.StructTuple (types,_) ->
            for _, t in types do
                x.ProcessSynType(t)

        | SynType.StaticConstantNamed(t1,t2,_)
        | SynType.MeasureDivide(t1,t2,_)
        | SynType.Fun(t1,t2,_) ->
            x.ProcessSynType(t1)
            x.ProcessSynType(t2)

        | SynType.WithGlobalConstraints(t,_,_)
        | SynType.HashConstraint(t,_)
        | SynType.MeasurePower(t,_,_)
        | SynType.Array(_,t,_) ->
            x.ProcessSynType(t)

        | SynType.StaticConstantExpr _
        | SynType.StaticConstant _
        | SynType.Anon _
        | SynType.Var _ -> ()
