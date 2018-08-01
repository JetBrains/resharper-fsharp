namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System
open System.Collections.Generic
open JetBrains.DataFlow
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.PrettyNaming

[<AbstractClass>]
type FSharpTreeBuilderBase(file: IPsiSourceFile, lexer: ILexer, lifetime: Lifetime) =
    inherit TreeBuilderBase(lifetime, lexer)

    let document = file.Document

    let rec (|Apps|_|) = function
        | SynExpr.App(_, true, expr, Apps ((cur, next: List<_>) as acc), _)
        | SynExpr.App(_, false, Apps ((cur, next) as acc), expr, _) ->
            next.Add(expr)
            Some acc

        | SynExpr.App(_, true, second, first, _) 
        | SynExpr.App(_, false, first, second, _) ->
            let list = List()
            list.Add(second)
            Some (first, list)

        | _ -> None

    abstract member CreateFSharpFile: unit -> IFSharpFile

    member internal x.GetLineOffset line = document.GetLineStartOffset(line - 1 |> docLine)
    member internal x.GetStartOffset (range: Range.range) = x.GetLineOffset(range.StartLine) + range.StartColumn
    member internal x.GetEndOffset (range: Range.range) = x.GetLineOffset(range.EndLine) + range.EndColumn
    member internal x.GetStartOffset (id: Ident) = x.GetStartOffset(id.idRange)
    member internal x.Eof = x.Builder.Eof()

    member val TypeExtensionsOffsets = OneToListMap<string, int>()

    member internal x.AdvanceToOffset offset =
        while x.Builder.GetTokenOffset() < offset && not x.Eof do x.Builder.AdvanceLexer() |> ignore

    member internal x.AdvanceToTokenOrOffset (keywordType: TokenNodeType) (maxOffset: int) =
        while x.Builder.GetTokenOffset() < maxOffset && x.Builder.GetTokenType() <> keywordType do
            x.Builder.AdvanceLexer() |> ignore

    member internal x.ProcessIdentifier (id: Ident) =
        let range = id.idRange
        x.AdvanceToOffset (x.GetStartOffset range)
        let mark = x.Builder.Mark()
        x.AdvanceToOffset (x.GetEndOffset range)
        x.Done(mark, ElementType.F_SHARP_IDENTIFIER)

    member internal x.ProcessLongIdentifier (lid: Ident list) =
        if not lid.IsEmpty then
            lid.Head.idRange |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            (List.last lid).idRange |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LONG_IDENTIFIER)

    member internal x.FinishFile(mark, fileType) =
        while not x.Eof do x.Builder.AdvanceLexer() |> ignore
        x.Done(mark, fileType)
        let fsFile = x.GetTree() :> ICompositeElement :?> IFSharpFile
        fsFile.TypeExtensionsOffsets <- x.TypeExtensionsOffsets
        fsFile

    member internal x.StartTopLevelDeclaration (lid: LongIdent) (attrs: SynAttributes) isModule (range: Range.range) =
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

    member internal x.FinishTopLevelDeclaration (mark: int option) range elementType =
        range |> x.GetEndOffset |> x.AdvanceToOffset
        if mark.IsSome then
            x.Done(mark.Value, elementType)

    member internal x.ProcessAttributesAndStartRange (attrs: SynAttributes) (id: Ident option) (range: Range.range) =
        if attrs.IsEmpty then
            let rangeStartOffset = x.GetStartOffset range
            let startOffset = if id.IsSome then Math.Min(x.GetStartOffset id.Value.idRange, rangeStartOffset) else rangeStartOffset
            startOffset |> x.AdvanceToOffset
            x.Builder.Mark()
        else
            attrs.Head.Range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            for attr in attrs do x.ProcessAttribute attr
            mark

    member internal x.StartNestedModule (attrs: SynAttributes) (lid: LongIdent) (range: Range.range) =
        let mark = x.ProcessAttributesAndStartRange attrs (List.tryHead lid) range
        x.Builder.AdvanceLexer() |> ignore // skip keyword
        if not lid.IsEmpty then
            let id = lid.Head
            x.ProcessModifiersBeforeOffset (x.GetStartOffset id)
            x.ProcessIdentifier id
        mark

    member internal x.StartException (SynExceptionDefnRepr(_,UnionCase(_,id,unionCaseType,_,_,_),_,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.Builder.AdvanceLexer() |> ignore // skip keyword
        x.ProcessModifiersBeforeOffset(x.GetStartOffset id)
        x.ProcessIdentifier(id)
        x.ProcessUnionCaseType(unionCaseType) |> ignore
        mark

    member internal x.ProcessModifiersBeforeOffset (endOffset: int) =
        let mark = x.Builder.Mark()
        x.AdvanceToOffset endOffset
        x.Builder.Done(mark, ElementType.ACCESS_MODIFIERS, null)

    member internal x.StartType attrs typeParams (lid: LongIdent) range =
        let mark = x.ProcessAttributesAndStartRange attrs (List.tryHead lid) range
        if not lid.IsEmpty then
            let id = lid.Head
            let idOffset = x.GetStartOffset id

            let typeParamsOffset =
                match typeParams with
                | TyparDecl(_,(Typar(id,_,_))) :: _ -> x.GetStartOffset id
                | [] -> idOffset

            x.ProcessModifiersBeforeOffset (min idOffset typeParamsOffset)

            let paramsInBraces = idOffset < typeParamsOffset
            if paramsInBraces then
                x.ProcessIdentifier id
                x.ProcessTypeParametersOfType typeParams range paramsInBraces
            else
                x.ProcessTypeParametersOfType typeParams range paramsInBraces
                x.ProcessIdentifier id
        mark

    member internal x.ProcessTypeParametersOfType typeParams range paramsInBraces =
        if not typeParams.IsEmpty then
            match typeParams.Head with
            | TyparDecl(_,(Typar(id,_,_))) ->
                id.idRange |> x.GetStartOffset |> x.AdvanceToTokenOrOffset FSharpTokenType.LESS
                let mark = x.Mark()
                for p in typeParams do
                    x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION
                if paramsInBraces then
                    let greaterTokenType = FSharpTokenType.GREATER
                    range |> x.GetEndOffset |> x.AdvanceToTokenOrOffset greaterTokenType
                    if LanguagePrimitives.PhysicalEquality (x.Builder.GetTokenType()) greaterTokenType then
                        x.Builder.AdvanceLexer() |> ignore
                x.Done(mark, ElementType.TYPE_PARAMETER_OF_TYPE_LIST)

    member internal x.ProcessTypeParameter (TyparDecl(_,(Typar(id,_,_)))) elementType =
        id |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.ProcessIdentifier id
        x.Done(mark, elementType)

    member internal x.ProcessUnionCaseType caseType =
        match caseType with
        | UnionCaseFields(fields) ->
            for f in fields do x.ProcessField f
            not fields.IsEmpty

        | UnionCaseFullType(_) ->
            true // todo: used in FSharp.Core only, otherwise warning

    member internal x.ProcessUnionCases(cases, range: Range.range) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let casesListMark = x.Mark()
        for case in cases do
            x.ProcessUnionCase(case)
        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(casesListMark, ElementType.UNION_CASES_LIST)

    member internal x.ProcessUnionCase (UnionCase(_,id,caseType,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()

        x.ProcessIdentifier(id)
        let hasFields = x.ProcessUnionCaseType(caseType)
        let elementType = if hasFields then ElementType.NESTED_TYPE_UNION_CASE_DECLARATION
                                       else ElementType.SINGLETON_CASE_DECLARATION
        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, elementType)

    member internal x.ProcessAttributeArg (expr: SynExpr) =
        match expr with
        | SynExpr.LongIdent(_,lid,_,_) -> x.ProcessLongIdentifier lid.Lid
        | SynExpr.Paren(expr,_,_,_) -> x.ProcessAttributeArg expr
        | _ -> () // we need to cover only these cases for now

    member internal x.ProcessAttribute (attr: SynAttribute) =
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
        x.Done(mark, ElementType.ENUM_MEMBER_DECLARATION)

    member internal x.ProcessField (Field(_,_,id,t,_,_,_,range)) =
        let mark =
            match id with
            | Some id ->
                x.AdvanceToOffset (min (x.GetStartOffset id) (x.GetStartOffset range))
                let mark = x.Builder.Mark()
                x.ProcessIdentifier id
                mark
            | None ->
                range |> x.GetStartOffset |> x.AdvanceToOffset
                x.Builder.Mark()

        x.ProcessSynType t
        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.FIELD_DECLARATION)

    member internal x.ProcessLocalId (id: Ident) =
        id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Mark()
        x.ProcessIdentifier id
        x.Done(mark, ElementType.LOCAL_DECLARATION)

    member internal x.ProcessSimplePattern (pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,isCompilerGenerated,_,_,_) ->
            if not isCompilerGenerated then
                x.ProcessLocalId id
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,isCompilerGenerated,_,_,_),t,_) ->
            if not isCompilerGenerated then
                x.ProcessLocalId id
            x.ProcessSynType t
        | _ -> ()

    member internal x.ProcessImplicitCtorParam (pat: SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessLocalId id
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark,ElementType.MEMBER_PARAM)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),t,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessLocalId id
            x.ProcessSynType t
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark,ElementType.MEMBER_PARAM)
        | _ -> ()

    member internal x.ProcessTypeMemberTypeParams (SynValTyparDecls(typeParams,_,_)) =
        for param in typeParams do
            x.ProcessTypeParameter param ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION

    member internal x.ProcessMemberDeclaration id (typeParamsOpt: SynValTyparDecls option) memberParams expr range =
        x.ProcessIdentifier id
        match typeParamsOpt with
        | Some(SynValTyparDecls(typeParams,_,_)) ->
            x.ProcessTypeParametersOfType typeParams range true
        | _ -> ()
        x.ProcessMemberParams memberParams
        x.ProcessLocalExpression expr

    member internal x.GetMemberAttributes(typeMember: SynMemberDefn) =
        match typeMember with
        | SynMemberDefn.Member(Binding(_,_,_,_,attrs,_,_,_,_,_,_,_),_)
        | SynMemberDefn.AbstractSlot(ValSpfn(attrs,_,_,_,_,_,_,_,_,_,_),_,_)
        | SynMemberDefn.AutoProperty(attrs,_,_,_,_,_,_,_,_,_,_)
        | SynMemberDefn.ValField(Field(attrs,_,_,_,_,_,_,_),_) -> attrs
        | _ -> []

    member internal x.ProcessTypeMember (typeMember: SynMemberDefn) =
        let attrs = x.GetMemberAttributes typeMember
        let rangeStart = x.GetStartOffset typeMember.Range
        let isMember =
            match typeMember with
            | SynMemberDefn.Member(_) -> true
            | _ -> false

        if x.Builder.GetTokenOffset() <= rangeStart || (not isMember) then
            let mark = x.ProcessAttributesAndStartRange attrs None typeMember.Range

            let memberType =
                match typeMember with
                | SynMemberDefn.ImplicitCtor(_,_,args,selfId,_) ->
                    for arg in args do
                        x.ProcessImplicitCtorParam arg
                    if selfId.IsSome then x.ProcessLocalId selfId.Value
                    ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

                | SynMemberDefn.ImplicitInherit(baseType,_,_,_) ->
                    x.ProcessSynType baseType
                    ElementType.TYPE_INHERIT

                | SynMemberDefn.Interface(interfaceType,interfaceMembersOpt,range) ->
                    x.ProcessSynType interfaceType
                    match interfaceMembersOpt with
                    | Some members -> for m in members do x.ProcessTypeMember m
                    | _ -> () 
                    ElementType.INTERFACE_IMPLEMENTATION

                | SynMemberDefn.Inherit(baseInterface,_,_) ->
                    x.ProcessSynType baseInterface
                    ElementType.INTERFACE_INHERIT

                | SynMemberDefn.Member(Binding(_,_,_,_,attrs,_,valData,headPat,ret,expr,_,_),range) ->
                    let elType =
                        match headPat with
                        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOpt,memberParams,_,_) ->
                            match lid with
                            | [id] ->
                                match valData with
                                | SynValData(Some (flags),_,_) when flags.MemberKind = MemberKind.Constructor ->
                                    x.ProcessLocalParams memberParams
                                    x.ProcessLocalExpression expr
                                    ElementType.CONSTRUCTOR_DECLARATION
                                | _ ->
                                    x.ProcessMemberDeclaration id typeParamsOpt memberParams expr range
                                    ElementType.MEMBER_DECLARATION

                            | selfId :: id :: _ ->
                                x.ProcessLocalId selfId
                                x.ProcessMemberDeclaration id typeParamsOpt memberParams expr range
                                ElementType.MEMBER_DECLARATION

                            | _ -> ElementType.OTHER_TYPE_MEMBER
                        | _ -> ElementType.OTHER_TYPE_MEMBER
                    match ret with
                    | Some (SynBindingReturnInfo(t,_,_)) -> x.ProcessSynType t
                    | _ -> ()
                    elType

                | SynMemberDefn.LetBindings(bindings,_,_,_) ->
                    for (Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_)) in bindings do
                        x.ProcessLocalPat headPat
                        x.ProcessLocalExpression expr
                    ElementType.OTHER_TYPE_MEMBER

                | SynMemberDefn.AbstractSlot(ValSpfn(_,id,typeParams,_,_,_,_,_,_,_,_),_,range) as slot ->
                    x.ProcessIdentifier id
                    match typeParams with
                    | SynValTyparDecls(typeParams,_,_) ->
                        x.ProcessTypeParametersOfType typeParams range true
                    ElementType.ABSTRACT_SLOT

                | SynMemberDefn.ValField(Field(_,_,id,_,_,_,_,_),_) ->
                    if id.IsSome then x.ProcessIdentifier id.Value
                    ElementType.VAL_FIELD

                | SynMemberDefn.AutoProperty(_,_,id,_,_,_,_,_,expr,_,_) ->
                    x.ProcessIdentifier id
                    x.ProcessLocalExpression expr
                    ElementType.AUTO_PROPERTY

                | _ -> ElementType.OTHER_TYPE_MEMBER

            x.AdvanceToOffset (x.GetEndOffset typeMember.Range)
            x.Done(mark, memberType)

    member internal x.ProcessActivePatternId (s: Ident) =
        let range = s.idRange
        let endOffset = x.GetEndOffset range
        x.AdvanceToOffset (x.GetStartOffset range)
        let idMark = x.Builder.Mark()
        
        while x.Builder.GetTokenOffset() < endOffset do
            if x.Builder.GetTokenType() = FSharpTokenType.IDENTIFIER then
                let caseMark = x.Builder.Mark()
                let idMark = x.Builder.Mark()
                x.Builder.AdvanceLexer() |> ignore
                x.Done(idMark, ElementType.F_SHARP_IDENTIFIER)
                x.Done(caseMark, ElementType.ACTIVE_PATTERN_CASE_DECLARATION) 
            else x.Builder.AdvanceLexer() |> ignore

        x.Done(idMark, ElementType.F_SHARP_IDENTIFIER)

    member internal x.ProcessMemberParams (memberParams: SynConstructorArgs) =
        match memberParams with
        | Pats(pats) ->
            for pat in pats do
                x.ProcessMemberParamPat pat
        | NamePatPairs(idsAndPats,_) ->
            for (id, pat) in idsAndPats do
                x.ProcessMemberParamPat pat

    member internal x.ProcessMemberParamPat (pat: SynPat) =
        pat.Range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()
        x.ProcessLocalPat pat
        pat.Range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.MEMBER_PARAM)

    member internal x.ProcessLocalParams (args: SynConstructorArgs) =
        match args with
        | Pats(pats) ->
            for p in pats do x.ProcessLocalPat p
        | NamePatPairs(idsAndPats,_) ->
            for (id, pat) in idsAndPats do x.ProcessLocalPat pat

    member internal x.ProcessLocalPat (pat: SynPat) =
        match pat with
        | SynPat.LongIdent(lidWithDots,_,_,patParams,_,range) ->
            match lidWithDots.Lid with
            | [] -> ()
            | [id] when id.idText = "op_ColonColon" -> ()
            | lid ->
                for id in lid do
                    let isActivePattern = IsActivePatternName id.idText 
                    if isActivePattern then x.ProcessActivePatternId id else x.ProcessLocalId id
            x.ProcessLocalParams patParams
        | SynPat.Named(pat,id,_,_,_) ->
            x.ProcessLocalPat pat
            x.ProcessLocalId id
        | SynPat.OptionalVal(id,_) ->
            x.ProcessLocalId id
        | SynPat.Tuple(patterns,_)
        | SynPat.StructTuple(patterns,_) ->
            for pat in patterns do
                x.ProcessLocalPat pat
        | SynPat.Paren(pat,_) ->
            x.ProcessLocalPat pat
        | SynPat.Typed(pat,t,_) ->
            x.ProcessLocalPat pat
            x.ProcessSynType t
        | SynPat.Ands(pats,_)
        | SynPat.ArrayOrList(_,pats,_) ->
            for pat in pats do
                x.ProcessLocalPat pat
        | SynPat.Or(pat1,pat2,_) ->
            x.ProcessLocalPat pat1
            x.ProcessLocalPat pat2
        | SynPat.QuoteExpr(expr,_) ->
            x.ProcessLocalExpression expr
        | SynPat.IsInst(t,_) ->
            x.ProcessSynType t
        | SynPat.Attrib(pat,_,_) ->
            x.ProcessLocalPat pat
        | SynPat.Record(pats,_) ->
            for (_, pat) in pats do
                x.ProcessLocalPat(pat)
        | _ -> ()

    member internal x.ProcessLocalBinding (Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_)) =
        x.ProcessLocalPat headPat
        x.ProcessLocalExpression expr

    member internal x.ProcessMatchClause (Clause(pat,exprOpt,expr,_,_)) =
        x.ProcessLocalPat pat
        x.ProcessLocalExpression expr

    member internal x.ProcessSimplePatterns (pats: SynSimplePats) =
        match pats with
        | SynSimplePats.SimplePats(pats,_) ->
            for p in pats do x.ProcessSimplePattern p
        | SynSimplePats.Typed(pats,t,_) ->
            x.ProcessSimplePatterns pats
            x.ProcessSynType t

    member internal x.ProcessTypeArgs(ltRange: Range.range, typeArgs, gtRange) =
        ltRange |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Mark()
        for t in typeArgs do x.ProcessSynType t
        gtRange |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.TYPE_ARGUMENT_LIST)

    member internal x.ProcessSynType(synType: SynType) =
        let range = synType.Range
        match synType with
        | SynType.LongIdent(lid) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Mark()
            x.ProcessLongIdentifier lid.Lid
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NAMED_TYPE_EXPRESSION)
        
        | SynType.App(typeName,lt,typeArgs,_,gt,isPostfix,_) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Mark()
            match typeName with
            | SynType.LongIdent(lid) -> x.ProcessLongIdentifier lid.Lid | _ -> ()
            match isPostfix, lt, gt with
            | false, Some ltRange, Some gtRange -> x.ProcessTypeArgs(ltRange, typeArgs, gtRange) | _ -> ()
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NAMED_TYPE_EXPRESSION)

        | SynType.LongIdentApp(_,_,lt,typeArgs,_,gt,_) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Mark()
            match lt, gt with
            | Some ltRange, Some gtRange -> x.ProcessTypeArgs(ltRange, typeArgs, gtRange) | _ -> ()
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NAMED_TYPE_EXPRESSION)

        | SynType.Tuple (types,_)
        | SynType.StructTuple (types,_) ->
            for _,t in types do x.ProcessSynType t

        | SynType.StaticConstantNamed(t1, t2,_)
        | SynType.MeasureDivide(t1,t2,_)
        | SynType.Fun(t1,t2,_) ->
            x.ProcessSynType t1
            x.ProcessSynType t2

        | SynType.WithGlobalConstraints(t,_,_)
        | SynType.HashConstraint(t,_)
        | SynType.MeasurePower(t,_,_)
        | SynType.Array(_,t,_) ->
            x.ProcessSynType t

        | SynType.StaticConstantExpr(_)
        | SynType.StaticConstant(_)
        | SynType.Anon(_)
        | SynType.Var(_) -> ()

    member internal x.ProcessLocalExpression (expr: SynExpr) =
        match expr with
        | SynExpr.Paren(expr,_,_,_)
        | SynExpr.Quote(_,_,expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Const(_) -> ()

        | SynExpr.Typed(expr,synType,_) ->
            x.ProcessLocalExpression expr
            x.ProcessSynType synType

        | SynExpr.Tuple(exprs,_,_)
        | SynExpr.StructTuple(exprs,_,_)
        | SynExpr.ArrayOrList(_,exprs,_) ->
            for e in exprs do
                x.ProcessLocalExpression e

        | SynExpr.Record(_,copyInfoOpt,fields,_) ->
            match copyInfoOpt with
            | Some (expr,_) -> x.ProcessLocalExpression expr
            | _ -> ()

            // todo: mark name for getting reference access type
            for name, expr, _ in fields do
                if expr.IsSome then x.ProcessLocalExpression expr.Value

        | SynExpr.New(_,t,expr,_) ->
            x.ProcessSynType t
            x.ProcessLocalExpression expr

        | SynExpr.ObjExpr(t,args,bindings,interfaceImpls,_,_) ->
            x.ProcessSynType t
            match args with
            | Some (expr,_) -> x.ProcessLocalExpression expr
            | _ -> ()

            for b in bindings do x.ProcessLocalBinding b

            for InterfaceImpl(_,bindings,_) in interfaceImpls do
                for b in bindings do x.ProcessLocalBinding b

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

        | SynExpr.TypeApp(expr,lt,typeArgs,_,rt,_,r) ->
            x.ProcessLocalExpression expr
            lt|> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Mark()
            for t in typeArgs do x.ProcessSynType t
            (if rt.IsSome then rt.Value else r) |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.TYPE_ARGUMENT_LIST)

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

        | SynExpr.IfThenElse(ifExpr,thenExpr,elseExprOpt,_,_,_,_) ->
            x.ProcessLocalExpression ifExpr
            x.ProcessLocalExpression thenExpr
            if elseExprOpt.IsSome then
                x.ProcessLocalExpression elseExprOpt.Value

        | SynExpr.Ident(_)
        | SynExpr.LongIdent(_) -> ()

        | SynExpr.LongIdentSet(_,expr,_)
        | SynExpr.DotGet(expr,_,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.NamedIndexedPropertySet(_,expr1,expr2,_)
        | SynExpr.DotSet(expr1,_,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | SynExpr.DotNamedIndexedPropertySet(expr1,_,expr2,expr3,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2
            x.ProcessLocalExpression expr3

        | SynExpr.DotIndexedGet(expr,indexerArgs,_,_) ->
            x.ProcessLocalExpression expr
            for arg in indexerArgs do
                x.ProcessIndexerArg(arg)

        | SynExpr.DotIndexedSet(expr1,indexerArgs,expr2,_,_,_) ->
            x.ProcessLocalExpression expr1
            for arg in indexerArgs do
                x.ProcessIndexerArg(arg)
            x.ProcessLocalExpression expr2

        | SynExpr.TypeTest(expr,t,_)
        | SynExpr.Upcast(expr,t,_)
        | SynExpr.Downcast(expr,t,_) ->
            x.ProcessLocalExpression expr
            x.ProcessSynType t

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

        | SynExpr.Sequential(_,_,expr1,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | Apps (first, next) ->
            x.ProcessLocalExpression(first)
            for expr in next do
                x.ProcessLocalExpression(expr)

        | _ -> ()

    member x.ProcessIndexerArg arg =
        for expr in arg.Exprs do
            x.ProcessLocalExpression(expr)
