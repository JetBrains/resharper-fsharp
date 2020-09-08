namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System.Collections.Generic
open FSharp.Compiler
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.PrettyNaming
open FSharp.Compiler.Range
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

type FSharpImplTreeBuilder(lexer, document, decls, lifetime, projectedOffset, lineShift) =
    inherit FSharpTreeBuilderBase(lexer, document, lifetime, projectedOffset, lineShift)

    /// FCS splits some declarations into separate fake ones:
    ///   * property declaration when both getter and setter bodies are present
    ///   * attributes for module-level do
    let mutable unfinishedDeclaration: (int * range * CompositeNodeType) option = None

    new (lexer, document, decls, lifetime) =
        FSharpImplTreeBuilder(lexer, document, decls, lifetime, 0, 0)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for decl in decls do
            x.ProcessTopLevelDeclaration(decl)
        x.FinishFile(mark, ElementType.F_SHARP_IMPL_FILE)

    member x.ProcessTopLevelDeclaration(SynModuleOrNamespace(lid, _, moduleKind, decls, _, attrs, _, range)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, moduleKind, range)
        for decl in decls do
            x.ProcessModuleMemberDeclaration(decl)
        x.EnsureMembersAreFinished()
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberDeclaration(moduleMember) =
        match unfinishedDeclaration with
        | None -> ()
        | Some(mark, range, elementType) ->
            match moduleMember with
            | SynModuleDecl.DoExpr _ -> ()
            | _ ->
                unfinishedDeclaration <- None
                x.Done(range, mark, elementType)

        match moduleMember with
        | SynModuleDecl.NestedModule(ComponentInfo(attrs, _, _, lid, _, _, _, _), _ ,decls, _, range) ->
            let mark = x.MarkAttributesOrIdOrRange(attrs, List.tryHead lid, range)
            for decl in decls do
                x.ProcessModuleMemberDeclaration(decl)
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(typeDefns, range) ->
            let mark = x.Mark(typeDefnGroupStartPos typeDefns range)
            match typeDefns with
            | [] -> ()
            | TypeDefn(ComponentInfo(attrs, _, _, _, _, _, _, _), _, _, _) :: _ ->
                x.ProcessOuterAttrs(attrs, range)

            for typeDefn in typeDefns do
                x.ProcessTypeDefn(typeDefn)
            x.Done(range, mark, ElementType.TYPE_DECLARATION_GROUP)

        | SynModuleDecl.Exception(SynExceptionDefn(exn, memberDefns, range), _) ->
            let mark = x.StartException(exn)
            for memberDefn in memberDefns do
                x.ProcessTypeMember(memberDefn)
            x.EnsureMembersAreFinished()
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleDecl.Open(lidWithDots, range) ->
            let mark = x.MarkTokenOrRange(FSharpTokenType.OPEN, range)
            x.ProcessNamedTypeReference(lidWithDots.Lid)
            x.Done(range, mark, ElementType.OPEN_STATEMENT)

        | SynModuleDecl.Let(_, bindings, range) ->
            let letMark = x.Mark(letBindingGroupStartPos bindings range)
            match bindings with
            | [] -> ()
            | Binding(_, _, _, _, attrs, _, _, _, _, _, _ , _) :: _ ->
                x.ProcessOuterAttrs(attrs, range)

            for binding in bindings do
                x.ProcessTopLevelBinding(binding, range)
            x.Done(range, letMark, ElementType.LET_BINDINGS_DECLARATION)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.DoExpr(_, expr, range) ->
            let mark =
                match unfinishedDeclaration with
                | Some(mark, _, _) ->
                    unfinishedDeclaration <- None
                    mark
                | _ ->
                    x.AdvanceToTokenOrRangeStart(FSharpTokenType.DO, range)
                    x.Mark()

            let expr = x.RemoveDoExpr(expr)
            x.MarkChameleonExpression(expr)
            x.Done(range, mark, ElementType.DO_STATEMENT)

        | SynModuleDecl.Attributes(attributeLists, range) ->
            let mark = x.Mark(range)
            x.ProcessAttributeLists(attributeLists)
            unfinishedDeclaration <- Some(mark, range, ElementType.DO_STATEMENT)

        | SynModuleDecl.ModuleAbbrev(_, lid, range) ->
            let mark = x.Mark(range)
            x.ProcessNamedTypeReference(lid)
            x.Done(range, mark, ElementType.MODULE_ABBREVIATION_DECLARATION)

        | decl ->
            failwithf "unexpected decl: %O" decl

    member x.ProcessTypeDefn(TypeDefn(ComponentInfo(attrs, typeParams, constraints, lid , _, _, _, _), repr, members, range) as typeDefn) =
        match repr with
        | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) ->
            let mark = x.Mark(range)
            x.ProcessReferenceNameSkipLast(lid)
            x.ProcessTypeParametersOfType typeParams constraints range false
            for typeConstraint in constraints do
                x.ProcessTypeConstraint(typeConstraint)
            for extensionMember in members do
                x.ProcessTypeMember(extensionMember)
            x.EnsureMembersAreFinished()
            x.Done(range, mark, ElementType.TYPE_EXTENSION_DECLARATION)
        | _ ->

        let attrs = x.SkipOuterAttrs(attrs, range)
        let mark = x.StartType attrs typeParams constraints lid range
        let elementType =
            match repr with
            | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                x.ProcessSimpleTypeRepresentation(simpleRepr)

            | SynTypeDefnRepr.Exception _ ->
                // This is compiler-internal union case with instances created inside type checker only.
                failwithf "unexpected exception type: %O" typeDefn 

            | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) ->
                ElementType.TYPE_EXTENSION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(kind, members, range) ->
                match kind with
                | TyconDelegate(synType, _) ->
                    match members with
                    | SynMemberDefn.ImplicitCtor _ as ctor :: _ -> x.ProcessTypeMember(ctor)
                    | _ -> ()

                    let mark = x.Mark(range)
                    x.ProcessType(synType)
                    x.Done(range, mark, ElementType.DELEGATE_REPRESENTATION)
                    ElementType.DELEGATE_DECLARATION

                | _ ->

                for m in members do
                    x.ProcessTypeMember m
                x.EnsureMembersAreFinished()

                match kind with
                | TyconClass -> ElementType.CLASS_DECLARATION
                | TyconInterface -> ElementType.INTERFACE_DECLARATION
                | TyconStruct -> ElementType.STRUCT_DECLARATION
                | _ -> ElementType.OBJECT_TYPE_DECLARATION

        for m in members do
            x.ProcessTypeMember m
        x.EnsureMembersAreFinished()
        x.Done(range, mark, elementType)

    member x.ProcessTypeMember(typeMember: SynMemberDefn) =
        let mark =
            match unfinishedDeclaration with
            | Some(mark, unfinishedRange, _) when unfinishedRange = typeMember.Range ->
                unfinishedDeclaration <- None
                mark

            | _ ->
                match typeMember with
                | SynMemberDefn.ImplicitCtor _ ->
                    while (isNotNull x.TokenType && x.TokenType.IsWhitespace) && not x.Eof do
                        x.AdvanceLexer()
                | _ -> ()
                
                x.MarkAttributesOrIdOrRange(typeMember.OuterAttributes, None, typeMember.Range)

        let memberType =
            match typeMember with
            | SynMemberDefn.ImplicitCtor(_, attrs, args, selfId, _) ->
                x.ProcessAttributeLists(attrs)
                x.ProcessImplicitCtorSimplePats(args)
                x.ProcessCtorSelfId(selfId)
                ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

            | SynMemberDefn.ImplicitInherit(baseType, args, _, _) ->
                x.ProcessTypeAsTypeReferenceName(baseType)
                x.MarkChameleonExpression(args)
                ElementType.TYPE_INHERIT

            | SynMemberDefn.Interface(interfaceType, interfaceMembersOpt , _) ->
                x.ProcessTypeAsTypeReferenceName(interfaceType)
                match interfaceMembersOpt with
                | Some members ->
                    for m in members do
                        x.ProcessTypeMember(m)
                    x.EnsureMembersAreFinished()
                | _ -> ()
                ElementType.INTERFACE_IMPLEMENTATION

            | SynMemberDefn.Inherit(baseType, _, _) ->
                try x.ProcessTypeAsTypeReferenceName(baseType)
                with _ -> () // Getting type range throws an exception if base type lid is empty.
                ElementType.INTERFACE_INHERIT

            | SynMemberDefn.Member(binding, range) ->
                x.ProcessMemberBinding(mark, binding, range)

            | SynMemberDefn.LetBindings([Binding(kind = SynBindingKind.DoBinding; expr = expr)], _, _, range) ->
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.DO, range)
                let expr = x.RemoveDoExpr(expr)
                x.MarkChameleonExpression(expr)
                ElementType.DO_STATEMENT

            | SynMemberDefn.LetBindings(bindings, _, _, range) ->
                for binding in bindings do
                    x.ProcessTopLevelBinding(binding, range)
                ElementType.LET_BINDINGS_DECLARATION

            | SynMemberDefn.AbstractSlot(ValSpfn(_, _, typeParams, synType, _, _, _, _, _, _, _), _, range) ->
                match typeParams with
                | SynValTyparDecls(typeParams, _, constraints) ->
                    x.ProcessTypeParametersOfType typeParams constraints range true
                    for typeConstraint in constraints do
                        x.ProcessTypeConstraint(typeConstraint)
                x.ProcessType(synType)
                ElementType.ABSTRACT_MEMBER_DECLARATION

            | SynMemberDefn.ValField(Field(_, _, _, synType, _, _, _, _), _) ->
                x.ProcessType(synType)
                ElementType.VAL_FIELD_DECLARATION

            | SynMemberDefn.AutoProperty(_, _, _, synTypeOpt, _, _, _, _, expr, accessorClause, _) ->
                match synTypeOpt with
                | Some synType -> x.ProcessType(synType)
                | _ -> ()
                x.MarkChameleonExpression(expr)
                match accessorClause with
                | Some clause -> x.MarkAndDone(clause, ElementType.ACCESSORS_NAMES_CLAUSE)
                | _ -> ()
                ElementType.AUTO_PROPERTY_DECLARATION

            | _ -> ElementType.OTHER_TYPE_MEMBER

        if unfinishedDeclaration.IsNone then
            x.Done(typeMember.Range, mark, memberType)

    member x.ProcessMemberBinding(mark, Binding(_, _, _, _, _, _, valData, headPat, returnInfo, expr, _, _), range) =
        let elType =
            match headPat with
            | SynPat.LongIdent(LongIdentWithDots(lid, _), accessorId, typeParamsOpt, memberParams, _, _) ->
                match lid with
                | [_] ->
                    match valData with
                    | SynValData(Some(flags), _, selfId) when flags.MemberKind = MemberKind.Constructor ->
                        x.ProcessPatternParams(memberParams, true, true) // todo: should check isLocal
                        x.ProcessCtorSelfId(selfId)

                        x.MarkChameleonExpression(expr)
                        ElementType.MEMBER_CONSTRUCTOR_DECLARATION

                    | _ ->
                        match accessorId with
                        | Some ident ->
                            x.ProcessAccessor(ident, memberParams, expr)
                            ElementType.MEMBER_DECLARATION
                        | _ ->

                        x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                        ElementType.MEMBER_DECLARATION

                | selfId :: _ :: _ ->
                    x.MarkAndDone(selfId.idRange, ElementType.MEMBER_SELF_ID)

                    match accessorId with
                    | Some ident ->
                        x.ProcessAccessor(ident, memberParams, expr)
                        ElementType.MEMBER_DECLARATION
                    | _ ->

                    x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range)
                    ElementType.MEMBER_DECLARATION

                | _ -> ElementType.OTHER_TYPE_MEMBER

            | SynPat.Named _ ->
                // In some cases patterns for static members inside records are represented this way.
                x.ProcessMemberDeclaration(None, SynArgPats.Pats [], returnInfo, expr, range)
                ElementType.MEMBER_DECLARATION

            | _ -> ElementType.OTHER_TYPE_MEMBER

        match valData with
        | SynValData(Some(flags), _, _) when
                flags.MemberKind = MemberKind.PropertyGet || flags.MemberKind = MemberKind.PropertySet ->
            if expr.Range.End <> range.End then
                unfinishedDeclaration <- Some(mark, range, ElementType.MEMBER_DECLARATION)

        | _ -> ()

        elType
    
    member x.ProcessAccessor(IdentRange range, memberParams, expr) =
        let mark = x.Mark(range)
        x.ProcessPatternParams(memberParams, true, true)
        x.MarkChameleonExpression(expr)
        x.Done(mark, ElementType.ACCESSOR_DECLARATION)

    member x.EnsureMembersAreFinished() =
        match unfinishedDeclaration with
        | Some(mark, unfinishedRange, elementType) ->
            unfinishedDeclaration <- None
            x.Done(unfinishedRange, mark, elementType)
        | _ -> ()

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
        x.ProcessAttributeLists(attrs)
        x.ProcessType(returnType)
        x.Done(range, mark, ElementType.RETURN_TYPE_INFO)

    member x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr, range) =
        match typeParamsOpt with
        | Some(SynValTyparDecls(typeParams, _, constraints)) ->
            x.ProcessTypeParametersOfType typeParams constraints range true // todo: of type?..
        | _ -> ()

        x.ProcessMemberParams(memberParams, true, true) // todo: should check isLocal
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
                | SynPat.Wild(range) when Range.equals id.idRange range ->
                    let mark = x.Mark(id.idRange)
                    if IsActivePatternName id.idText then x.ProcessActivePatternId(id, isLocal)
                    x.Done(id.idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)
                    if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT

                | _ ->
                    x.ProcessPat(pat, isLocal, false)
                    if isLocal then ElementType.LOCAL_AS_PAT else ElementType.TOP_AS_PAT

            | SynPat.LongIdent(lid, _, typars, args, _, _) ->
                match lid.Lid, args with
                | [id], Pats([SynPat.Tuple(_, pats, _)]) when id.idText = "op_ColonColon" ->
                    for pat in pats do
                        x.ProcessPat(pat, isLocal, false)

                    ElementType.LIST_CONS_PAT

                | _ ->

                // todo: replace all lids
                match lid.Lid with
                | [ IdentRange idRange as id ] ->
                    let mark = x.Mark(idRange)
                    if IsActivePatternName id.idText then
                        x.ProcessActivePatternId(id, isLocal)
                    x.Done(idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)
    
                    match typars with
                    | None -> ()
                    | Some(SynValTyparDecls(typarDecls, _, _)) ->

                    for typarDecl in typarDecls do
                        x.ProcessTypeParameter(typarDecl, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)

                | lid ->
                    x.ProcessReferenceName(lid)

                if args.IsEmpty then
                    if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT
                else
                    x.ProcessPatternParams(args, isLocal || isTopLevelPat, false)
                    if isLocal then ElementType.LOCAL_PARAMETERS_OWNER_PAT else ElementType.TOP_PARAMETERS_OWNER_PAT

            | SynPat.Typed(pat, synType, _) ->
                x.ProcessPat(pat, isLocal, false)
                x.ProcessType(synType)
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

            | SynPat.Paren(SynPat.Const(SynConst.Unit, _), _) ->
                ElementType.UNIT_PAT

            | SynPat.Paren(pat, _) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.PAREN_PAT

            | SynPat.Record(pats, _) ->
                for (lid, IdentRange range), pat in pats do
                    let fieldMark =
                        match lid with
                        | IdentRange headRange :: _ ->
                            let fieldMark = x.Mark(headRange)
                            let referenceNameMark = x.Mark()
                            x.ProcessReferenceName(lid)
                            x.Done(range, referenceNameMark, ElementType.EXPRESSION_REFERENCE_NAME)
                            fieldMark

                        | _ ->
                            let fieldMark = x.Mark(range)
                            x.MarkAndDone(range, ElementType.EXPRESSION_REFERENCE_NAME)
                            fieldMark

                    x.ProcessPat(pat, isLocal, false)
                    x.Done(fieldMark, ElementType.FIELD_PAT)
                ElementType.RECORD_PAT

            | SynPat.IsInst(typ, _) ->
                x.ProcessType(typ)
                ElementType.IS_INST_PAT

            | SynPat.Wild _ ->
                ElementType.WILD_PAT

            | SynPat.Attrib(pat, attrs, _) ->
                x.ProcessAttributeLists(attrs)
                x.ProcessPat(pat, isLocal, false)
                ElementType.ATTRIB_PAT

            | SynPat.Const _ ->
                ElementType.LITERAL_PAT

            | SynPat.OptionalVal(id, _) ->
                let mark = x.Mark(id.idRange)
                x.MarkAndDone(id.idRange, ElementType.EXPRESSION_REFERENCE_NAME)
                x.Done(mark, if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT)
                ElementType.OPTIONAL_VAL_PAT

            | _ ->
                ElementType.OTHER_PAT

        x.Done(range, mark, elementType)

    member x.ProcessListLikePat(pats, isLocal) =
        for pat in pats do
            x.ProcessPat(pat, isLocal, false)

    member x.ProcessPatternParams(args: SynArgPats, isLocal, markMember) =
        match args with
        | Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | NamePatPairs(idsAndPats, _) ->
            for IdentRange range, pat in idsAndPats do
                let mark = x.Mark(range)
                x.MarkAndDone(range, ElementType.EXPRESSION_REFERENCE_NAME)
                x.ProcessParam(pat, isLocal, markMember)
                x.Done(range, mark, ElementType.FIELD_PAT)

    member x.ProcessMemberParams(args: SynArgPats, isLocal, markMember) =
        match args with
        | Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | _ -> failwithf "args: %O" args

    member x.ProcessParam(PatRange range as pat, isLocal, markMember) =
        if not markMember then x.ProcessPat(pat, isLocal, false) else

        let mark = x.Mark(range)
        x.ProcessPat(pat, isLocal, false)
        x.Done(range, mark, ElementType.PARAMETERS_PATTERN_DECLARATION)

    member x.MarkOtherType(TypeRange range as typ) =
        let mark = x.Mark(range)
        x.ProcessType(typ)
        x.Done(range, mark, ElementType.UNSUPPORTED_TYPE_USAGE)

    member x.SkipOuterAttrs(attrs: SynAttributeList list, outerRange: range) =
        match attrs with
        | [] -> []
        | { Range = r } :: rest ->
            if posGt r.End outerRange.Start then attrs else
            x.SkipOuterAttrs(rest, outerRange)

    member x.ProcessTopLevelBinding(Binding(_, kind, _, _, attrs, _, _ , headPat, returnInfo, expr, _, _) as binding, letRange) =
        let expr = x.FixExpresion(expr)

        match kind with
        | StandaloneExpression
        | DoBinding -> x.MarkChameleonExpression(expr)
        | _ ->

        let mark =
            let attrs = x.SkipOuterAttrs(attrs, letRange)
            match attrs with
            | [] -> x.Mark(binding.StartPos)
            | { Range = r } :: _ ->
                let mark = x.MarkTokenOrRange(FSharpTokenType.LBRACK_LESS, r)
                x.ProcessAttributeLists(attrs)
                mark

        x.ProcessPat(headPat, false, true)
        x.ProcessReturnInfo(returnInfo)
        x.MarkChameleonExpression(expr)

        x.Done(binding.RangeOfBindingAndRhs, mark, ElementType.TOP_BINDING)


[<Struct>]
type BuilderStep =
    { Item: obj
      Processor: IBuilderStepProcessor }


and IBuilderStepProcessor =
    abstract Process: step: obj * builder: FSharpExpressionTreeBuilder -> unit


type FSharpExpressionTreeBuilder(lexer, document, lifetime, projectedOffset, lineShift) =
    inherit FSharpImplTreeBuilder(lexer, document, [], lifetime, projectedOffset, lineShift)

    let nextSteps = Stack<BuilderStep>()

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
                x.ProcessAttributeLists(attrs)
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

    member x.PushExpression(synExpr: SynExpr) =
        x.PushStep(synExpr, expressionProcessor)

    member x.PushSequentialExpression(synExpr: SynExpr) =
        x.PushStep(synExpr, sequentialExpressionProcessor)
    
    member x.PushStepList(items, processor: StepListProcessorBase<_>) =
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

        | SynExpr.Const(synConst, _) ->
            let elementType =
                match synConst with
                | SynConst.Unit -> ElementType.UNIT_EXPR
                | _ -> ElementType.LITERAL_EXPR
            x.MarkAndDone(range, elementType)

        | SynExpr.Typed(expr, synType, _) ->
            let typeRange = synType.Range
            Assertion.Assert(rangeContainsRange range typeRange,
                             "rangeContainsRange range synType.Range; {0}; {1}", range, typeRange)

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
            // SynExpr.ArrayOrList is currently only used for error recovery and empty lists in the parser.
            // Non-empty SynExpr.ArrayOrList is created in the type checker only.
            Assertion.Assert(List.isEmpty exprs, "Non-empty SynExpr.ArrayOrList: {0}", expr)
            x.MarkAndDone(range, ElementType.ARRAY_OR_LIST_EXPR)

        | SynExpr.AnonRecd(_, copyInfo, fields, _) ->
            x.PushRange(range, ElementType.ANON_RECORD_EXPR)
            if not fields.IsEmpty then
                x.PushStep(fields, anonRecordBindingListRepresentationProcessor)

            match copyInfo with
            | Some(expr, _) -> x.ProcessExpression(expr)
            | _ -> ()

        | SynExpr.Record(baseInfo, copyInfo, fields, _) ->
            x.PushRange(range, ElementType.RECORD_EXPR)
            if not fields.IsEmpty then
                x.PushStep(fields, recordBindingListRepresentationProcessor)

            match baseInfo, copyInfo with
            | Some(typeName, expr, _, _, _), _ ->
                x.ProcessTypeAsTypeReferenceName(typeName)
                x.ProcessExpression(expr)

            | _, Some(expr, _) ->
                x.ProcessExpression(expr)

            | _ -> ()

        | SynExpr.New(_, synType, expr, _) ->
            x.PushRange(range, ElementType.NEW_EXPR)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.ProcessExpression(expr)

        | SynExpr.ObjExpr(synType, args, bindings, interfaceImpls, _, _) ->
            x.PushRange(range, ElementType.OBJ_EXPR)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.PushStepList(interfaceImpls, interfaceImplementationListProcessor)
            x.PushStepList(bindings, objectExpressionMemberListProcessor)

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
            let expr = match expr with | SynExpr.CompExpr(expr = expr) -> expr | _ -> expr
            x.PushRangeAndProcessExpression(expr, range, ElementType.ARRAY_OR_LIST_EXPR)

        | SynExpr.CompExpr(_, _, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.COMPUTATION_EXPR)

        | SynExpr.Lambda(_, inLambdaSeq, args, bodyExpr, _) ->
            // Lambdas get "desugared" by converting to fake nested lambdas and match expressions.
            // Simple patterns like ids are preserved in lambdas and more complex ones are replaced
            // with generated placeholder patterns and go to generated match expressions inside lambda bodies.

            // Generated match expression have have a single generated clause with a generated id pattern.
            // Their ranges overlap with lambda param pattern ranges and they have the same start pos as lambdas. 

            Assertion.Assert(not inLambdaSeq, "Expecting non-generated lambda expression, got:\n{0}", expr)
            x.PushRange(range, ElementType.LAMBDA_EXPR)

            let skippedLambdas = skipGeneratedLambdas bodyExpr
            let parametersMark = x.Mark(args.Range)
            x.ProcessLambdaParameters(expr, skippedLambdas, true)
            x.Done(parametersMark, ElementType.LAMBDA_PARAMETERS_LIST)
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

        | SynExpr.App(_, false, SynExpr.App(_, true, funcExpr, leftArg, _), rightArg, prefixAppRange) ->
            match funcExpr with
            | SynExpr.Ident(id) when id.idText = "op_Range" ->
                x.ProcessRangeExpr(leftArg, rightArg, prefixAppRange)

            | SynExpr.Ident(id) when id.idText = "op_RangeStep" ->
                x.ProcessRangeStepExpr(leftArg, rightArg)

            | _ ->

            x.PushRange(range, ElementType.BINARY_APP_EXPR)
            x.PushExpression(rightArg)
            x.PushExpression(funcExpr)
            x.ProcessExpression(leftArg)

        | SynExpr.App(_, true, (SynExpr.Ident(id) as funcExpr), SynExpr.Tuple(_, [first; second], _, _), _) when
                id.idText = "op_ColonColon" ->

            x.PushRange(range, ElementType.BINARY_APP_EXPR)
            x.PushExpression(second)
            x.PushExpression(funcExpr)
            x.ProcessExpression(first)

        | SynExpr.App(_, isInfix, funcExpr, argExpr, _) ->
            Assertion.Assert(not isInfix, "Expecting prefix app, got: {0}", expr)

            x.PushRange(range, ElementType.PREFIX_APP_EXPR)
            x.PushExpression(argExpr)
            x.ProcessExpression(funcExpr)

        | SynExpr.TypeApp(expr, _, _, _, _, _, _) as typeApp ->
            // Process expression first, then inject type args into it in the processor.
            x.PushStep(typeApp, typeArgsInReferenceExprProcessor)
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
            // Nested ifExpr may have wrong range, e.g. `else` goes inside the nested expr range here:
            // `if true then "a" else if true then "b" else "c"`
            // However, elif expressions actually start this way.
            x.AdvanceToStart(range)
            let isElif = x.TokenType == FSharpTokenType.ELIF

            if not isElif then
                x.AdvanceToTokenOrRangeStart(FSharpTokenType.IF, thenExpr.Range)

            let elementType = if isElif then ElementType.ELIF_EXPR else ElementType.IF_THEN_ELSE_EXPR
            x.PushRangeForMark(range, x.Mark(), elementType)

            if elseExprOpt.IsSome then
                x.PushExpression(elseExprOpt.Value)
            x.PushExpression(thenExpr)
            x.ProcessExpression(ifExpr)

        | SynExpr.Ident _ ->
            x.MarkAndDone(range, ElementType.REFERENCE_EXPR)

        // todo: isOptional
        | SynExpr.LongIdent(_, lid, _, _) ->
            x.ProcessLongIdentifierExpression(lid.Lid, range)

        | SynExpr.LongIdentSet(lid, expr, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.ProcessLongIdentifierExpression(lid.Lid, lid.Range)
            x.ProcessExpression(expr)

        | SynExpr.DotGet(expr, _, lid, _) ->
            x.ProcessLongIdentifierAndQualifierExpression(expr, lid)

        | SynExpr.DotSet(expr1, lid, expr2, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr2)
            x.ProcessLongIdentifierAndQualifierExpression(expr1, lid)

        | SynExpr.Set(expr1, expr2, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr2)
            x.ProcessExpression(expr1)

        | SynExpr.NamedIndexedPropertySet(lid, expr1, expr2, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr2)
            x.PushRange(unionRanges lid.Range expr1.Range, ElementType.NAMED_INDEXER_EXPR)
            x.ProcessLongIdentifierExpression(lid.Lid, lid.Range)
            x.PushRange(expr1.Range, ElementType.INDEXER_ARG_EXPR)
            x.ProcessExpression(expr1)

        | SynExpr.DotNamedIndexedPropertySet(expr1, lid, expr2, expr3, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr3)
            x.PushRange(unionRanges expr1.Range expr2.Range, ElementType.NAMED_INDEXER_EXPR)
            x.PushNamedIndexerArgExpression(expr2)
            x.ProcessLongIdentifierAndQualifierExpression(expr1, lid)

        | SynExpr.DotIndexedGet(expr, _, _, _) as get ->
            x.PushRange(range, ElementType.ITEM_INDEXER_EXPR)
            x.PushStep(get, indexerArgsProcessor)
            x.ProcessExpression(expr)

        | SynExpr.DotIndexedSet(expr1, _, expr2, leftRange, _, _) as set ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushRange(leftRange, ElementType.ITEM_INDEXER_EXPR)
            x.PushExpression(expr2)
            x.PushStep(set, indexerArgsProcessor)
            x.ProcessExpression(expr1)

        | SynExpr.TypeTest(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.TYPE_TEST_EXPR)

        | SynExpr.Upcast(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.UPCAST_EXPR)

        | SynExpr.Downcast(expr, synType, _) ->
            x.MarkTypeExpr(expr, synType, range, ElementType.DOWNCAST_EXPR)

        | SynExpr.InferredUpcast(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.INFERRED_UPCAST_EXPR)

        | SynExpr.InferredDowncast(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.INFERRED_DOWNCAST_EXPR)

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

        | SynExpr.SequentialOrImplicitYield _ ->
            failwithf "Unexpected internal type checker node: %A" expr

        | SynExpr.ImplicitZero _ -> ()

        | SynExpr.YieldOrReturn(_, expr, _) ->
            x.AdvanceToStart(range)
            if x.TokenType == FSharpTokenType.RARROW then
                // Remove fake yield expressions in list comprehensions
                // by replacing `-> a` with `a` in `[ for a in 1 .. 2 -> a ]`.
                x.ProcessExpression(expr)
            else
                x.PushRangeAndProcessExpression(expr, range, ElementType.YIELD_OR_RETURN_EXPR)

        | SynExpr.YieldOrReturnFrom(_, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.YIELD_OR_RETURN_EXPR)

        | SynExpr.LetOrUseBang(_, _, _, pat, expr, ands, inExpr, range) ->
            x.PushRange(range, ElementType.LET_OR_USE_EXPR)
            x.PushExpression(inExpr)
            x.PushStepList(ands, andLocalBindingListProcessor)
            x.PushRangeForMark(expr.Range, x.Mark(pat.Range), ElementType.LOCAL_BINDING)
            x.ProcessPat(pat, true, false)
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
        | SynExpr.DiscardAfterMissingQualificationAfterDot _ ->
            x.MarkAndDone(range, ElementType.FROM_ERROR_EXPR)

        | SynExpr.FromParseError(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.FROM_ERROR_EXPR)

        | SynExpr.Fixed(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.FIXED_EXPR)

        | SynExpr.Sequential(_, _, expr1, expr2, _) ->
            x.PushRange(range, ElementType.SEQUENTIAL_EXPR)
            x.PushSequentialExpression(expr2)
            x.ProcessExpression(expr1)

    member x.ProcessAndLocalBinding(_, _, _, pat: SynPat, expr: SynExpr, _) =
        x.PushRangeForMark(expr.Range, x.Mark(pat.Range), ElementType.LOCAL_BINDING)
        x.ProcessPat(pat, true, false)
        x.ProcessExpression(expr)

    member x.ProcessRangeStepExpr(fromExpr: SynExpr, stepExpr: SynExpr) =
        let toExpr = nextSteps.Pop().Item :?> SynExpr

        // There's one extra app expr for ranges with step specified, remove it.
        let rangeSeqAppExprStep = nextSteps.Pop().Item :?> RangeMarkAndType
        x.Builder.Drop(rangeSeqAppExprStep.Mark)

        x.ProcessRangeStepExpr(fromExpr, ValueSome(stepExpr), toExpr, rangeSeqAppExprStep.Range)

    member x.ProcessRangeExpr(fromExpr: SynExpr, toExpr: SynExpr, r) =
        x.ProcessRangeStepExpr(fromExpr, ValueNone, toExpr, r)

    member x.ProcessRangeStepExpr(fromExpr: SynExpr, stepExpr: SynExpr voption, toExpr: SynExpr, appRange) =
        // Range sequences are hacked to look like function applications.
        // We need to change already pushed builder steps to fix it. 

        let fromRange = fromExpr.Range
        let toRange = toExpr.Range

        let rangeSeqRange = unionRanges fromRange toRange

        // Range sequence expr also contains braces in the fake app expr, mark it as a separate expr node.
        if appRange <> rangeSeqRange then
            x.PushRange(appRange, ElementType.COMPUTATION_EXPR)

        let seqMark = x.Mark(fromRange)
        x.PushRangeForMark(toRange, seqMark, ElementType.RANGE_SEQUENCE_EXPR)
        x.PushExpression(toExpr)

        match stepExpr with
        | ValueSome stepExpr -> x.PushExpression(stepExpr)
        | _ -> ()

        x.ProcessExpression(fromExpr)

    member x.ProcessLongIdentifierExpression(lid, range) =
        let marks = Stack()

        x.AdvanceToStart(range)
        for _ in lid do
            marks.Push(x.Mark())

        for IdentRange idRange in lid do
            let range = if marks.Count <> 1 then idRange else range
            x.Done(range, marks.Pop(), ElementType.REFERENCE_EXPR)

    member x.ProcessLongIdentifierAndQualifierExpression(ExprRange exprRange as expr, lid) =
        x.AdvanceToStart(exprRange)

        let mutable isFirstId = true 
        for IdentRange idRange in List.rev lid.Lid do
            let range = if not isFirstId then idRange else lid.Range
            x.PushRangeForMark(range, x.Mark(), ElementType.REFERENCE_EXPR)
            isFirstId <- false

        x.ProcessExpression(expr)
    
    member x.ProcessLambdaParameters(expr, outerBodyExpr, topLevel) =
        match expr with
        | SynExpr.Lambda(_, inLambdaSeq, pats, bodyExpr, _) when inLambdaSeq <> topLevel ->
            x.ProcessLambdaParameters(pats, bodyExpr, outerBodyExpr)

        | _ -> ()

    member x.ProcessLambdaParameters(pats: SynSimplePats, lambdaBody: SynExpr, outerBodyExpr) =
        match pats with
        | SynSimplePats.SimplePats(pats, range) ->
            match pats with
            | [] ->
                x.MarkAndDone(range, ElementType.UNIT_PAT)
                x.ProcessLambdaParameters(lambdaBody, outerBodyExpr, false)

            | [pat] ->
                if posLt range.Start pat.Range.Start then
                    let mark = x.Mark(range)
                    x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, false)
                    x.Done(range, mark, ElementType.PAREN_PAT)
                    x.ProcessLambdaParameters(lambdaBody, outerBodyExpr, false)
                else
                    x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, true)

            | pats ->
                let parenMark = x.Mark(range)
                let tupleMark = x.Mark(range)
                x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, false)
                x.Done(tupleMark, ElementType.TUPLE_PAT)
                x.Done(parenMark, ElementType.PAREN_PAT)
                x.ProcessLambdaParameters(lambdaBody, outerBodyExpr, false)

        | SynSimplePats.Typed _ ->
            failwithf "Expecting SimplePats, got:\n%A" pats

    member x.ProcessOneLambdaParam(pats: SynSimplePat list, lambdaBody: SynExpr, outerBodyExpr, processNext) =
        match pats with
        | [] ->
            if processNext then
                x.ProcessLambdaParameters(lambdaBody, outerBodyExpr, false)

        | pat :: pats ->

        match pat with
        | SynSimplePat.Id(_, _, isGenerated, _, _, range) ->
            if not isGenerated then
                let mark = x.Mark(range)
                x.MarkAndDone(range, ElementType.EXPRESSION_REFERENCE_NAME)
                x.Done(range, mark, ElementType.LOCAL_REFERENCE_PAT)
                x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, processNext)
            else
                match outerBodyExpr with
                | SynExpr.Match(_, _, [ Clause(pat, whenExpr, innerExpr, _, _) as clause ], matchRange) when
                        matchRange.Start = clause.Range.Start ->

                    Assertion.Assert(whenExpr.IsNone, "whenExpr.IsNone")
                    x.ProcessPat(pat, true, false)
                    x.ProcessOneLambdaParam(pats, lambdaBody, innerExpr, processNext)

                | _ ->
                    failwithf "Expecting generated match expression, got:\n%A" lambdaBody

        | SynSimplePat.Typed(pat, patType, range) ->
            let mark = x.Mark(range)
            x.ProcessOneLambdaParam([pat], lambdaBody, outerBodyExpr, false)
            x.ProcessType(patType)
            x.Done(range, mark, ElementType.TYPED_PAT)
            x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, processNext)

        | SynSimplePat.Attrib(pat, attrs, range) ->
            let mark = x.Mark(range)
            x.ProcessAttributeLists(attrs)
            x.ProcessOneLambdaParam([pat], lambdaBody, outerBodyExpr, false)
            x.Done(range, mark, ElementType.ATTRIB_PAT)
            x.ProcessOneLambdaParam(pats, lambdaBody, outerBodyExpr, processNext)

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
        x.PushRange(range, ElementType.INTERFACE_IMPLEMENTATION)
        x.ProcessTypeAsTypeReferenceName(interfaceType)
        x.PushStepList(bindings, objectExpressionMemberListProcessor)

    member x.ProcessSynIndexerArg(arg) =
        match arg with
        | SynIndexerArg.One(ExprRange range as expr, _, _) ->
            x.PushRange(range, ElementType.INDEXER_ARG_EXPR)
            x.PushExpression(getGeneratedAppArg expr)

        | SynIndexerArg.Two(expr1, _, expr2, _, range1, range2) ->
            x.PushRange(unionRanges range1 range2, ElementType.INDEXER_ARG_RANGE)
            x.PushExpression(getGeneratedAppArg expr2)
            x.PushExpression(getGeneratedAppArg expr1)

    member x.PushNamedIndexerArgExpression(expr) =
        let wrappedArgExpr = { Expression = expr; ElementType = ElementType.INDEXER_ARG_EXPR }
        x.PushStep(wrappedArgExpr, wrapExpressionProcessor)

    member x.ProcessRecordFieldBindingList(fields: (RecordFieldName * (SynExpr option) * BlockSeparator option) list) =
        let fieldsRange =
            match fields.Head, List.last fields with
            | ((lid, _), _, _), (_, Some(fieldValue), _) -> unionRanges lid.Range fieldValue.Range
            | ((lid, _), _, _), _ -> lid.Range
        
        x.PushRange(fieldsRange, ElementType.RECORD_FIELD_BINDING_LIST)
        x.PushStepList(fields, recordFieldBindingListProcessor)

    member x.ProcessAnonRecordFieldBindingList(fields: (Ident * SynExpr) list) =
        let fieldsRange =
            match fields.Head, List.last fields with
            | (id, _), (_, value) -> unionRanges id.idRange value.Range
        
        x.PushRange(fieldsRange, ElementType.RECORD_FIELD_BINDING_LIST)
        x.PushStepList(fields, anonRecordFieldBindingListProcessor)

    member x.ProcessAnonRecordFieldBinding(IdentRange idRange, (ExprRange range as expr)) =
        // Start node at id range, end at expr range.
        let mark = x.Mark(idRange)
        x.PushRangeForMark(range, mark, ElementType.RECORD_FIELD_BINDING)
        x.MarkAndDone(idRange, ElementType.EXPRESSION_REFERENCE_NAME)
        x.ProcessExpression(expr)

    member x.ProcessRecordFieldBinding(field: (RecordFieldName * (SynExpr option) * BlockSeparator option)) =
        let (lid, _), expr, blockSep = field
        let lid = lid.Lid
        match lid, expr with
        | IdentRange headRange :: _, Some(ExprRange exprRange as expr) ->
            let mark = x.Mark(headRange)
            x.PushRangeForMark(exprRange, mark, ElementType.RECORD_FIELD_BINDING)
            x.PushRecordBlockSep(blockSep)
            x.ProcessReferenceName(lid)
            x.ProcessExpression(expr)
        | _ -> ()

    member x.PushRecordBlockSep(blockSep) =
        match blockSep with
        | Some(_, Some(pos)) -> x.PushStep(pos, advanceToPosProcessor)
        | _ -> ()

    member x.ProcessListExpr(exprs, range, elementType) =
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
        x.PushExpression(expr)
        x.ProcessWhenExpr(whenExprOpt)
            
    member x.ProcessWhenExpr(whenExpr) =
        match whenExpr with
        | None -> ()
        | Some whenExpr ->
            
        let range = whenExpr.Range
        let mark = x.MarkTokenOrRange(FSharpTokenType.WHEN, range)
        x.PushRangeForMark(range, mark, ElementType.WHEN_EXPR_CLAUSE)
        
        x.ProcessExpression(whenExpr)

    member x.ProcessIndexerArg(arg: SynIndexerArg) =
        x.ProcessExpressionList(arg.Exprs)

[<AbstractClass>]
type StepProcessorBase<'TStep>() =
    abstract Process: step: 'TStep * builder: FSharpExpressionTreeBuilder -> unit

    interface IBuilderStepProcessor with
        member x.Process(step, builder) =
            x.Process(step :?> 'TStep, builder)

[<AbstractClass>]
type StepListProcessorBase<'TStep>() =
    abstract Process: 'TStep * FSharpExpressionTreeBuilder -> unit

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

type SequentialExpressionProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(expr, builder) =
        match expr with
        | SynExpr.Sequential(_, _, currentExpr, SynExpr.Sequential(_, _, nextExpr1, nextExpr2, _), _) ->
            builder.PushSequentialExpression(nextExpr2)
            builder.PushExpression(nextExpr1)
            builder.ProcessExpression(currentExpr)

        | SynExpr.Sequential(_, _, currentExpr, nextExpr, _) ->
            builder.PushExpression(nextExpr)
            builder.ProcessExpression(currentExpr)

        | _ -> builder.ProcessExpression(expr)


[<Struct>]
type ExpressionAndWrapperType =
    { Expression: SynExpr
      ElementType: CompositeNodeType }

type WrapExpressionProcessor() =
    inherit StepProcessorBase<ExpressionAndWrapperType>()

    override x.Process(arg, builder) =
        builder.PushRange(arg.Expression.Range, arg.ElementType)
        builder.ProcessExpression(arg.Expression)


type RangeMarkAndType =
    { Range: range
      Mark: int
      ElementType: NodeType }

type AdvanceToPosProcessor() =
    inherit StepProcessorBase<pos>()

    override x.Process(item, builder) =
        builder.AdvanceTo(item)

type EndRangeProcessor() =
    inherit StepProcessorBase<RangeMarkAndType>()

    override x.Process(item, builder) =
        builder.Done(item.Range, item.Mark, item.ElementType)


type SynTypeProcessor() =
    inherit StepProcessorBase<SynType>()

    override x.Process(synType, builder) =
        builder.ProcessType(synType)


type TypeArgsInReferenceExprProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(synExpr, builder) =
        builder.ProcessTypeArgsInReferenceExpr(synExpr)


type RecordBindingListRepresentationProcessor() =
    inherit StepProcessorBase<(RecordFieldName * (SynExpr option) * BlockSeparator option) list>()
    
    override x.Process(fields, builder) =
        builder.ProcessRecordFieldBindingList(fields)


type AnonRecordBindingListRepresentationProcessor() =
    inherit StepProcessorBase<(Ident * SynExpr) list>()
    
    override x.Process(fields, builder) =
        builder.ProcessAnonRecordFieldBindingList(fields)


type ExpressionListProcessor() =
    inherit StepListProcessorBase<SynExpr>()

    override x.Process(expr, builder) =
        builder.ProcessExpression(expr)


type BindingListProcessor() =
    inherit StepListProcessorBase<SynBinding>()

    override x.Process(binding, builder) =
        builder.ProcessLocalBinding(binding)


type AndLocalBindingListProcessor() =
    inherit StepListProcessorBase<DebugPointForBinding * bool * bool * SynPat * SynExpr * range>()

    override x.Process(binding, builder) =
        builder.ProcessAndLocalBinding(binding)


type RecordFieldBindingListProcessor() =
    inherit StepListProcessorBase<RecordFieldName * (SynExpr option) * BlockSeparator option>()

    override x.Process(field, builder) =
        builder.ProcessRecordFieldBinding(field)


type AnonRecordFieldBindingListProcessor() =
    inherit StepListProcessorBase<Ident * SynExpr>()

    override x.Process(field, builder) =
        builder.ProcessAnonRecordFieldBinding(field)


type MatchClauseListProcessor() =
    inherit StepListProcessorBase<SynMatchClause>()

    override x.Process(matchClause, builder) =
        builder.ProcessMatchClause(matchClause)

type ObjectExpressionMemberListProcessor() =
    inherit StepListProcessorBase<SynBinding>()

    override x.Process(binding, builder) =
        let (Binding(_, _, _, _, _, _, _, _, _, _, range, _)) = binding
        let mark = builder.Mark(range)
        let elementType = builder.ProcessMemberBinding(mark, binding, range)
        builder.Done(range, mark, elementType)

type InterfaceImplementationListProcessor() =
    inherit StepListProcessorBase<SynInterfaceImpl>()

    override x.Process(interfaceImpl, builder) =
        builder.ProcessInterfaceImplementation(interfaceImpl)


type IndexerArgListProcessor() =
    inherit StepListProcessorBase<SynIndexerArg>()

    override x.Process(indexerArg, builder) =
        builder.ProcessSynIndexerArg(indexerArg)


type IndexerArgsProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(synExpr, builder) =
        match synExpr with
        | SynExpr.DotIndexedGet(_, args, dotRange, range)
        | SynExpr.DotIndexedSet(_, args, _, range, dotRange, _) ->
            let argsListRange = mkFileIndexRange range.FileIndex dotRange.End range.End
            builder.PushRange(argsListRange, ElementType.INDEXER_ARG_LIST)
            builder.PushStepList(args, indexerArgListProcessor)

        | _ -> failwithf "Expecting dotIndexedGet/Set, got: %A" synExpr


[<AutoOpen>]
module BuilderStepProcessors =
    // We have to create these singletons this way instead of object expressions
    // due to compiler producing additional recursive init checks otherwise in this case.

    let expressionProcessor = ExpressionProcessor()
    let sequentialExpressionProcessor = SequentialExpressionProcessor()
    let wrapExpressionProcessor = WrapExpressionProcessor()
    let advanceToPosProcessor = AdvanceToPosProcessor()
    let endRangeProcessor = EndRangeProcessor()
    let synTypeProcessor = SynTypeProcessor()
    let typeArgsInReferenceExprProcessor = TypeArgsInReferenceExprProcessor()
    let indexerArgsProcessor = IndexerArgsProcessor()
    let recordBindingListRepresentationProcessor = RecordBindingListRepresentationProcessor() 
    let anonRecordBindingListRepresentationProcessor = AnonRecordBindingListRepresentationProcessor() 

    let expressionListProcessor = ExpressionListProcessor()
    let bindingListProcessor = BindingListProcessor()
    let andLocalBindingListProcessor = AndLocalBindingListProcessor()
    let recordFieldBindingListProcessor = RecordFieldBindingListProcessor()
    let anonRecordFieldBindingListProcessor = AnonRecordFieldBindingListProcessor()
    let matchClauseListProcessor = MatchClauseListProcessor()
    let objectExpressionMemberListProcessor = ObjectExpressionMemberListProcessor()
    let interfaceImplementationListProcessor = InterfaceImplementationListProcessor()
    let indexerArgListProcessor = IndexerArgListProcessor()
