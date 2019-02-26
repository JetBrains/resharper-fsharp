namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.PrettyNaming

type internal FSharpImplTreeBuilder(file, lexer, decls, lifetime) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for decl in decls do
            x.ProcessTopLevelDeclaration(decl)
        x.FinishFile(mark, ElementType.F_SHARP_IMPL_FILE)

    member private x.ProcessTopLevelDeclaration (SynModuleOrNamespace(lid,_,isModule,decls,_,attrs,_,range)) =
        let mark, elementType = x.StartTopLevelDeclaration lid attrs isModule range
        for decl in decls do x.ProcessModuleMemberDeclaration decl
        x.FinishTopLevelDeclaration mark range elementType  

    member internal x.ProcessModuleMemberDeclaration moduleMember =
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
            x.MarkOtherExpr(expr)
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
                    x.MarkOtherExpr(args)
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

                | SynMemberDefn.Member(Binding(_,_,_,_,_,_,valData,headPat,ret,expr,_,_),range) ->
                    let elType =
                        match headPat with
                        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOpt,memberParams,_,_) ->
                            match lid with
                            | [id] ->
                                match valData with
                                | SynValData(Some (flags),_,_) when flags.MemberKind = MemberKind.Constructor ->
                                    x.ProcessParams(memberParams, true, true) // todo: should check isLocal
                                    x.MarkOtherExpr(expr)
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
                    for binding in bindings do
                        x.ProcessBinding(binding, false)
                    ElementType.LET

                | SynMemberDefn.AbstractSlot(ValSpfn(_,_,typeParams,_,_,_,_,_,_,_,_),_,range) as slot ->
                    match typeParams with
                    | SynValTyparDecls(typeParams,_,_) ->
                        x.ProcessTypeParametersOfType typeParams range true
                    ElementType.ABSTRACT_SLOT

                | SynMemberDefn.ValField(Field(_,_,_,_,_,_,_,_),_) ->
                    ElementType.VAL_FIELD

                | SynMemberDefn.AutoProperty(_,_,_,_,_,_,_,_,expr,_,_) ->
                    x.ProcessLocalExpression expr
                    ElementType.AUTO_PROPERTY

                | _ -> ElementType.OTHER_TYPE_MEMBER

            x.Done(typeMember.Range, mark, memberType)

    member x.ProcessMemberDeclaration id (typeParamsOpt: SynValTyparDecls option) memberParams expr range =
        match typeParamsOpt with
        | Some(SynValTyparDecls(typeParams,_,_)) ->
            x.ProcessTypeParametersOfType typeParams range true
        | _ -> ()
        x.ProcessParams(memberParams, true, true) // todo: should check isLocal
        x.MarkOtherExpr(expr)

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

            | SynPat.Tuple (pats, _)
            | SynPat.StructTuple (pats, _)
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

    member x.MarkOtherExpr(ExprRange range as expr) =
        let mark = x.Mark(range)
        x.ProcessLocalExpression(expr)
        x.Done(range, mark, ElementType.OTHER_EXPR)

    member x.MarkOtherType(TypeRange range as typ) =
        let mark = x.Mark(range)
        x.ProcessSynType(typ)
        x.Done(range, mark, ElementType.OTHER_TYPE)

    member x.ProcessBinding(Binding(_,_,_,_,attrs,_,_,headPat,_, expr,_,_) as binding, isLocal: bool) =
        let mark =
            match attrs with
            | [] -> x.Mark(binding.StartPos)
            | { Range = r } :: _ ->
                let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, r)
                x.ProcessAttributes(attrs)
                mark

        x.ProcessPat(headPat, isLocal, true)
        x.MarkOtherExpr(expr)

        let elementType = if isLocal then ElementType.LOCAL_BINDING else ElementType.TOP_BINDING
        x.Done(binding.RangeOfBindingAndRhs, mark, elementType)

    member x.ProcessLocalExpression (expr: SynExpr) =
        match expr with
        | SynExpr.Paren(expr,_,_,_)
        | SynExpr.Quote(_,_,expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Const(synConst, range) ->
            match synConst with
            | SynConst.Unit ->
                x.MarkAndDone(range, ElementType.CONST_EXPR)
            | _ -> ()

        | SynExpr.Typed(expr,synType,_) ->
            x.ProcessLocalExpression expr
            x.ProcessSynType synType

        | SynExpr.Tuple(exprs,_,range)
        | SynExpr.StructTuple(exprs,_,range) ->
            x.MarkListExpr(exprs, range, ElementType.TUPLE_EXPR)

        | SynExpr.ArrayOrList(_,exprs,range) ->
            x.MarkListExpr(exprs, range, ElementType.ARRAY_OR_LIST_EXPR)

        | SynExpr.Record(_,copyInfoOpt,fields,range) ->
            let mark = x.Mark(range)
            match copyInfoOpt with
            | Some (expr,_) -> x.MarkOtherExpr(expr)
            | _ -> ()

            for (lid, _), expr, _ in fields do
                let lid = lid.Lid
                match lid, expr with
                | [], None -> ()
                | [], Some (ExprRange range as expr) ->
                    let mark = x.Mark(range)
                    x.MarkOtherExpr(expr)
                    x.Done(mark, ElementType.RECORD_EXPR_BINDING)

                | IdentRange headRange :: _, expr ->
                    let mark = x.Mark(headRange)
                    x.ProcessLongIdentifier(lid)
                    if expr.IsSome then
                        x.MarkOtherExpr(expr.Value)
                    x.Done(mark, ElementType.RECORD_EXPR_BINDING)
            x.Done(range, mark, ElementType.RECORD_EXPR)

        | SynExpr.New(_,t,expr,_) ->
            x.ProcessSynType(t)
            x.ProcessLocalExpression(expr)

        | SynExpr.ObjExpr(t,args,bindings,interfaceImpls,_,_) ->
            x.ProcessSynType(t)
            match args with
            | Some (expr,_) -> x.ProcessLocalExpression(expr)
            | _ -> ()

            for b in bindings do
                x.ProcessBinding(b, true)

            for InterfaceImpl(_,bindings,_) in interfaceImpls do
                for b in bindings do x.ProcessBinding(b, true) // todo: it looks weird, add tests

        | SynExpr.While(_,whileExpr,doExpr,_) ->
            x.ProcessLocalExpression whileExpr
            x.ProcessLocalExpression doExpr

        | SynExpr.For(_,id,idBody,_,toBody,doBody,_) ->
            x.ProcessLocalId id
            x.ProcessLocalExpression idBody
            x.ProcessLocalExpression toBody
            x.ProcessLocalExpression doBody

        | SynExpr.ForEach(_,_,_,pat,enumExpr,bodyExpr,_) ->
            x.ProcessPat(pat, true, false)
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
            let mark = x.Mark(lt)
            for t in typeArgs do x.ProcessSynType t
            let endRange = if rt.IsSome then rt.Value else r
            x.Done(endRange, mark, ElementType.TYPE_ARGUMENT_LIST)

        | SynExpr.LetOrUse(_,_,bindings,bodyExpr,_) ->
            for binding in bindings do
                x.ProcessBinding(binding, true)
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

        | SynExpr.LongIdentSet(lid,expr,range) ->
            let mark = x.Mark(range)
            x.ProcessLongIdentifier(lid.Lid)
            x.MarkOtherExpr(expr)
            x.Done(range, mark, ElementType.LONG_IDENT_SET_EXPR)

        | SynExpr.DotGet(expr,_,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.DotSet(expr1,lid,expr2,range) ->
            let mark = x.Mark(range)
            x.MarkOtherExpr(expr1)
            x.ProcessLongIdentifier(lid.Lid)
            x.ProcessLocalExpression(expr2)
            x.Done(range, mark, ElementType.DOT_SET_EXPR)

        | SynExpr.NamedIndexedPropertySet(_,expr1,expr2,_) ->
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

        | SynExpr.TypeTest(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.TYPE_TEST_EXPR)

        | SynExpr.Upcast(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.UPCAST_EXPR)

        | SynExpr.Downcast(expr,typ,range) ->
            x.MarkTypeExpr(expr, typ, range, ElementType.DOWNCAST_EXPR)

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
            x.ProcessPat(pat, true, false)
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

    member x.MarkListExpr(exprs, range, elementType) =
        let mark = x.Mark(range)
        for e in exprs do
            x.ProcessLocalExpression(e)
        x.Done(range, mark, elementType)

    member x.MarkTypeExpr(expr, typ, range, elementType) =
        let mark = x.Mark(range)
        x.MarkOtherExpr(expr)
        x.MarkOtherType(typ)
        x.Done(range, mark, elementType)

    member x.ProcessMatchClause(Clause(pat,whenExpr,expr,_,_) as clause) =
        let range = clause.Range
        let mark = x.MarkTokenOrRange(FSharpTokenType.BAR, range)

        x.ProcessPat(pat, true, false)
        match whenExpr with
        | Some expr -> x.MarkOtherExpr(expr)
        | _ -> ()

        x.MarkOtherExpr(expr)
        x.Done(range, mark, ElementType.MATCH_CLAUSE)

    member x.ProcessIndexerArg(arg) =
        for expr in arg.Exprs do
            x.ProcessLocalExpression(expr)
