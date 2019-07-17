namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System.Collections.Generic
open FSharp.Compiler.Ast
open FSharp.Compiler.PrettyNaming
open FSharp.Compiler.Range
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

[<Struct>]
type BuilderStep =
    { Item: obj
      Processor: IBuilderStepProcessor }


and IBuilderStepProcessor =
    abstract Process: step: obj * builder: FSharpImplTreeBuilder -> unit


type FSharpImplTreeBuilder(lexer, document, decls, lifetime, projectedOffset) =
    inherit FSharpTreeBuilderBase(lexer, document, lifetime, projectedOffset)

    let nextSteps = Stack<BuilderStep>()

    new (lexer, document, decls, lifetime) =
        FSharpImplTreeBuilder(lexer, document, decls, lifetime, 0) 

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for decl in decls do
            x.ProcessTopLevelDeclaration(decl)
        x.FinishFile(mark, ElementType.F_SHARP_IMPL_FILE)

    member x.ProcessTopLevelDeclaration(SynModuleOrNamespace(lid, _, moduleKind, decls, _, attrs, _, range)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, moduleKind, range)
        for decl in decls do
            x.ProcessModuleMemberDeclaration(decl)
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberDeclaration(moduleMember) =
        match moduleMember with
        | SynModuleDecl.NestedModule(ComponentInfo(attrs, _, _, lid, _, _, _, _), _ ,decls, _, range) ->
            let mark = x.StartNestedModule attrs lid range
            for decl in decls do
                x.ProcessModuleMemberDeclaration(decl)
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(typeDefns, _) ->
            for typeDefn in typeDefns do
                x.ProcessTypeDefn(typeDefn)

        | SynModuleDecl.Exception(SynExceptionDefn(exn, memberDefns, range), _) ->
            let mark = x.StartException(exn)
            for memberDefn in memberDefns do
                x.ProcessTypeMember(memberDefn)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleDecl.Open(lidWithDots, range) ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.OPEN, range)
            x.ProcessLongIdentifier(lidWithDots.Lid)
            x.Done(range, mark, ElementType.OPEN_STATEMENT)

        | SynModuleDecl.Let(_, bindings, range) ->
            let letStart = letStartPos bindings range
            let letMark = x.Mark(letStart)
            for binding in bindings do
                x.ProcessTopLevelBinding(binding)
            x.Done(range, letMark, ElementType.LET_MODULE_DECL)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.DoExpr(_, expr, range) ->
            let mark = x.Mark(range)
            x.MarkChameleonExpression(expr)
            x.Done(range, mark, ElementType.DO)

        | SynModuleDecl.Attributes(attributes, _) ->
            for attribute in attributes do
                x.ProcessAttribute(attribute)

        | decl ->
            x.MarkAndDone(decl.Range, ElementType.OTHER_MEMBER_DECLARATION)

    member x.ProcessHashDirective(ParsedHashDirective(id, _, range)) =
        let mark = x.Mark(range)
        let elementType =
            match id with
            | "l" | "load" -> ElementType.LOAD_DIRECTIVE
            | "r" | "reference" -> ElementType.REFERENCE_DIRECTIVE
            | "I" -> ElementType.I_DIRECTIVE
            | _ -> ElementType.OTHER_DIRECTIVE
        x.Done(range, mark, elementType)

    member x.ProcessTypeDefn(TypeDefn(ComponentInfo(attrs, typeParams, _, lid , _, _, _, _), repr, members, range)) =
        match repr with
        | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid)
            x.ProcessTypeParametersOfType typeParams range false
            for extensionMember in members do
                x.ProcessTypeMember(extensionMember)
            x.Done(range, mark, ElementType.TYPE_EXTENSION_DECLARATION)
        | _ ->

        let mark = x.StartType attrs typeParams lid range
        let elementType =
            match repr with
            | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_, fields, _) ->
                    for field in fields do
                        x.ProcessField field ElementType.RECORD_FIELD_DECLARATION
                    ElementType.RECORD_DECLARATION

                | SynTypeDefnSimpleRepr.Enum(enumCases, _) ->
                    for case in enumCases do
                        x.ProcessEnumCase case
                    ElementType.ENUM_DECLARATION

                | SynTypeDefnSimpleRepr.Union(_, cases, range) ->
                    x.ProcessUnionCases(cases, range)
                    ElementType.UNION_DECLARATION

                | SynTypeDefnSimpleRepr.TypeAbbrev(_, synType, _) ->
                    x.ProcessType(synType)
                    ElementType.TYPE_ABBREVIATION_DECLARATION

                | SynTypeDefnSimpleRepr.None _ ->
                    ElementType.ABSTRACT_TYPE_DECLARATION

                | _ -> ElementType.OTHER_SIMPLE_TYPE_DECLARATION

            | SynTypeDefnRepr.Exception _ ->
                ElementType.EXCEPTION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) ->
                ElementType.TYPE_EXTENSION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(kind, members, _) ->
                for m in members do x.ProcessTypeMember m
                match kind with
                | TyconClass -> ElementType.CLASS_DECLARATION
                | TyconInterface -> ElementType.INTERFACE_DECLARATION
                | TyconStruct -> ElementType.STRUCT_DECLARATION

                | TyconDelegate(synType, _) ->
                    x.MarkOtherType(synType)                    
                    ElementType.DELEGATE_DECLARATION

                | _ -> ElementType.OBJECT_TYPE_DECLARATION

        for m in members do x.ProcessTypeMember m
        x.Done(range, mark, elementType)

    member x.ProcessTypeMember(typeMember: SynMemberDefn) =
        let attrs = typeMember.Attributes
        // todo: let/attrs range
        let rangeStart = x.GetStartOffset typeMember.Range
        let isMember =
            match typeMember with
            | SynMemberDefn.Member _ -> true
            | _ -> false

        if x.CurrentOffset <= rangeStart || (not isMember) then
            let mark = x.MarkAttributesOrIdOrRange(attrs, None, typeMember.Range)

            // todo: mark body exprs as synExpr
            let memberType =
                match typeMember with
                | SynMemberDefn.ImplicitCtor(_, _, args, selfId, _) ->
                    for arg in args do
                        x.ProcessImplicitCtorParam arg
                    x.ProcessCtorSelfId(selfId)
                    ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

                | SynMemberDefn.ImplicitInherit(baseType, args, _, _) ->
                    x.ProcessType(baseType)
                    x.MarkChameleonExpression(args)
                    ElementType.TYPE_INHERIT

                | SynMemberDefn.Interface(interfaceType, interfaceMembersOpt , _) ->
                    x.ProcessType(interfaceType)
                    match interfaceMembersOpt with
                    | Some members ->
                        for m in members do
                            x.ProcessTypeMember(m)
                    | _ -> ()
                    ElementType.INTERFACE_IMPLEMENTATION

                | SynMemberDefn.Inherit(baseType, _, _) ->
                    try x.ProcessType(baseType)
                    with _ -> () // Getting type range throws an exception if base type lid is empty.
                    ElementType.INTERFACE_INHERIT

                | SynMemberDefn.Member(Binding(_, _, _, _, _, _, valData, headPat, returnInfo, expr, _, _) ,range) ->
                    let elType =
                        match headPat with
                        | SynPat.LongIdent(LongIdentWithDots(lid, _), _, typeParamsOpt, memberParams, _, _) ->
                            match lid with
                            | [_] ->
                                match valData with
                                | SynValData(Some(flags), _, selfId) when flags.MemberKind = MemberKind.Constructor ->
                                    x.ProcessParams(memberParams, true, true) // todo: should check isLocal
                                    x.ProcessCtorSelfId(selfId)

                                    x.MarkChameleonExpression(expr)
                                    ElementType.MEMBER_CONSTRUCTOR_DECLARATION
                                | _ ->
                                    x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                                    ElementType.MEMBER_DECLARATION

                            | selfId :: _ :: _ ->
                                x.Done(selfId.idRange, x.Mark(), ElementType.MEMBER_SELF_ID)
                                x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                                ElementType.MEMBER_DECLARATION

                            | _ -> ElementType.OTHER_TYPE_MEMBER
                        | _ -> ElementType.OTHER_TYPE_MEMBER
                    elType

                | SynMemberDefn.LetBindings(bindings, _, _, _) ->
                    for binding in bindings do
                        x.ProcessTopLevelBinding(binding)
                    ElementType.LET_MODULE_DECL

                | SynMemberDefn.AbstractSlot(ValSpfn(_, _, typeParams, _, _, _, _, _, _, _, _), _, range) ->
                    match typeParams with
                    | SynValTyparDecls(typeParams, _, _) ->
                        x.ProcessTypeParametersOfType typeParams range true
                    ElementType.ABSTRACT_SLOT

                | SynMemberDefn.ValField(Field(_, _, _, _, _, _, _, _), _) ->
                    ElementType.VAL_FIELD

                | SynMemberDefn.AutoProperty(_, _, _, _, _, _, _, _, expr, _, _) ->
                    x.MarkChameleonExpression(expr)
                    ElementType.AUTO_PROPERTY

                | _ -> ElementType.OTHER_TYPE_MEMBER

            x.Done(typeMember.Range, mark, memberType)

    member x.ProcessCtorSelfId(selfId) =
        match selfId with
        | Some (IdentRange range) ->
            x.AdvanceToTokenOrRangeStart(FSharpTokenType.AS, range)
            x.Done(range, x.Mark(), ElementType.CTOR_SELF_ID)
        | _ -> ()
    
    member x.ProcessReturnInfo(returnInfo) =
        match returnInfo with
        | None -> ()
        | Some(SynBindingReturnInfo(returnType, range, attrs)) ->

        let startOffset = x.GetStartOffset(range)
        x.AdvanceToTokenOrOffset(FSharpTokenType.COLON, startOffset, range)

        let mark = x.Mark()
        x.ProcessAttributes(attrs)
        x.ProcessType(returnType)
        x.Done(range, mark, ElementType.RETURN_TYPE_INFO)

    member x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range) =
        match typeParamsOpt with
        | Some(SynValTyparDecls(typeParams, _, _)) ->
            x.ProcessTypeParametersOfType typeParams range true // todo: of type?..
        | _ -> ()

        x.ProcessParams(memberParams, true, true) // todo: should check isLocal
        x.ProcessReturnInfo(returnInfo)
        x.MarkChameleonExpression(expr)

    // isTopLevelPat is needed to distinguish function definitions from other long ident pats:
    // let (Some x) = ...
    // let Some x = ...
    // When long pat is a function pat its args are currently mapped as local decls. todo: rewrite it to be params
    // Getting proper params (with right impl and sig ranges) isn't easy, probably a fix is needed in FCS.
    member x.ProcessPat(PatRange range as pat, isLocal, isTopLevelPat) =
        let mark = x.Mark(range)

        let elementType =
            match pat with
            | SynPat.Named(pat, id, _, _, _) ->
                match pat with
                | SynPat.Wild _ -> ()
                | _ -> x.ProcessPat(pat, isLocal, false)

                if IsActivePatternName id.idText then x.ProcessActivePatternId(id, isLocal)
                if isLocal then ElementType.LOCAL_NAMED_PAT else ElementType.TOP_NAMED_PAT

            | SynPat.LongIdent(lid, _, typars, args, _, _) ->
                match lid.Lid with
                | [id] when id.idText = "op_ColonColon" ->
                    match args with
                    | Pats pats ->
                        for pat in pats do
                            x.ProcessPat(pat, isLocal, false)
                    | NamePatPairs(pats, _) ->
                        for _, pat in pats do
                            x.ProcessPat(pat, isLocal, false)

                    ElementType.CONS_PAT

                | _ ->

                match lid.Lid with
                | [id] ->
                    if IsActivePatternName id.idText then
                        x.ProcessActivePatternId(id, isLocal)
    
                    match typars with
                    | None -> ()
                    | Some(SynValTyparDecls(typarDecls, _, _)) ->

                    for typarDecl in typarDecls do
                        x.ProcessTypeParameter(typarDecl, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)

                | lid ->
                    x.ProcessLongIdentifier(lid)

                x.ProcessParams(args, isLocal || isTopLevelPat, false)
                if isLocal then ElementType.LOCAL_LONG_IDENT_PAT else ElementType.TOP_LONG_IDENT_PAT

            | SynPat.Typed(pat, _, _) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.TYPED_PAT

            | SynPat.Or(pat1, pat2, _) ->
                x.ProcessPat(pat1, isLocal, false)
                x.ProcessPat(pat2, isLocal, false)
                ElementType.OR_PAT

            | SynPat.Ands(pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.ANDS_PAT

            | SynPat.Tuple(_, pats, _) ->
                x.ProcessListLikePat(pats, isLocal)
                ElementType.TUPLE_PAT

            | SynPat.ArrayOrList(_, pats, _) ->
                x.ProcessListLikePat(pats, isLocal)
                ElementType.LIST_PAT

            | SynPat.Paren(pat, _) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.PAREN_PAT

            | SynPat.Record(pats, _) ->
                for _, pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.RECORD_PAT

            | SynPat.IsInst(typ, _) ->
                x.ProcessType(typ)
                ElementType.IS_INST_PAT

            | SynPat.Wild _ ->
                ElementType.WILD_PAT

            | SynPat.Attrib(pat, attrs, _) ->
                x.ProcessAttributes(attrs)
                x.ProcessPat(pat, isLocal, false)
                ElementType.ATTRIB_PAT

            | SynPat.Const _ ->
                ElementType.CONST_PAT

            | _ ->
                ElementType.OTHER_PAT

        x.Done(range, mark, elementType)

    member x.ProcessListLikePat(pats, isLocal) =
        for pat in pats do
            x.ProcessPat(pat, isLocal, false)

    member x.ProcessParams(args: SynConstructorArgs, isLocal, markMember) =
        match args with
        | Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | NamePatPairs(idsAndPats, _) ->
            for _, pat in idsAndPats do
                x.ProcessParam(pat, isLocal, markMember)

    member x.ProcessParam(PatRange range as pat, isLocal, markMember) =
        if not markMember then x.ProcessPat(pat, isLocal, false) else

        let mark = x.Mark(range)
        x.ProcessPat(pat, isLocal, false)
        x.Done(range, mark, ElementType.MEMBER_PARAM)

    member x.MarkOtherType(TypeRange range as typ) =
        let mark = x.Mark(range)
        x.ProcessType(typ)
        x.Done(range, mark, ElementType.OTHER_TYPE)

    member x.ProcessTopLevelBinding(Binding(_, kind, _, _, attrs, _, _ , headPat, returnInfo, expr, _, _) as binding) =
        let expr = x.FixExpresion(expr)

        match kind with
        | StandaloneExpression
        | DoBinding -> x.MarkChameleonExpression(expr)
        | _ ->

        let mark =
            match attrs with
            | [] -> x.Mark(binding.StartPos)
            | { Range = r } :: _ ->
                let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, r)
                x.ProcessAttributes(attrs)
                mark

        x.ProcessPat(headPat, false, true)
        x.ProcessReturnInfo(returnInfo)
        x.MarkChameleonExpression(expr)

        x.Done(binding.RangeOfBindingAndRhs, mark, ElementType.TOP_BINDING)

    member x.ProcessLocalBinding(Binding(_, kind, _, _, attrs, _, _, headPat, returnInfo, expr, _, _) as binding) =
        let expr = x.FixExpresion(expr)

        match kind with
        | StandaloneExpression
        | DoBinding -> x.ProcessExpression(expr)
        | _ ->

        let mark =
            match attrs with
            | [] -> x.Mark(binding.StartPos)
            | { Range = r } :: _ ->
                let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, r)
                x.ProcessAttributes(attrs)
                mark

        x.PushRangeForMark(binding.RangeOfBindingAndRhs, mark, ElementType.LOCAL_BINDING)
        x.ProcessPat(headPat, true, true)
        x.ProcessReturnInfo(returnInfo)
        x.ProcessExpression(expr)

    member x.PushRange(range: range, elementType) =
        x.PushRangeForMark(range, x.Mark(range), elementType)

    member x.PushRangeForMark(range, mark, elementType) =
        x.PushStep({ Range = range; Mark = mark; ElementType = elementType }, endRangeProcessor)

    member x.PushRangeAndProcessExpression(expr, range, elementType) =
        x.PushRange(range, elementType)
        x.ProcessExpression(expr)

    member x.PushStep(step: obj, processor: IBuilderStepProcessor) =
        nextSteps.Push({ Item = step; Processor = processor })

    member x.PushType(synType: SynType) =
        x.PushStep(synType, synTypeProcessor)

    member x.PushLongIdentifier(lid: LongIdent) =
        x.PushStep(lid, lidProcessor)

    member x.PushExpression(synExpr: SynExpr) =
        x.PushStep(synExpr, expressionProcessor)

    member x.PushStepList(items, processor) =
        match items with
        | [] -> ()
        | _ -> x.PushStep(items, processor)
    
    member x.PushExpressionList(exprs: SynExpr list) =
        x.PushStepList(exprs, expressionListProcessor)

    member x.ProcessTopLevelExpression(expr) =
        x.PushExpression(expr)

        while nextSteps.Count > 0 do
            let step = nextSteps.Pop()
            step.Processor.Process(step.Item, x)

    member x.ProcessExpression(ExprRange range as expr) =
        match expr with
        | SynExpr.Paren(expr, _, _, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.PAREN_EXPR)

        | SynExpr.Quote(_, _, expr, _, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.QUOTE_EXPR)

        | SynExpr.Const(_, _) ->
            x.MarkAndDone(range, ElementType.CONST_EXPR)

        | SynExpr.Typed(expr, synType, _) ->
            Assertion.Assert(rangeContainsRange range synType.Range,
                             "rangeContainsRange range synType.Range; {0}; {1}", range, synType.Range)

            x.PushRange(range, ElementType.TYPED_EXPR)
            x.PushType(synType)
            x.ProcessExpression(expr)

        | SynExpr.Tuple(isStruct, exprs, _, _) ->
            if isStruct then
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.STRUCT, range)
            else
                x.AdvanceToStart(range)

            x.PushRangeForMark(range, x.Mark(), ElementType.TUPLE_EXPR)
            x.ProcessExpressionList(exprs)

        | SynExpr.ArrayOrList(_, exprs, _) ->
            x.MarkListExpr(exprs, range, ElementType.ARRAY_OR_LIST_EXPR)

        | SynExpr.AnonRecd(_, copyInfo, fields, _) ->
            x.PushRange(range, ElementType.ANON_RECD_EXPR)
            x.PushStepList(fields, anonRecordFieldListProcessor)
            match copyInfo with
            | Some(expr, _) -> x.ProcessExpression(expr)
            | _ -> ()

        | SynExpr.Record(_, copyInfo, fields, _) ->
            x.PushRange(range, ElementType.RECORD_EXPR)
            x.PushStepList(fields, recordFieldListProcessor)
            match copyInfo with
            | Some(expr, _) -> x.ProcessExpression(expr)
            | _ -> ()

        | SynExpr.New(_, synType, expr, _) ->
            x.PushRange(range, ElementType.NEW_EXPR)
            x.ProcessType(synType)
            x.ProcessExpression(expr)

        | SynExpr.ObjExpr(synType, args, bindings, interfaceImpls, _, _) ->
            x.PushRange(range, ElementType.OBJ_EXPR)
            x.ProcessType(synType)
            x.PushStepList(interfaceImpls, interfaceImplementationListProcessor)
            x.PushStepList(bindings, bindingListProcessor)

            match args with
            | Some(expr, _) -> x.ProcessExpression(expr)
            | _ -> ()

        | SynExpr.While(_, whileExpr, doExpr, _) ->
            x.PushRange(range, ElementType.WHILE_EXPR)
            x.PushExpression(doExpr)
            x.ProcessExpression(whileExpr)

        | SynExpr.For(_, id, idBody, _, toBody, doBody, _) ->
            x.PushRange(range, ElementType.FOR_EXPR)
            x.PushExpression(doBody)
            x.PushExpression(toBody)
            x.ProcessLocalId(id)
            x.ProcessExpression(idBody)

        | SynExpr.ForEach(_, _, _, pat, enumExpr, bodyExpr, _) ->
            x.PushRange(range, ElementType.FOR_EACH_EXPR)
            x.ProcessPat(pat, true, false)
            x.PushExpression(bodyExpr)
            x.ProcessExpression(enumExpr)

        | SynExpr.ArrayOrListOfSeqExpr(_, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.ARRAY_OR_LIST_OF_SEQ_EXPR)

        | SynExpr.CompExpr(_, _, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.COMP_EXPR)

        | SynExpr.Lambda(_, inLambdaSeq, _, bodyExpr, _) ->
            // Lambdas get "desugared" by converting to fake nested lambdas and match expressions.
            // Simple patterns like ids are preserved in lambdas and more complex ones are replaced
            // with generated placeholder patterns and go to generated match expressions inside lambda bodies.

            // Generated match expression have have a single generated clause with a generated id pattern.
            // Their ranges overlap with lambda param pattern ranges and they have the same start pos as lambdas. 

            Assertion.Assert(not inLambdaSeq, "Expecting non-generated lambda expression, got:\n{0}", expr)
            x.PushRange(range, ElementType.LAMBDA_EXPR)

            let skippedLambdas = skipGeneratedLambdas bodyExpr
            x.MarkLambdaParams(expr, skippedLambdas, true)
            x.ProcessExpression(skipGeneratedMatch skippedLambdas)

        | SynExpr.MatchLambda(_, _, clauses, _, _) ->
            x.PushRange(range, ElementType.MATCH_LAMBDA_EXPR)
            x.ProcessMatchClauses(clauses)

        | SynExpr.Match(_, expr, clauses, _) ->
            x.MarkMatchExpr(range, expr, clauses)

        | SynExpr.Do(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.DO_EXPR)

        | SynExpr.Assert(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.ASSERT_EXPR)

        | SynExpr.App(_, isInfix, funcExpr, argExpr, _) ->
            // todo: mark separate nodes for infix apps
            x.PushRange(range, ElementType.APP_EXPR)
            if isInfix then
                x.PushExpression(funcExpr)
                x.ProcessExpression(argExpr)
            else
                x.PushExpression(argExpr)
                x.ProcessExpression(funcExpr)

        | SynExpr.TypeApp(expr, _, _, _, _, _, _) as typeApp ->
            x.PushRange(range, ElementType.TYPE_APP_EXPR)
            x.PushStep(typeApp, typeArgsProcessor)
            x.ProcessExpression(expr)

        | SynExpr.LetOrUse(_, _, bindings, bodyExpr, _) ->
            x.PushRange(range, ElementType.LET_OR_USE_EXPR)
            x.PushExpression(bodyExpr)
            x.ProcessBindings(bindings)

        | SynExpr.TryWith(tryExpr, _, withCases, _, _, _, _) ->
            x.PushRange(range, ElementType.TRY_WITH_EXPR)
            x.PushStepList(withCases, matchClauseListProcessor)
            x.ProcessExpression(tryExpr)

        | SynExpr.TryFinally(tryExpr, finallyExpr, _, _, _) ->
            x.PushRange(range, ElementType.TRY_FINALLY_EXPR)
            x.PushExpression(finallyExpr)
            x.ProcessExpression(tryExpr)

        | SynExpr.Lazy(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.LAZY_EXPR)

        | SynExpr.IfThenElse(ifExpr, thenExpr, elseExprOpt, _, _, _, _) ->
            x.PushRange(range, ElementType.IF_THEN_ELSE_EXPR)
            if elseExprOpt.IsSome then
                x.PushExpression(elseExprOpt.Value)
            x.PushExpression(thenExpr)
            x.ProcessExpression(ifExpr)

        | SynExpr.Ident _ ->
            x.MarkAndDone(range, ElementType.IDENT_EXPR)

        | SynExpr.LongIdent(_, lid, _, _) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid.Lid)
            x.Done(range, mark, ElementType.LONG_IDENT_EXPR)

        | SynExpr.LongIdentSet(lid, expr, _) ->
            x.PushRange(range, ElementType.LONG_IDENT_SET_EXPR)
            x.ProcessLongIdentifier(lid.Lid)
            x.ProcessExpression(expr)

        | SynExpr.DotGet(expr, _, lidWithDots, _) ->
            x.PushRange(range, ElementType.DOT_GET_EXPR)
            x.PushLongIdentifier(lidWithDots.Lid)
            x.ProcessExpression(expr)

        | SynExpr.DotSet(expr1, lidWithDots, expr2, _) ->
            x.PushRange(range, ElementType.DOT_SET_EXPR)
            x.PushExpression(expr2)
            x.PushLongIdentifier(lidWithDots.Lid)
            x.ProcessExpression(expr1)

        | SynExpr.Set(expr1, expr2, _) ->
            x.PushRange(range, ElementType.EXPR_SET_EXPR)
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

        | SynExpr.NamedIndexedPropertySet(_, expr1, expr2, _) ->
            x.PushRange(range, ElementType.NAMED_INDEXED_PROPERTY_SET)
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

        | SynExpr.DotNamedIndexedPropertySet(expr1, lidWithDots, expr2, expr3, _) ->
            x.PushRange(range, ElementType.DOT_NAMED_INDEXED_PROPERTY_SET)
            x.PushExpression(expr3)
            x.PushExpression(expr2)
            x.PushLongIdentifier(lidWithDots.Lid)
            x.ProcessExpression(expr1)

        | SynExpr.DotIndexedGet(expr, _, _, _) as get ->
            x.PushRange(range, ElementType.DOT_INDEXED_GET_EXPR)
            x.PushStep(get, indexerArgsProcessor)
            x.ProcessExpression(expr)

        | SynExpr.DotIndexedSet(expr1, _, expr2, _, _, _) as set ->
            x.PushRange(range, ElementType.DOT_INDEXED_SET_EXPR)
            x.PushExpression(expr2)
            x.PushStep(set, indexerArgsProcessor)
            x.ProcessExpression(expr1)

        | SynExpr.TypeTest(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.TYPE_TEST_EXPR)

        | SynExpr.Upcast(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.UPCAST_EXPR)

        | SynExpr.Downcast(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.DOWNCAST_EXPR)

        | SynExpr.InferredUpcast(expr, _)
        | SynExpr.InferredDowncast(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.INFERRED_CAST_EXPR)

        | SynExpr.Null _ ->
            x.MarkAndDone(range, ElementType.NULL_EXPR)

        | SynExpr.AddressOf(_, expr, _, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.ADDRESS_OF_EXPR)

        | SynExpr.TraitCall(_, _, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.TRAIT_CALL_EXPR)

        | SynExpr.JoinIn(expr1, _, expr2, _) ->
            x.PushRange(range, ElementType.JOIN_IN_EXPR)
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

        | SynExpr.ImplicitZero _ ->
            x.MarkAndDone(range, ElementType.IMPLICIT_ZERO_EXPR)

        | SynExpr.YieldOrReturn(_, expr, _)
        | SynExpr.YieldOrReturnFrom(_, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.YIELD_OR_RETURN_EXPR)

        | SynExpr.LetOrUseBang(_, _, _, pat, expr, inExpr, _) ->
            x.PushRange(range, ElementType.LET_OR_USE_BANG_EXPR)
            x.ProcessPat(pat, true, false)
            x.PushExpression(inExpr)
            x.ProcessExpression(expr)

        | SynExpr.MatchBang(_, expr, clauses, _) ->
            x.MarkMatchExpr(range, expr, clauses)

        | SynExpr.DoBang(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.DO_EXPR)

        | SynExpr.LibraryOnlyILAssembly _
        | SynExpr.LibraryOnlyStaticOptimization _
        | SynExpr.LibraryOnlyUnionCaseFieldGet _
        | SynExpr.LibraryOnlyUnionCaseFieldSet _
        | SynExpr.LibraryOnlyILAssembly _ ->
            x.MarkAndDone(range, ElementType.LIBRARY_ONLY_EXPR)

        | SynExpr.ArbitraryAfterError _
        | SynExpr.FromParseError _
        | SynExpr.DiscardAfterMissingQualificationAfterDot _ ->
            x.MarkAndDone(range, ElementType.FROM_ERROR_EXPR)

        | SynExpr.Fixed(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.FIXED_EXPR)

        | SynExpr.Sequential(_, _, expr1, expr2, _) ->
            // todo: concat nested sequential expressions
            x.PushRange(range, ElementType.SEQUENTIAL_EXPR)
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

    member x.MarkLambdaParams(expr, outerBodyExpr, topLevel) =
        match expr with
        | SynExpr.Lambda(_, inLambdaSeq, pats, bodyExpr, _) when inLambdaSeq <> topLevel ->
            x.MarkLambdaParams(pats, bodyExpr, outerBodyExpr)

        | _ -> ()

    member x.MarkLambdaParams(pats: SynSimplePats, lambdaBody: SynExpr, outerBodyExpr) =
        match pats with
        | SynSimplePats.SimplePats(pats, _) ->
            // `pats` can be empty for unit patterns.

            x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)

//            match pats with
//            | [pat] ->
////                if posLt range.Start pat.Range.Start then
////                    let mark = x.Mark(range)
//                    x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)
////                    x.Done(range, mark, ElementType.PAREN_PAT)
////                else
////                    x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)
//
//            | _ ->
//                let mark = x.Mark(range) // todo: mark before lparen
//                x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)
//                x.Done(range, mark, ElementType.PAREN_PAT) // todo: marp tuple pat

        | SynSimplePats.Typed _ ->
            failwithf "Expecting SimplePats, got:\n%A" pats

    member x.MarkLambdaParam(pats: SynSimplePat list, lambdaBody: SynExpr, outerBodyExpr) =
        match pats with
        | [] -> x.MarkLambdaParams(lambdaBody, outerBodyExpr, false)
        | pat :: pats ->
            match pat with
            | SynSimplePat.Id(_, _, isGenerated, _, _, range) ->
                if not isGenerated then
                    x.MarkAndDone(range, ElementType.LOCAL_NAMED_PAT)
                    x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)
                else
                    match outerBodyExpr with
                    | SynExpr.Match(_, _, [ Clause(pat, whenExpr, innerExpr, clauseRange, _) ], matchRange) when
                            matchRange.Start = clauseRange.Start ->

                        Assertion.Assert(whenExpr.IsNone, "whenExpr.IsNone")
                        x.ProcessPat(pat, true, false)
                        x.MarkLambdaParam(pats, lambdaBody, innerExpr)

                    | _ ->
                        failwithf "Expecting generated match expression, got:\n%A" lambdaBody
            | _ ->
                x.MarkLambdaParam(pats, lambdaBody, outerBodyExpr)
    
    member x.MarkMatchExpr(range: range, expr, clauses) =
        x.PushRange(range, ElementType.MATCH_EXPR)
        x.PushStepList(clauses, matchClauseListProcessor)
        x.ProcessExpression(expr)

    member x.ProcessMatchClauses(clauses) =
        match clauses with
        | [] -> ()
        | [ clause ] ->
            x.ProcessMatchClause(clause)

        | clause :: clauses ->
            x.PushStepList(clauses, matchClauseListProcessor)
            x.ProcessMatchClause(clause)

    member x.ProcessBindings(clauses) =
        match clauses with
        | [] -> ()
        | [ binding ] ->
            x.ProcessLocalBinding(binding)

        | binding :: bindings ->
            x.PushStepList(bindings, bindingListProcessor)
            x.ProcessLocalBinding(binding)
    
    member x.ProcessExpressionList(exprs) =
        match exprs with
        | [] -> ()
        | [ expr ] ->
            x.ProcessExpression(expr)

        | [ expr1; expr2 ] ->
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

        | expr :: rest ->
            x.PushExpressionList(rest)
            x.ProcessExpression(expr)
    
    member x.ProcessInterfaceImplementation(InterfaceImpl(interfaceType, bindings, range)) =
        x.PushRange(range, ElementType.OBJ_EXPR_SECONDARY_INTERFACE)
        x.ProcessType(interfaceType)
        x.ProcessBindings(bindings)

    member x.ProcessAnonRecordField(IdentRange idRange, (ExprRange range as expr)) =
        // Start node at id range, end at expr range.
        let mark = x.Mark(idRange)
        x.PushRangeForMark(range, mark, ElementType.RECORD_EXPR_BINDING)
        x.ProcessExpression(expr)

    member x.ProcessRecordField(lid, expr) =
        match lid, expr with
        | [], None -> ()
        | [], Some(ExprRange range as expr) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.RECORD_EXPR_BINDING)

        | IdentRange headRange :: _, expr ->
            let mark = x.Mark(headRange)
            x.PushRangeForMark(headRange, mark, ElementType.RECORD_EXPR_BINDING)
            x.ProcessLongIdentifier(lid)
            if expr.IsSome then
                x.ProcessExpression(expr.Value)
    
    member x.MarkListExpr(exprs, range, elementType) =
        x.PushRange(range, elementType)
        x.ProcessExpressionList(exprs)

    member x.MarkTypeExpr(expr, synType, range, elementType) =
        x.PushRange(range, elementType)
        x.PushType(synType)
        x.ProcessExpression(expr)

    member x.ProcessMatchClause(Clause(pat, whenExprOpt, expr, _, _) as clause) =
        let range = clause.Range
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)
        x.PushRangeForMark(range, mark, ElementType.MATCH_CLAUSE)

        x.ProcessPat(pat, true, false)
        match whenExprOpt with
        | Some whenExpr ->
            x.PushExpression(expr)
            x.ProcessExpression(whenExpr)
        | _ ->
            x.ProcessExpression(expr)

    member x.ProcessIndexerArg(arg: SynIndexerArg) =
        x.ProcessExpressionList(arg.Exprs)


[<AbstractClass>]
type StepProcessorBase<'TStep>() =
    abstract Process: step: 'TStep * builder: FSharpImplTreeBuilder -> unit

    interface IBuilderStepProcessor with
        member x.Process(step, builder) =
            x.Process(step :?> 'TStep, builder)

[<AbstractClass>]
type StepListProcessorBase<'TStep>() =
    abstract Process: 'TStep * FSharpImplTreeBuilder -> unit

    interface IBuilderStepProcessor with
        member x.Process(step, builder) =
            match step :?> 'TStep list with
            | [ item ] ->
                x.Process(item, builder)

            | item :: rest ->
                builder.PushStepList(rest, x)
                x.Process(item, builder)

            | [] -> failwithf "Unexpected empty items list"


type ExpressionProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(expr, builder) =
        builder.ProcessExpression(expr)


type RangeMarkAndType =
    { Range: range
      Mark: int
      ElementType: NodeType }

type EndRangeProcessor() =
    inherit StepProcessorBase<RangeMarkAndType>()

    override x.Process(item, builder) =
        builder.Done(item.Range, item.Mark, item.ElementType)


type LidProcessor() =
    inherit StepProcessorBase<LongIdent>()

    override x.Process(lid, builder) =
        builder.ProcessLongIdentifier(lid)


type SynTypeProcessor() =
    inherit StepProcessorBase<SynType>()

    override x.Process(synType, builder) =
        builder.ProcessType(synType)


type TypeArgsProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(synExpr, builder) =
        match synExpr with
            | SynExpr.TypeApp(_, ltRange, typeArgs, _, gtRangeOpt, _, _) ->
                builder.ProcessTypeArgs(typeArgs, ltRange, gtRangeOpt)
            | _ -> failwithf "Expecting typeApp, got: %A" synExpr


type ExpressionListProcessor() =
    inherit StepListProcessorBase<SynExpr>()

    override x.Process(expr, builder) =
        builder.ProcessExpression(expr)


type BindingListProcessor() =
    inherit StepListProcessorBase<SynBinding>()

    override x.Process(binding, builder) =
        builder.ProcessLocalBinding(binding)


type RecordFieldListProcessor() =
    inherit StepListProcessorBase<RecordFieldName * (SynExpr option) * BlockSeparator option>()

    override x.Process(field, builder) =
        let (lid, _), expr, _ = field 
        builder.ProcessRecordField(lid.Lid, expr)


type AnonRecordFieldListProcessor() =
    inherit StepListProcessorBase<Ident * SynExpr>()

    override x.Process(field, builder) =
        builder.ProcessAnonRecordField(field)


type MatchClauseListProcessor() =
    inherit StepListProcessorBase<SynMatchClause>()

    override x.Process(matchClause, builder) =
        builder.ProcessMatchClause(matchClause)


type InterfaceImplementationListProcessor() =
    inherit StepListProcessorBase<SynInterfaceImpl>()

    override x.Process(interfaceImpl, builder) =
        builder.ProcessInterfaceImplementation(interfaceImpl)


type IndexerArgsProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(synExpr, builder) =
        match synExpr with
        | SynExpr.DotIndexedGet(_, [indexerArg], dotRange, range)
        | SynExpr.DotIndexedSet(_, [indexerArg], _, range, dotRange, _) ->
            let indexerRange = mkFileIndexRange range.FileIndex dotRange.End range.End
            builder.PushRange(indexerRange, ElementType.INDEXER_ARG)
            builder.PushExpressionList(indexerArg.Exprs)

        | _ -> failwithf "Expecting dotIndexedGet/Set, got: %A" synExpr


[<AutoOpen>]
module BuilderStepProcessors =
    // We have to create these singletons this way instead of object expressions
    // due to compiler producing additional recursive init checks otherwise in this case.

    let expressionProcessor = ExpressionProcessor()
    let endRangeProcessor = EndRangeProcessor()
    let lidProcessor = LidProcessor()
    let synTypeProcessor = SynTypeProcessor()
    let typeArgsProcessor = TypeArgsProcessor()
    let expressionListProcessor = ExpressionListProcessor()
    let bindingListProcessor = BindingListProcessor()
    let recordFieldListProcessor = RecordFieldListProcessor()
    let anonRecordFieldListProcessor = AnonRecordFieldListProcessor()
    let matchClauseListProcessor = MatchClauseListProcessor()
    let interfaceImplementationListProcessor = InterfaceImplementationListProcessor()
    let indexerArgsProcessor = IndexerArgsProcessor()
