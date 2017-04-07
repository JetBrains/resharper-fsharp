namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open System
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util.dataStructures.TypedIntrinsics
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast

[<AbstractClass>]
type FSharpTreeBuilderBase(file : IPsiSourceFile, lexer : ILexer, lifetime) as this =
    inherit TreeStructureBuilderBase(lifetime)

    let document = file.Document

    abstract member CreateFSharpFile : unit -> ICompositeElement

    member internal x.GetLineOffset line = document.GetLineStartOffset(line - 1 |> Int32.op_Explicit)
    member internal x.GetStartOffset (range : Range.range) = x.GetLineOffset range.StartLine + range.StartColumn
    member internal x.GetEndOffset (range : Range.range) = x.GetLineOffset range.EndLine + range.EndColumn
    member internal x.GetStartOffset (id : Ident) = x.GetStartOffset id.idRange
    member internal x.Eof = x.Builder.Eof()

    member internal x.AdvanceToOffset offset =
        while x.Builder.GetTokenOffset() < offset && not x.Eof do x.Builder.AdvanceLexer() |> ignore

    member internal x.AdvanceToKeywordOrOffset (keywordType : TokenNodeType) (maxOffset : int) =
        while x.Builder.GetTokenOffset() < maxOffset &&
              (not (x.Builder.GetTokenType().IsKeyword) || x.Builder.GetTokenType() <> keywordType) do
            x.Builder.AdvanceLexer() |> ignore

    member internal x.ProcessIdentifier (id : Ident) =
        let range = id.idRange
        x.AdvanceToOffset (x.GetStartOffset range)
        let mark = x.Builder.Mark()
        x.AdvanceToOffset (x.GetEndOffset range)
        x.Done(mark, ElementType.F_SHARP_IDENTIFIER)

    member internal x.ProcessLongIdentifier (lid : Ident list) =
        if not lid.IsEmpty then
            lid.Head.idRange |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            (List.last lid).idRange |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LONG_IDENTIFIER)

    member internal x.FinishFile mark fileType =
        while not x.Eof do x.Builder.AdvanceLexer() |> ignore
        x.Done(mark, fileType)
        x.GetTree() :> ICompositeElement

    member internal x.StartTopLevelDeclaration (lid : LongIdent) isModule =
        let firstId = lid.Head
        let idRange = firstId.idRange
        if idRange.Start <> idRange.End then
            // Missing ident may be replaced with file name with range 1,0-1,0.

            // Ast namespace range starts after its identifier,
            // try to locate a keyword followed by access modifiers
            let keywordTokenType = if isModule then FSharpTokenType.MODULE else FSharpTokenType.NAMESPACE
            x.GetStartOffset firstId |> x.AdvanceToKeywordOrOffset keywordTokenType

        let mark = x.Builder.Mark()
        if idRange.Start <> idRange.End then x.Builder.AdvanceLexer() |> ignore // skip keyword

        if isModule then x.ProcessModifiersBeforeOffset (x.GetStartOffset firstId)
        x.ProcessLongIdentifier lid
        mark

    member internal x.FinishTopLevelDeclaration mark range isModule =
        range |> x.GetEndOffset |> x.AdvanceToOffset
        let elementType =
            if isModule
            then ElementType.TOP_LEVEL_MODULE_DECLARATION
            else ElementType.F_SHARP_NAMESPACE_DECLARATION
        x.Done(mark, elementType)

    member internal x.ProcessAttributesAndStartRange (attrs : SynAttributes) (range : Range.range) =
        if attrs.IsEmpty then
            range |> x.GetStartOffset |> x.AdvanceToOffset
            x.Builder.Mark()
        else
            attrs.Head.Range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            for attr in attrs do x.ProcessAttribute attr
            mark

    member internal x.StartNestedModule (attrs : SynAttributes) (lid : LongIdent) (range : Range.range) =
        let mark = x.ProcessAttributesAndStartRange attrs range
        x.Builder.AdvanceLexer() |> ignore // skip keyword
        if not lid.IsEmpty then
            let id = lid.Head
            x.ProcessModifiersBeforeOffset (x.GetStartOffset id)
            x.ProcessIdentifier id
        mark

    member internal x.ProcessException (SynExceptionDefnRepr(_,UnionCase(_,id,_,_,_,_),_,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.Builder.AdvanceLexer() |> ignore // skip keyword
        x.ProcessModifiersBeforeOffset  (x.GetStartOffset id)
        x.ProcessIdentifier id

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Builder.Done(mark, ElementType.F_SHARP_EXCEPTION_DECLARATION, null)

    member internal x.ProcessModifiersBeforeOffset (endOffset : int) =
        let mark = x.Builder.Mark()
        x.AdvanceToOffset endOffset
        x.Builder.Done(mark, ElementType.ACCESS_MODIFIERS, null)

    member internal x.StartType attrs typeParams (lid : LongIdent) range =
        let mark = x.ProcessAttributesAndStartRange attrs range
        if not lid.IsEmpty then
            let id = lid.Head
            let idOffset = x.GetStartOffset id

            let typeParamsOffset =
                match List.tryHead typeParams with
                | Some (TyparDecl(_,(Typar(id,_,_)))) -> x.GetStartOffset id
                | None -> idOffset

            x.ProcessModifiersBeforeOffset (min idOffset typeParamsOffset)
            if idOffset < typeParamsOffset then
                x.ProcessIdentifier id
                for p in typeParams do x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION
            else
                for p in typeParams do x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION
                x.ProcessIdentifier id
        mark

    member internal x.ProcessTypeParameter (TyparDecl(_,(Typar(id,_,_)))) elementType =
        id |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.ProcessIdentifier id
        x.Done(mark, elementType)

    member internal x.ProcessUnionCaseType caseType =
        match caseType with
        | UnionCaseFields(fields) ->
            for f in fields do x.ProcessField f

        | UnionCaseFullType(_) ->
            () // todo: used in FSharp.Core only, otherwise warning

    member internal x.ProcessUnionCase (UnionCase(_,id,caseType,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()

        x.ProcessIdentifier id
        x.ProcessUnionCaseType caseType

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_UNION_CASE_DECLARATION)

    member internal x.ProcessAttributeArg (expr : SynExpr) =
        match expr with
        | SynExpr.LongIdent(_,lid,_,_) -> x.ProcessLongIdentifier lid.Lid
        | SynExpr.Paren(expr,_,_,_) -> x.ProcessAttributeArg expr
        | _ -> () // we need to cover only these cases for now

    member internal x.ProcessAttribute (attr : SynAttribute) =
        x.AdvanceToOffset (x.GetStartOffset attr.Range)
        let mark = x.Builder.Mark()
        x.ProcessLongIdentifier attr.TypeName.Lid

        let argExpr = attr.ArgExpr
        argExpr.Range.StartRange |> x.GetStartOffset |> x.AdvanceToOffset
        let argMark = x.Builder.Mark()
        x.ProcessAttributeArg attr.ArgExpr
        argExpr.Range.EndRange |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(argMark, ElementType.ARG_EXPRESSION)

        x.AdvanceToOffset (x.GetEndOffset attr.Range)
        x.Done(mark, ElementType.F_SHARP_ATTRIBUTE)

    member internal x.ProcessEnumCase (EnumCase(_,id,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.ProcessIdentifier id

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_ENUM_MEMBER_DECLARATION)

    member internal x.ProcessField (Field(_,_,id,_,_,_,_,range)) =
        let mark =
            match id with
            | Some id ->
                let startOffset = min (x.GetStartOffset id) (x.GetStartOffset range)
                x.AdvanceToOffset startOffset
                let mark = x.Builder.Mark()
                x.ProcessIdentifier id
                mark
            | None ->
                range |> x.GetStartOffset |> x.AdvanceToOffset
                x.Builder.Mark()

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_FIELD_DECLARATION)

    member internal x.ProcessLocalId (id : Ident) =
        id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Mark()
        x.ProcessIdentifier id
        x.Done(mark, ElementType.LOCAL_DECLARATION)

    member internal x.ProcessSimplePattern (pat : SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,_)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),_,_) ->
            x.ProcessLocalId id
        | _ -> ()

    member internal x.ProcessImplicitCtorParam (pat : SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,range)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),_,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessLocalId id
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark,ElementType.MEMBER_PARAM)
        | _ -> ()

    member internal x.ProcessTypeMemberTypeParams (SynValTyparDecls(typeParams,_,_)) =
        for param in typeParams do
            x.ProcessTypeParameter param ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION

    member internal x.ProcessMemberDeclaration id (typeParamsOpt : SynValTyparDecls option) memberParams expr =
        x.ProcessIdentifier id
        if typeParamsOpt.IsSome then x.ProcessTypeMemberTypeParams typeParamsOpt.Value
        x.ProcessMemberParams memberParams
        x.ProcessLocalExpression expr

    member internal x.ProcessTypeMember (typeMember : SynMemberDefn) =
        typeMember.Range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()

        let memberType =
            match typeMember with
            | SynMemberDefn.ImplicitCtor(_,_,args,selfId,_) ->
                for arg in args do
                    x.ProcessImplicitCtorParam arg
                if selfId.IsSome then x.ProcessLocalId selfId.Value
                ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

            | SynMemberDefn.ImplicitInherit(SynType.LongIdent(lidWithDots),_,_,_) ->
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.TYPE_INHERIT

            | SynMemberDefn.Interface(SynType.LongIdent(lidWithDots),interfaceMembersOpt,_) ->
                if interfaceMembersOpt.IsSome then
                    for m in interfaceMembersOpt.Value do
                        x.ProcessTypeMember m
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.INTERFACE_IMPLEMENTATION

            | SynMemberDefn.Inherit(SynType.LongIdent(lidWithDots),_,_) ->
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.INTERFACE_INHERIT

            | SynMemberDefn.Member(Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_),_) ->
                match headPat with
                | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOpt,memberParams,_,_) ->
                    match lid with
                    | [id] when id.idText = "new" ->
                        x.ProcessLocalParams memberParams
                        x.ProcessLocalExpression expr
                        ElementType.CONSTRUCTOR_DECLARATION

                    | [id] ->
                        x.ProcessMemberDeclaration id typeParamsOpt memberParams expr
                        ElementType.MEMBER_DECLARATION

                    | selfId :: id :: _ ->
                        x.ProcessLocalId selfId
                        x.ProcessMemberDeclaration id typeParamsOpt memberParams expr
                        ElementType.MEMBER_DECLARATION

                    | _ -> ElementType.OTHER_TYPE_MEMBER
                | _ -> ElementType.OTHER_TYPE_MEMBER

            | SynMemberDefn.LetBindings(bindings,_,_,_) ->
                for binding in bindings do
                    x.ProcessLocalBinding binding
                ElementType.TYPE_LET_BINDINGS

            | SynMemberDefn.AbstractSlot(ValSpfn(_,id,typeParams,_,_,_,_,_,_,_,_),_,_) as slot ->
                x.ProcessIdentifier id
                x.ProcessTypeMemberTypeParams typeParams
                ElementType.ABSTRACT_SLOT

            | SynMemberDefn.ValField(Field(_,_,id,_,_,_,_,_),_) ->
                if id.IsSome then x.ProcessIdentifier id.Value
                ElementType.VAL_FIELD

            | SynMemberDefn.AutoProperty(_,_,id,_,_,_,_,_,_,_,_) ->
                x.ProcessIdentifier id
                ElementType.AUTO_PROPERTY

            | _ -> ElementType.OTHER_TYPE_MEMBER

        x.AdvanceToOffset (x.GetEndOffset typeMember.Range)
        x.Done(mark, memberType)

    member internal x.ProcessMemberParams (memberParams : SynConstructorArgs) =
        match memberParams with
        | Pats(pats) ->
            for pat in pats do
                x.ProcessMemberParamPat pat
        | NamePatPairs(idsAndPats,_) ->
            for (id, pat) in idsAndPats do
                x.ProcessMemberParamPat pat

    member internal x.ProcessMemberParamPat (pat : SynPat) =
        pat.Range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.ProcessLocalPat pat
        pat.Range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark,ElementType.MEMBER_PARAM)

    member internal x.ProcessLocalParams (args : SynConstructorArgs) =
        match args with
        | Pats(pats) ->
            for pat in pats do
                x.ProcessLocalPat pat
        | NamePatPairs(idsAndPats,_) ->
            for (id, pat) in idsAndPats do
                x.ProcessLocalPat pat

    member internal x.ProcessLocalPat (pat : SynPat) =
        match pat with
        | SynPat.LongIdent(_,_,_,patParams,_,range) ->
            x.ProcessLocalParams patParams
        | SynPat.Named(_,id,_,_,_)
        | SynPat.OptionalVal(id,_) ->
            id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessIdentifier id
            x.Done(mark, ElementType.LOCAL_DECLARATION)
        | SynPat.Tuple(patterns,_)
        | SynPat.StructTuple(patterns,_) ->
            for pat in patterns do
                x.ProcessLocalPat pat
        | SynPat.Paren(pat,_)
        | SynPat.Typed(pat,_,_) ->
            x.ProcessLocalPat pat
        | SynPat.Ands(pats,_)
        | SynPat.ArrayOrList(_,pats,_) ->
            for pat in pats do
                x.ProcessLocalPat pat
        | SynPat.Or(pat1,pat2,_) ->
            x.ProcessLocalPat pat1
            x.ProcessLocalPat pat2
        | SynPat.QuoteExpr(expr,_) ->
            x.ProcessLocalExpression expr
        | _ -> ()

    member internal x.ProcessLocalBinding (Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_)) =
        x.ProcessLocalPat headPat
        x.ProcessLocalExpression expr

    member internal x.ProcessMatchClause (Clause(pat,exprOpt,expr,_,_)) =
        x.ProcessLocalPat pat
        x.ProcessLocalExpression expr

    member internal x.ProcessSimplePatterns (pats : SynSimplePats) =
        match pats with
        | SynSimplePats.SimplePats(pats,_) ->
            for p in pats do x.ProcessSimplePattern p
        | SynSimplePats.Typed(pats,_,_) ->
            x.ProcessSimplePatterns pats

    member internal x.ProcessLocalExpression (expr : SynExpr) =
        match expr with
        | SynExpr.Paren(expr,_,_,_)
        | SynExpr.Quote(_,_,expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Const(_) -> ()

        | SynExpr.Typed(expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Tuple(exprs,_,_)
        | SynExpr.StructTuple(exprs,_,_)
        | SynExpr.ArrayOrList(_,exprs,_) ->
            for e in exprs do
                x.ProcessLocalExpression e

        | SynExpr.Record(_) -> () // todo

        | SynExpr.New(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.ObjExpr(_) -> () // todo

        | SynExpr.While(_,whileExpr,doExpr,_) ->
            x.ProcessLocalExpression whileExpr
            x.ProcessLocalExpression doExpr

        | SynExpr.For(_,id,idBody,_,toBody,doBody,_) ->
            x.ProcessLocalId id
            x.ProcessLocalExpression idBody
            x.ProcessLocalExpression toBody
            x.ProcessLocalExpression doBody

        | SynExpr.ForEach(_,_,_,pat,enumExpr,bodyExpr,_) ->
            x.ProcessLocalPat pat
            x.ProcessLocalExpression enumExpr
            x.ProcessLocalExpression bodyExpr

        | SynExpr.ArrayOrListOfSeqExpr(_,expr,_)
        | SynExpr.CompExpr(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Lambda(_,_,pats,expr,_) ->
            x.ProcessSimplePatterns pats
            x.ProcessLocalExpression expr

        | SynExpr.MatchLambda(_,_,cases,_,_) ->
            for case in cases do
                x.ProcessMatchClause case

        | SynExpr.Match(_,expr,cases,_,_) ->
            x.ProcessLocalExpression expr
            for case in cases do
                x.ProcessMatchClause case

        | SynExpr.Do(expr,_)
        | SynExpr.Assert(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.App(_,_,funExpr,argExpr,_) ->
            x.ProcessLocalExpression funExpr
            x.ProcessLocalExpression argExpr

        | SynExpr.TypeApp(_) -> ()

        | SynExpr.LetOrUse(_,_,bindings,bodyExpr,_) ->
            for binding in bindings do
                    x.ProcessLocalBinding binding
            x.ProcessLocalExpression bodyExpr

        | SynExpr.TryWith(tryExpr,_,withCases,_,_,_,_) ->
            x.ProcessLocalExpression tryExpr
            for case in withCases do
                x.ProcessMatchClause case

        | SynExpr.TryFinally(tryExpr,finallyExpr,_,_,_) ->
            x.ProcessLocalExpression tryExpr
            x.ProcessLocalExpression finallyExpr

        | SynExpr.Lazy(expr,_) -> x.ProcessLocalExpression expr

        | SynExpr.Sequential(_,_,expr1,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | SynExpr.IfThenElse(ifExpr,thenExpr,elseExprOpt,_,_,_,_) ->
            x.ProcessLocalExpression ifExpr
            x.ProcessLocalExpression thenExpr
            if elseExprOpt.IsSome then
                x.ProcessLocalExpression elseExprOpt.Value

        | SynExpr.Ident(_)
        | SynExpr.LongIdent(_)
        | SynExpr.LongIdentSet(_)
        | SynExpr.DotGet(_)
        | SynExpr.DotSet(_)
        | SynExpr.DotIndexedGet(_)
        | SynExpr.DotIndexedSet(_)
        | SynExpr.NamedIndexedPropertySet(_)
        | SynExpr.DotNamedIndexedPropertySet(_) -> ()

        | SynExpr.TypeTest(expr,_,_)
        | SynExpr.Upcast(expr,_,_)
        | SynExpr.Downcast(expr,_,_)
        | SynExpr.InferredUpcast(expr,_)
        | SynExpr.InferredDowncast(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Null(_) -> ()

        | SynExpr.AddressOf(_,expr,_,_)
        | SynExpr.TraitCall(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.JoinIn(expr1,_,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | SynExpr.ImplicitZero(_) -> ()

        | SynExpr.YieldOrReturn(_,expr,_)
        | SynExpr.YieldOrReturnFrom(_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.LetOrUseBang(_,_,_,pat,expr,inExpr,_) ->
            x.ProcessLocalPat pat
            x.ProcessLocalExpression expr
            x.ProcessLocalExpression inExpr

        | SynExpr.DoBang(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.LibraryOnlyStaticOptimization(_)
        | SynExpr.LibraryOnlyUnionCaseFieldGet(_)
        | SynExpr.LibraryOnlyUnionCaseFieldSet(_)
        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.ArbitraryAfterError(_)
        | SynExpr.FromParseError(_)
        | SynExpr.DiscardAfterMissingQualificationAfterDot(_) -> ()

        | SynExpr.Fixed(expr,_) ->
            x.ProcessLocalExpression expr

    override val Builder = PsiBuilder(lexer, ElementType.F_SHARP_IMPL_FILE, this, lifetime)
    override val NewLine = FSharpTokenType.NEW_LINE
    override val CommentsOrWhiteSpacesTokens = FSharpTokenType.CommentsOrWhitespaces
    override x.GetExpectedMessage name  = NotImplementedException() |> raise

    interface IPsiBuilderTokenFactory with
        member x.CreateToken(tokenType, buffer, startOffset, endOffset) =
            tokenType.Create(buffer, TreeOffset(startOffset), TreeOffset(endOffset))
