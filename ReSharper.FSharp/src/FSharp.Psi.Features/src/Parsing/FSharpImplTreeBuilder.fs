namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.Ast
open FSharp.Compiler.PrettyNaming
open FSharp.Compiler.Range
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util

type internal FSharpImplTreeBuilder(file, lexer, decls, lifetime) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for decl in decls do
            x.ProcessTopLevelDeclaration(decl)
        x.FinishFile(mark, ElementType.F_SHARP_IMPL_FILE)

    member x.ProcessTopLevelDeclaration(SynModuleOrNamespace(lid,_,moduleKind,decls,_,attrs,_,range)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, moduleKind, range)
        for decl in decls do
            x.ProcessModuleMemberDeclaration(decl)
        x.FinishTopLevelDeclaration(mark, range, elementType)  

    member x.ProcessModuleMemberDeclaration moduleMember =
        match moduleMember with
        | SynModuleDecl.NestedModule(ComponentInfo(attrs,_,_,lid,_,_,_,_),_,decls,_,range) ->
            let mark = x.StartNestedModule attrs lid range
            for d in decls do x.ProcessModuleMemberDeclaration d
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(types,_) ->
            for t in types do x.ProcessType t

        | SynModuleDecl.Exception(SynExceptionDefn(exn, members, range),_) ->
            let mark = x.StartException(exn)
            for m in members do x.ProcessTypeMember(m)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleDecl.Open(lidWithDots,range) ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.OPEN, range)
            x.ProcessLongIdentifier lidWithDots.Lid
            x.Done(range, mark, ElementType.OPEN_STATEMENT)

        | SynModuleDecl.Let(_, bindings, range) ->
            let letStart = letStartPos bindings range
            let letMark = x.Mark(letStart)
            for binding in bindings do
                x.ProcessBinding(binding, false)
            x.Done(range, letMark, ElementType.LET)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.DoExpr (_, expr, range) ->
            let mark = x.Mark(range)
            x.MarkTopLevelExpression(expr)
            x.Done(range, mark, ElementType.DO)

        | decl ->
            x.MarkAndDone(decl.Range, ElementType.OTHER_MEMBER_DECLARATION)

    member x.ProcessHashDirective(ParsedHashDirective (id, _, range)) =
        let mark = x.Mark(range)
        let elementType =
            match id with
            | "l" | "load" -> ElementType.LOAD_DIRECTIVE
            | "r" | "reference" -> ElementType.REFERENCE_DIRECTIVE
            | "I" -> ElementType.I_DIRECTIVE
            | _ -> ElementType.OTHER_DIRECTIVE
        x.Done(range, mark, elementType)

    member x.ProcessType(TypeDefn(ComponentInfo(attrs, typeParams,_,lid,_,_,_,_), repr, members, range)) =
        match repr with
        | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation,_,_) ->
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
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                    for field in fields do
                        x.ProcessField field ElementType.RECORD_FIELD_DECLARATION
                    ElementType.RECORD_DECLARATION

                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
                    for case in enumCases do
                        x.ProcessEnumCase case
                    ElementType.ENUM_DECLARATION

                | SynTypeDefnSimpleRepr.Union(_,cases, range) ->
                    x.ProcessUnionCases(cases, range)
                    ElementType.UNION_DECLARATION

                | SynTypeDefnSimpleRepr.TypeAbbrev(_,t,_) ->
                    x.ProcessSynType t
                    ElementType.TYPE_ABBREVIATION_DECLARATION

                | SynTypeDefnSimpleRepr.None(_) ->
                    ElementType.ABSTRACT_TYPE_DECLARATION

                | _ -> ElementType.OTHER_SIMPLE_TYPE_DECLARATION

            | SynTypeDefnRepr.Exception(_) ->
                ElementType.EXCEPTION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation,_,_) ->
                ElementType.TYPE_EXTENSION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(kind, members, _) ->
                for m in members do x.ProcessTypeMember m
                match kind with
                | TyconClass -> ElementType.CLASS_DECLARATION
                | TyconInterface -> ElementType.INTERFACE_DECLARATION
                | TyconStruct -> ElementType.STRUCT_DECLARATION

                | TyconDelegate (synType, _) ->
                    x.MarkOtherType(synType)                    
                    ElementType.DELEGATE_DECLARATION

                | _ -> ElementType.OBJECT_TYPE_DECLARATION

        for m in members do x.ProcessTypeMember m
        x.Done(range, mark, elementType)

    member x.ProcessTypeMember (typeMember: SynMemberDefn) =
        let attrs = typeMember.Attributes
        // todo: let/attrs range
        let rangeStart = x.GetStartOffset typeMember.Range
        let isMember =
            match typeMember with
            | SynMemberDefn.Member _ -> true
            | _ -> false

        if x.Builder.GetTokenOffset() <= rangeStart || (not isMember) then
            let mark = x.MarkAttributesOrIdOrRange(attrs, None, typeMember.Range)

            // todo: mark body exprs as synExpr
            let memberType =
                match typeMember with
                | SynMemberDefn.ImplicitCtor(_,_,args,selfId,_) ->
                    for arg in args do
                        x.ProcessImplicitCtorParam arg
                    if selfId.IsSome then x.ProcessLocalId selfId.Value
                    ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

                | SynMemberDefn.ImplicitInherit(baseType,args,_,_) ->
                    x.ProcessSynType(baseType)
                    x.MarkTopLevelExpression(args)
                    ElementType.TYPE_INHERIT

                | SynMemberDefn.Interface(interfaceType,interfaceMembersOpt,_) ->
                    x.ProcessSynType(interfaceType)
                    match interfaceMembersOpt with
                    | Some members ->
                        for m in members do
                            x.ProcessTypeMember(m)
                    | _ -> ()
                    ElementType.INTERFACE_IMPLEMENTATION

                | SynMemberDefn.Inherit(baseType,_,_) ->
                    try x.ProcessSynType(baseType)
                    with _ -> () // Getting type range throws an exception if base type lid is empty.
                    ElementType.INTERFACE_INHERIT

                | SynMemberDefn.Member(Binding(_,_,_,_,_,_,valData,headPat,returnInfo,expr,_,_),range) ->
                    let elType =
                        match headPat with
                        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOpt,memberParams,_,_) ->
                            match lid with
                            | [_] ->
                                match valData with
                                | SynValData(Some flags,_,selfId) when flags.MemberKind = MemberKind.Constructor ->
                                    x.ProcessParams(memberParams, true, true) // todo: should check isLocal
                                    if selfId.IsSome then
                                        x.ProcessLocalId(selfId.Value)

                                    x.MarkTopLevelExpression(expr)
                                    ElementType.CONSTRUCTOR_DECLARATION
                                | _ ->
                                    x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                                    ElementType.MEMBER_DECLARATION

                            | selfId :: _ :: _ ->
                                x.ProcessLocalId selfId
                                x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                                ElementType.MEMBER_DECLARATION

                            | _ -> ElementType.OTHER_TYPE_MEMBER
                        | _ -> ElementType.OTHER_TYPE_MEMBER
                    elType

                | SynMemberDefn.LetBindings(bindings,_,_,_) ->
                    for binding in bindings do
                        x.ProcessBinding(binding, false)
                    ElementType.LET

                | SynMemberDefn.AbstractSlot(ValSpfn(_,_,typeParams,_,_,_,_,_,_,_,_),_,range) ->
                    match typeParams with
                    | SynValTyparDecls(typeParams,_,_) ->
                        x.ProcessTypeParametersOfType typeParams range true
                    ElementType.ABSTRACT_SLOT

                | SynMemberDefn.ValField(Field(_,_,_,_,_,_,_,_),_) ->
                    ElementType.VAL_FIELD

                | SynMemberDefn.AutoProperty(_,_,_,_,_,_,_,_,expr,_,_) ->
                    x.ProcessExpression(expr)
                    ElementType.AUTO_PROPERTY

                | _ -> ElementType.OTHER_TYPE_MEMBER

            x.Done(typeMember.Range, mark, memberType)

    member x.ProcessReturnInfo(returnInfo) =
        match returnInfo with
        | Some(SynBindingReturnInfo(returnType,_,_)) -> x.ProcessSynType(returnType)
        | _ -> ()

    member x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range) =
        match typeParamsOpt with
        | Some(SynValTyparDecls(typeParams,_,_)) ->
            x.ProcessTypeParametersOfType typeParams range true // todo: of type?..
        | _ -> ()

        x.ProcessParams(memberParams, true, true) // todo: should check isLocal
        x.ProcessReturnInfo(returnInfo)
        x.MarkTopLevelExpression(expr)

    // isTopLevelPat is needed to distinguish function definitions from other long ident pats:
    // let (Some x) = ...
    // let Some x = ...
    // When long pat is a function pat its args are currently mapped as local decls. todo: rewrite it to be params
    // Getting proper params (with right impl and sig ranges) isn't easy, probably a fix is needed in FCS.
    member x.ProcessPat(PatRange range as pat, isLocal, isTopLevelPat) =
        let mark = x.Mark(range)

        let elementType =
            match pat with
            | SynPat.Named (pat, id, _, _, _) ->
                match pat with
                | SynPat.Wild _ -> ()
                | _ -> x.ProcessPat(pat, isLocal, false)

                if IsActivePatternName id.idText then x.ProcessActivePatternId(id, isLocal)
                if isLocal then ElementType.LOCAL_NAMED_PAT else ElementType.TOP_NAMED_PAT

            | SynPat.LongIdent (lid, _, typars, args, _, _) ->
                match lid.Lid with
                | [id] when id.idText = "op_ColonColon" ->
                    match args with
                    | Pats pats -> for p in pats do x.ProcessPat(p, isLocal, false)
                    | NamePatPairs (pats, _) -> for _, p in pats do x.ProcessPat(p, isLocal, false)
                    ElementType.CONS_PAT

                | _ ->

                match lid.Lid with
                | [id] ->
                    if IsActivePatternName id.idText then
                        x.ProcessActivePatternId(id, isLocal)
    
                    match typars with
                    | None -> ()
                    | Some (SynValTyparDecls (typars, _, _)) ->

                    for p in typars do
                        x.ProcessTypeParameter(p, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)

                | lid ->
                    x.ProcessLongIdentifier(lid)

                x.ProcessParams(args, isLocal || isTopLevelPat, false)
                if isLocal then ElementType.LOCAL_LONG_IDENT_PAT else ElementType.TOP_LONG_IDENT_PAT

            | SynPat.Typed (pat, _, _) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.TYPED_PAT

            | SynPat.Or (pat1, pat2, _) ->
                x.ProcessPat(pat1, isLocal, false)
                x.ProcessPat(pat2, isLocal, false)
                ElementType.OR_PAT

            | SynPat.Ands (pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.ANDS_PAT

            | SynPat.Tuple (_, pats, _)
            | SynPat.ArrayOrList (_, pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.LIST_PAT

            | SynPat.Paren (pat,_) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.PAREN_PAT

            | SynPat.Record (pats, _) ->
                for _, pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.RECORD_PAT

            | SynPat.IsInst (typ, _) ->
                x.ProcessSynType(typ)
                ElementType.IS_INST_PAT

            | SynPat.Wild _ ->
                ElementType.WILD_PAT

            | SynPat.Attrib (pat, attrs, _) ->
                x.ProcessAttributes(attrs)
                x.ProcessPat(pat, isLocal, false)
                ElementType.ATTRIB_PAT

            | _ ->
                ElementType.OTHER_PAT

        x.Done(range, mark, elementType)

    member x.ProcessParams(args: SynConstructorArgs, isLocal, markMember) =
        match args with
        | Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | NamePatPairs (idsAndPats, _) ->
            for _, pat in idsAndPats do
                x.ProcessParam(pat, isLocal, markMember)

    member x.ProcessParam(PatRange range as pat, isLocal, markMember) =
        if not markMember then x.ProcessPat(pat, isLocal, false) else

        let mark = x.Mark(range)
        x.ProcessPat(pat, isLocal, false)
        x.Done(range, mark, ElementType.MEMBER_PARAM)

    member x.FixExpresion(expr) =
        // A fake SynExpr.Typed node is added for binding with return type specification like in the following
        // member x.Prop: int = 1
        // where 1 is replaced with `1: int`. 
        // These fake nodes have original type specification ranges that are out of the actual expression ranges.
        match expr with
        | SynExpr.Typed(inner, synType, range) when not (rangeContainsRange range synType.Range) -> inner
        | _ -> expr

    member x.MarkTopLevelExpression(expr) =
        let (ExprRange range as expr) = x.FixExpresion(expr)

        let mark = x.Mark(range)
        x.ProcessExpression(expr)
        x.Done(range, mark, ElementType.OTHER_EXPR)

    member x.MarkOtherType(TypeRange range as typ) =
        let mark = x.Mark(range)
        x.ProcessSynType(typ)
        x.Done(range, mark, ElementType.OTHER_TYPE)

    member x.ProcessBinding(Binding(_,kind,_,_,attrs,_,_,headPat,returnInfo, expr,_,_) as binding, isLocal: bool) =
        match kind with
        | StandaloneExpression
        | DoBinding ->
            x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr for isLocal = true

        | _ ->

        let mark =
            match attrs with
            | [] -> x.Mark(binding.StartPos)
            | { Range = r } :: _ ->
                let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, r)
                x.ProcessAttributes(attrs)
                mark

        x.ProcessPat(headPat, isLocal, true)
        x.ProcessReturnInfo(returnInfo)
        x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr for isLocal = true

        let elementType = if isLocal then ElementType.LOCAL_BINDING else ElementType.TOP_BINDING
        x.Done(binding.RangeOfBindingAndRhs, mark, elementType)

    member x.ProcessExpression(expr: SynExpr) =
        match expr with
        | SynExpr.Paren(expr,_,_,_)
        | SynExpr.Quote(_,_,expr,_,_) ->
            x.ProcessExpression(expr)

        | SynExpr.Const(synConst, range) ->
            match synConst with
            | SynConst.Unit ->
                x.MarkAndDone(range, ElementType.CONST_EXPR)
            | _ -> ()

        | SynExpr.Typed(expr, synType, range) ->
            Assertion.Assert(rangeContainsRange range synType.Range, "rangeContainsRange range synType.Range")

            x.ProcessExpression(expr)
            x.ProcessSynType(synType)

        | SynExpr.Tuple(_, exprs,_,range) ->
            x.MarkListExpr(exprs, range, ElementType.TUPLE_EXPR)

        | SynExpr.ArrayOrList(_,exprs,range) ->
            x.MarkListExpr(exprs, range, ElementType.ARRAY_OR_LIST_EXPR)

        | SynExpr.Record(_,copyInfo,fields,range) ->
            let mark = x.Mark(range)
            match copyInfo with
            | Some (expr,_) ->
                x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
            | _ -> ()

            for (lid, _), expr, _ in fields do
                let lid = lid.Lid
                match lid, expr with
                | [], None -> ()
                | [], Some (ExprRange range as expr) ->
                    let mark = x.Mark(range)
                    x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
                    x.Done(mark, ElementType.RECORD_EXPR_BINDING)

                | IdentRange headRange :: _, expr ->
                    let mark = x.Mark(headRange)
                    x.ProcessLongIdentifier(lid)
                    if expr.IsSome then
                        x.MarkTopLevelExpression(expr.Value) // todo: replace with normal synExpr
                    x.Done(mark, ElementType.RECORD_EXPR_BINDING)
            x.Done(range, mark, ElementType.RECORD_EXPR)

        | SynExpr.AnonRecd(_,copyInfo,fields,range) ->
            let mark = x.Mark(range)
            match copyInfo with
            | Some (expr, _) -> x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
            | _ -> ()

            for IdentRange idRange, expr in fields do
                let mark = x.Mark(idRange)
                x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
                x.Done(mark, ElementType.RECORD_EXPR_BINDING)
            x.Done(range, mark, ElementType.RECORD_EXPR)

        | SynExpr.New(_,t,expr,range) ->
            let mark = x.Mark(range)
            x.ProcessSynType(t)
            x.ProcessExpression(expr)
            x.Done(range, mark, ElementType.NEW_EXPR)

        | SynExpr.ObjExpr(t,args,bindings,interfaceImpls,_,range) ->
            let mark = x.Mark(range)
            x.ProcessSynType(t)
            match args with
            | Some (expr,_) -> x.ProcessExpression(expr)
            | _ -> ()

            for b in bindings do
                x.ProcessBinding(b, true)

            for InterfaceImpl(interfaceType,bindings,range) in interfaceImpls do
                let mark = x.Mark(range)
                x.ProcessSynType(interfaceType)
                for b in bindings do
                    x.ProcessBinding(b, true)
                x.Done(range, mark, ElementType.OBJ_EXPR_SECONDARY_INTERFACE)

            x.Done(range, mark, ElementType.OBJ_EXPR)

        | SynExpr.While(_,whileExpr,doExpr,_) ->
            x.ProcessExpression(whileExpr)
            x.ProcessExpression(doExpr)

        | SynExpr.For(_,id,idBody,_,toBody,doBody,_) ->
            x.ProcessLocalId id
            x.ProcessExpression(idBody)
            x.ProcessExpression(toBody)
            x.ProcessExpression(doBody)

        | SynExpr.ForEach(_,_,_,pat,enumExpr,bodyExpr,_) ->
            x.ProcessPat(pat, true, false)
            x.ProcessExpression(enumExpr)
            x.ProcessExpression(bodyExpr)

        | SynExpr.ArrayOrListOfSeqExpr(_,expr,_)
        | SynExpr.CompExpr(_,_,expr,_) ->
            x.ProcessExpression expr

        | SynExpr.Lambda(_,_,pats,expr,_) ->
            x.ProcessSimplePatterns pats
            x.ProcessExpression(expr)

        | SynExpr.MatchLambda(_,_,cases,_,_) ->
            for case in cases do
                x.ProcessMatchClause case

        | SynExpr.Match(_,expr,cases,_) ->
            x.ProcessExpression(expr)
            for case in cases do
                x.ProcessMatchClause case

        | SynExpr.Do(expr,_)
        | SynExpr.Assert(expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.TypeApp(expr,lt,typeArgs,_,rt,_,r) ->
            x.ProcessExpression(expr)
            let mark = x.Mark(lt)
            for t in typeArgs do x.ProcessSynType t
            let endRange = if rt.IsSome then rt.Value else r
            x.Done(endRange, mark, ElementType.TYPE_ARGUMENT_LIST)

        | SynExpr.LetOrUse(_,_,bindings,bodyExpr,_) ->
            for binding in bindings do
                x.ProcessBinding(binding, true)
            x.ProcessExpression(bodyExpr)

        | SynExpr.TryWith(tryExpr,_,withCases,_,_,_,_) ->
            x.ProcessExpression(tryExpr)
            for case in withCases do
                x.ProcessMatchClause case

        | SynExpr.TryFinally(tryExpr,finallyExpr,_,_,_) ->
            x.ProcessExpression(tryExpr)
            x.ProcessExpression(finallyExpr)

        | SynExpr.Lazy(expr,_) -> x.ProcessExpression(expr)

        | SynExpr.IfThenElse(ifExpr,thenExpr,elseExprOpt,_,_,_,_) ->
            x.ProcessExpression(ifExpr)
            x.ProcessExpression(thenExpr)
            if elseExprOpt.IsSome then
                x.ProcessExpression(elseExprOpt.Value)

        | SynExpr.Ident(IdentRange range) ->
            x.MarkAndDone(range, ElementType.IDENT_EXPR)

        | SynExpr.LongIdent(_,lid,_,range) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid.Lid)
            x.Done(range, mark, ElementType.LONG_IDENT_EXPR)

        | SynExpr.LongIdentSet(lid,expr,range) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid.Lid)
            x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
            x.Done(range, mark, ElementType.LONG_IDENT_SET_EXPR)

        | SynExpr.DotGet(expr,_,_,_) ->
            x.ProcessExpression(expr)

        | SynExpr.DotSet(expr1,lid,expr2,range) ->
            let mark = x.Mark(range)
            x.MarkTopLevelExpression(expr1) // todo: replace with normal synExpr
            x.ProcessLongIdentifier(lid.Lid)
            x.ProcessExpression(expr2)
            x.Done(range, mark, ElementType.DOT_SET_EXPR)

        | SynExpr.NamedIndexedPropertySet(_,expr1,expr2,_) ->
            x.ProcessExpression(expr1)
            x.ProcessExpression(expr2)

        | SynExpr.DotNamedIndexedPropertySet(expr1,_,expr2,expr3,_) ->
            x.ProcessExpression(expr1)
            x.ProcessExpression(expr2)
            x.ProcessExpression(expr3)

        | SynExpr.DotIndexedGet(expr,indexerArgs,_,_) ->
            x.ProcessExpression(expr)
            for arg in indexerArgs do
                x.ProcessIndexerArg(arg)

        | SynExpr.DotIndexedSet(expr1,indexerArgs,expr2,_,_,_) ->
            x.ProcessExpression(expr1)
            for arg in indexerArgs do
                x.ProcessIndexerArg(arg)
            x.ProcessExpression(expr2)

        | SynExpr.TypeTest(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.TYPE_TEST_EXPR)

        | SynExpr.Upcast(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.UPCAST_EXPR)

        | SynExpr.Downcast(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.DOWNCAST_EXPR)

        | SynExpr.InferredUpcast(expr,_)
        | SynExpr.InferredDowncast(expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.Null(_) -> ()

        | SynExpr.AddressOf(_,expr,_,_)
        | SynExpr.TraitCall(_,_,expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.JoinIn(expr1,_,expr2,_) ->
            x.ProcessExpression(expr1)
            x.ProcessExpression(expr2)

        | SynExpr.ImplicitZero(_) -> ()

        | SynExpr.YieldOrReturn(_,expr,_)
        | SynExpr.YieldOrReturnFrom(_,expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.LetOrUseBang(_,_,_,pat,expr,inExpr,_) ->
            x.ProcessPat(pat, true, false)
            x.ProcessExpression(expr)
            x.ProcessExpression(inExpr)

        | SynExpr.DoBang(expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.LibraryOnlyStaticOptimization(_)
        | SynExpr.LibraryOnlyUnionCaseFieldGet(_)
        | SynExpr.LibraryOnlyUnionCaseFieldSet(_)
        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.ArbitraryAfterError(_)
        | SynExpr.FromParseError(_)
        | SynExpr.DiscardAfterMissingQualificationAfterDot(_) -> ()

        | SynExpr.Fixed(expr,_) ->
            x.ProcessExpression(expr)

        | SynExpr.Sequential(_,_,expr1,expr2,_) ->
            x.ProcessExpression(expr1)
            x.ProcessExpression(expr2)

        | Apps (first, next) ->
            x.ProcessExpression(first)
            for expr in next do
                x.ProcessExpression(expr)

        | _ -> ()

    member x.MarkListExpr(exprs, range, elementType) =
        let mark = x.Mark(range)
        for expr in exprs do
            x.ProcessExpression(expr)
        x.Done(range, mark, elementType)

    member x.MarkTypeExpr(expr, typ, range, elementType) =
        let mark = x.Mark(range)
        x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr
        x.MarkOtherType(typ)
        x.Done(range, mark, elementType)

    member x.ProcessMatchClause(Clause(pat,whenExpr,expr,_,_) as clause) =
        let range = clause.Range
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)

        x.ProcessPat(pat, true, false)
        match whenExpr with
        | Some expr -> x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr for local matches
        | _ -> ()

        x.MarkTopLevelExpression(expr) // todo: replace with normal synExpr for local matches
        x.Done(range, mark, ElementType.MATCH_CLAUSE)

    member x.ProcessIndexerArg(arg) =
        for expr in arg.Exprs do
            x.ProcessExpression(expr)
