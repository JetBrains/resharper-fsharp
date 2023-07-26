namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open System.Collections.Generic
open FSharp.Compiler.Syntax
open FSharp.Compiler.Syntax.PrettyNaming
open FSharp.Compiler.SyntaxTrivia
open FSharp.Compiler.Text
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree

type FSharpImplTreeBuilder(lexer, document, decls, lifetime, path, projectedOffset, lineShift) =
    inherit FSharpTreeBuilderBase(lexer, document, lifetime, path, projectedOffset, lineShift)

    /// FCS splits some declarations into separate fake ones:
    ///   * property declaration when both getter and setter bodies are present
    ///   * attributes for module-level do
    let mutable unfinishedDeclaration: (int * range * CompositeNodeType) option = None
    let mutable isFinishingDeclaration = false

    new (lexer, document, decls, lifetime, path) =
        FSharpImplTreeBuilder(lexer, document, decls, lifetime, path, 0, 0)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for decl in decls do
            x.ProcessTopLevelDeclaration(decl)
        x.FinishFile(mark, ElementType.F_SHARP_IMPL_FILE)

    member x.ProcessTopLevelDeclaration(moduleOrNamespace) =
        let (SynModuleOrNamespace(lid, _, moduleKind, decls, XmlDoc xmlDoc, attrs, _, range, _)) = moduleOrNamespace
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, moduleKind, xmlDoc, range)
        for decl in decls do
            x.ProcessModuleMemberDeclaration(decl)
        x.EnsureMembersAreFinished()
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberDeclaration(moduleMember) =
        match unfinishedDeclaration with
        | None -> ()
        | Some(mark, range, elementType) ->
            match moduleMember with
            | SynModuleDecl.Expr _ -> ()
            | _ ->
                unfinishedDeclaration <- None
                x.Done(range, mark, elementType)

        match moduleMember with
        | SynModuleDecl.NestedModule(SynComponentInfo(attrs, _, _, _, XmlDoc xmlDoc, _, _, _), _, decls, _, range, _) ->
            let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)
            for decl in decls do
                x.ProcessModuleMemberDeclaration(decl)
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(typeDefns, range) ->
            let mark = x.Mark(range)
            match typeDefns with
            | [] -> ()
            | primary :: secondary ->
                x.ProcessTypeDefn(primary, FSharpTokenType.TYPE)
                for typeDefn in secondary do
                    x.ProcessTypeDefn(typeDefn, FSharpTokenType.AND)
            x.Done(range, mark, ElementType.TYPE_DECLARATION_GROUP)

        | SynModuleDecl.Exception(SynExceptionDefn(exn, _, members, range), _) ->
            let mark = x.StartException(exn, range)
            x.ProcessTypeMembers(members)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleDecl.Open(openDeclTarget, range) ->
            x.ProcessOpenDeclTarget(openDeclTarget, range)

        | SynModuleDecl.Let(_, bindings, range) ->
            x.ProcessTopLevelBindings(bindings, range)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.Expr(expr, range) ->
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

            let elementType = if range = expr.Range then ElementType.EXPRESSION_STATEMENT else ElementType.DO_STATEMENT
            x.Done(range, mark, elementType)

        | SynModuleDecl.Attributes(attributeLists, range) ->
            let mark = x.Mark(range)
            x.ProcessAttributeLists(attributeLists)
            unfinishedDeclaration <- Some(mark, range, ElementType.EXPRESSION_STATEMENT)

        | SynModuleDecl.ModuleAbbrev(_, lid, range) ->
            let mark = x.Mark(range)
            x.ProcessNamedTypeReference(lid)
            x.Done(range, mark, ElementType.MODULE_ABBREVIATION_DECLARATION)

        | decl ->
            failwithf "unexpected decl: %A" decl

    member x.ProcessTypeDefn(SynTypeDefn(info, repr, members, implicitCtor, range, _) as typeDefn, typeKeywordType) =
        let (SynComponentInfo(attrs, typeParams, constraints, lid , XmlDoc xmlDoc, _, _, _)) = info

        match repr with
        | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.Augmentation _, _, _) ->
            x.ProcessTypeExtensionDeclaration(typeDefn, attrs)
        | _ ->

        let mark = x.StartType(attrs, xmlDoc, typeParams, constraints, lid, range, typeKeywordType)

        // Mark primary constructor before type representation.
        match implicitCtor with
        | Some ctor -> x.ProcessPrimaryConstructor(ctor)
        | _ -> ()

        match repr with
        | SynTypeDefnRepr.Simple(simpleRepr, _) ->
            x.ProcessSimpleTypeRepresentation(simpleRepr)

        | SynTypeDefnRepr.ObjectModel(kind, members, reprRange) ->
            let members =
                match members with
                | SynMemberDefn.ImplicitCtor _ :: rest -> rest
                | _ -> members

            match kind with
            | SynTypeDefnKind.Delegate(synType, signatureInfo) ->
                let mark = x.Mark(reprRange)
                x.ProcessSignatureType(signatureInfo, synType)
                x.Done(reprRange, mark, ElementType.DELEGATE_REPRESENTATION)

            | _ ->

            if x.AddObjectModelTypeReprNode(kind) then
                let mark = x.Mark(reprRange)
                x.ProcessTypeMembers(members)
                let elementType = x.GetObjectModelTypeReprElementType(kind)
                x.Done(reprRange, mark, elementType)
            else
                x.ProcessTypeMembers(members)

        | _ -> failwithf "Unexpected simple type representation: %A" repr

        x.ProcessTypeMembers(members)
        x.Done(range, mark, ElementType.F_SHARP_TYPE_DECLARATION)

    member x.ProcessTypeMembers(members: SynMemberDefn list) =
        for m in members do
            x.ProcessTypeMember(m)
        x.EnsureMembersAreFinished()

    member x.ProcessTypeExtensionDeclaration(SynTypeDefn(info, _, members, _, range, _), attrs) =
        let (SynComponentInfo(_, typeParams, constraints, lid , XmlDoc xmlDoc, _, _, _)) = info
        let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)

        x.ProcessTypeParametersAndConstraints(typeParams, constraints, lid)
        x.ProcessTypeMembers(members)
        x.Done(range, mark, ElementType.TYPE_EXTENSION_DECLARATION)

    member x.ProcessPrimaryConstructor(typeMember: SynMemberDefn) =
        match typeMember with
        | SynMemberDefn.ImplicitCtor(_, attrs, args, selfId, XmlDoc xmlDoc, range, _) ->

            // Skip spaces inside `T ()` range
            while (isNotNull x.TokenType && x.TokenType.IsWhitespace) && not x.Eof do
                x.AdvanceLexer()

            // TODO: add range for primary constructor in FCS
            let mark =
                if xmlDoc.HasDeclaration then
                    let mark = x.Mark(xmlDoc.Range)
                    x.MarkAndDone(xmlDoc.Range, DocCommentBlockNodeType.Instance)
                    mark
                else x.Mark()

            x.ProcessAttributeLists(attrs)
            x.ProcessImplicitCtorSimplePats(args)
            x.ProcessCtorSelfId(selfId)

            x.Done(range, mark, ElementType.PRIMARY_CONSTRUCTOR_DECLARATION)

        | _ -> failwithf "Expecting primary constructor, got: %A" typeMember

    member x.ProcessTypeMember(typeMember: SynMemberDefn) =
        match typeMember with
        | SynMemberDefn.ImplicitCtor _ -> ()
        | SynMemberDefn.LetBindings(bindings, _, _, range) ->
            x.ProcessTopLevelBindings(bindings, range)
        | _ ->

        let mark =
            match x.ContinueMemberDecl(typeMember.Range) with
            | ValueSome(mark) -> mark
            | _ -> x.MarkAndProcessIntro(typeMember.Attributes, typeMember.XmlDoc, null, typeMember.Range)

        let memberType =
            match typeMember with
            | SynMemberDefn.ImplicitInherit(baseType, args, _, _) ->
                x.ProcessTypeAsTypeReferenceName(baseType)
                x.MarkChameleonExpression(args)
                ElementType.TYPE_INHERIT

            | SynMemberDefn.Interface(interfaceType, _, interfaceMembersOpt, _) ->
                x.ProcessTypeAsTypeReferenceName(interfaceType)
                match interfaceMembersOpt with
                | Some(members) -> x.ProcessTypeMembers(members)
                | _ -> ()
                ElementType.INTERFACE_IMPLEMENTATION

            | SynMemberDefn.Inherit(baseType, _, _) ->
                try x.ProcessTypeAsTypeReferenceName(baseType)
                with _ -> () // Getting type range throws an exception if base type lid is empty.
                ElementType.INTERFACE_INHERIT

            | SynMemberDefn.GetSetMember(getBinding, setBinding, range, trivia) ->
                let withKeywordStart = Some trivia.WithKeyword.Start
                match getBinding, setBinding with
                | Some getBinding, Some setBinding ->
                    let binding1, binding2 =
                        if Position.posLt getBinding.RangeOfHeadPattern.Start setBinding.RangeOfHeadPattern.Start then
                            getBinding, setBinding
                        else
                            setBinding, getBinding

                    x.ProcessMemberBinding(mark, binding1, range, withKeywordStart) |> ignore

                    isFinishingDeclaration <- true
                    unfinishedDeclaration <- None
                    x.ProcessMemberBinding(mark, binding2, range, withKeywordStart)

                | Some binding, _
                | _, Some binding ->
                    x.ProcessMemberBinding(mark, binding, range, withKeywordStart)

                | _ -> failwithf "Unexpected getSet member: %A" typeMember

            | SynMemberDefn.Member(binding, range) ->
                x.ProcessMemberBinding(mark, binding, range, None)

            | SynMemberDefn.AbstractSlot(SynValSig(explicitTypeParams = typeParams; synType = synType; arity = arity; trivia = trivia), _, range, _) ->
                match typeParams with
                | SynValTyparDecls(Some(typeParams), _) ->
                    x.ProcessTypeParameters(typeParams, false)
                | _ -> ()
                x.ProcessReturnTypeInfo(arity, synType)
                x.ProcessAccessorsNamesClause(trivia, range)
                ElementType.ABSTRACT_MEMBER_DECLARATION

            | SynMemberDefn.ValField(SynField(fieldType = synType), _) ->
                x.ProcessType(synType)
                ElementType.VAL_FIELD_DECLARATION

            | SynMemberDefn.AutoProperty(_, _, _, synTypeOpt, _, _, _, _, _, expr, _, accessorClause) ->
                match synTypeOpt with
                | Some synType -> x.ProcessType(synType)
                | _ -> ()
                x.MarkChameleonExpression(expr)

                match accessorClause.WithKeyword, accessorClause.GetSetKeywords with
                | Some withKeyword, None ->
                    x.MarkAndDone(withKeyword, ElementType.ACCESSORS_NAMES_CLAUSE)
                | Some withKeyword, Some getSetKeywords ->
                    let range = Range.unionRanges withKeyword getSetKeywords.Range
                    x.MarkAndDone(range, ElementType.ACCESSORS_NAMES_CLAUSE)
                | _ -> ()

                ElementType.AUTO_PROPERTY_DECLARATION

            | _ -> failwithf "Unexpected type member: %A" typeMember

        x.FinishMemberDecl(typeMember.Range, mark, memberType)

    member x.ProcessObjExprMember(binding) =
        let (SynBinding(range = range)) = binding
        let mark =
            match x.ContinueMemberDecl(range) with
            | ValueSome(mark) -> mark
            | _ ->
                x.EnsureMembersAreFinished()
                x.Mark(range)

        let elementType = x.ProcessMemberBinding(mark, binding, range, None) // todo: member with attributes
        x.FinishMemberDecl(range, mark, elementType)

    member x.ContinueMemberDecl(range: range) =
        match unfinishedDeclaration with
        | Some(mark, unfinishedRange, _) when unfinishedRange.Start = range.Start ->
            isFinishingDeclaration <- true
            unfinishedDeclaration <- None
            ValueSome(mark)
        | _ -> ValueNone

    member x.FinishMemberDecl(range, mark, elementType) =
        isFinishingDeclaration <- false
        if unfinishedDeclaration.IsNone then
            x.Done(range, mark, elementType)

    member x.ProcessMemberBinding(mark, SynBinding(_, _, _, _, attrLists, _, valData, headPat, returnInfo, expr, _, _, _), range, accessorsStart: pos option) : CompositeNodeType =
        let elType =
            match headPat with
            | SynPat.LongIdent(SynLongIdent(id = lid), accessorId, typeParamsOpt, memberParams, _, range) ->
                match lid with
                | [_] ->
                    match valData with
                    | SynValData(Some(flags), _, selfId) when flags.MemberKind = SynMemberKind.Constructor ->
                        x.ProcessPatternParams(memberParams, true, true) // todo: should check isLocal
                        x.ProcessCtorSelfId(selfId)

                        x.MarkChameleonExpression(expr)
                        ElementType.SECONDARY_CONSTRUCTOR_DECLARATION

                    | _ ->
                        match accessorId with
                        | Some _ ->
                            x.ProcessAccessor(range, memberParams, expr, attrLists, accessorsStart)
                            ElementType.MEMBER_DECLARATION
                        | _ ->

                        x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr)
                        ElementType.MEMBER_DECLARATION

                | selfId :: _ :: _ ->
                    if not isFinishingDeclaration then
                        let selfIdNodeType =
                            if selfId.idText = "_" then ElementType.WILD_SELF_ID else ElementType.NAMED_SELF_ID
                        x.MarkAndDone(selfId.idRange, selfIdNodeType)

                    match accessorId with
                    | Some _ ->
                        x.ProcessAccessor(range, memberParams, expr, attrLists, accessorsStart)
                        ElementType.MEMBER_DECLARATION
                    | _ ->

                    x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr)
                    ElementType.MEMBER_DECLARATION

                | _ -> ElementType.MEMBER_DECLARATION

            | SynPat.Named _ ->
                // In some cases patterns for static members inside records are represented this way.
                x.ProcessMemberDeclaration(None, SynArgPats.Pats [], returnInfo, expr)
                ElementType.MEMBER_DECLARATION

            | _ -> ElementType.MEMBER_DECLARATION

        match valData with
        | SynValData(Some(flags), _, _) when
                flags.MemberKind = SynMemberKind.PropertyGet || flags.MemberKind = SynMemberKind.PropertySet ->
            if expr.Range.End <> range.End then
                unfinishedDeclaration <- Some(mark, range, ElementType.MEMBER_DECLARATION)

        | _ -> ()

        elType

    member x.ProcessAccessor(range: range, memberParams, expr, attrLists, accessorsStart) =
        let attrs =
            match accessorsStart with
            | Some pos -> attrLists |> List.skipWhile (fun attrList -> Position.posLt attrList.Range.Start pos)
            | _ -> []

        let range =
            match attrs with
            | attrList :: _ -> Range.unionRanges attrList.Range range
            | _ -> range

        let mark = x.Mark(range)
        x.ProcessAttributeLists(attrs)

        let memberParams =
            match memberParams with
            | SynArgPats.Pats([SynPat.Tuple(_, patterns, _, _)]) -> SynArgPats.Pats(patterns)
            | _ -> memberParams

        x.ProcessPatternParams(memberParams, true, true)
        x.MarkChameleonExpression(expr)
        x.Done(mark, ElementType.ACCESSOR_DECLARATION)

    /// The last member may be a property the could be extended
    /// with another accessor represented as a separate member declaration.
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
        | Some(SynBindingReturnInfo(returnType, range, attrs, _)) ->

        x.AdvanceToTokenOrPos(FSharpTokenType.COLON, range.Start)

        let mark = x.Mark()
        x.ProcessAttributeLists(attrs)
        x.ProcessType(returnType)
        x.Done(range, mark, ElementType.RETURN_TYPE_INFO)

    member x.ProcessMemberDeclaration(typeParamsOpt, memberParams, returnInfo, expr) =
        match typeParamsOpt with
        | Some(SynValTyparDecls(Some(typeParams), _)) ->
            x.ProcessTypeParameters(typeParams, false)
        | _ -> ()

        x.ProcessMemberParams(memberParams, true, true) // todo: should check isLocal
        x.ProcessReturnInfo(returnInfo)
        x.MarkChameleonExpression(expr)

    // isBindingHeadPattern is needed to distinguish function definitions from other long ident pats:
    //   let (Some x) = ...
    //   let Some x = ...
    member x.ProcessPat(PatRange range as pat, isLocal, isBindingHeadPattern) =
        let patMark = x.Mark(range)

        match isBindingHeadPattern, pat with
        | true, SynPat.LongIdent(lid, _, typars, args, _, _) ->
            match lid.IdentsWithTrivia with
            | [ SynIdentRange idRange as SynIdent(id, _) ] ->
                let mark = x.Mark(idRange)
                if IsActivePatternName id.idText then
                    x.ProcessActivePatternDecl(idRange, isLocal)
                x.Done(idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)
            | lid ->
                x.ProcessReferenceName(lid)

            let elementType = if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT
            x.Done(patMark, elementType)

            match typars with
            | Some(SynValTyparDecls(Some(typarDecls), _)) ->
                let mark = x.Mark(typarDecls.Range)
                for typarDecl in typarDecls.TyparDecls do
                    x.ProcessTypeParameter(typarDecl, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)
                x.ProcessConstraintsClause(typarDecls.Constraints)
                x.Done(typarDecls.Range, mark, ElementType.POSTFIX_TYPE_PARAMETER_DECLARATION_LIST)
            | _ -> ()

            x.ProcessMemberParams(args, true, true)

        | true, SynPat.Named(SynIdent(id, trivia), _, _, _) when IsActivePatternName id.idText ->
            let idRange = getActivePatternIdRange trivia id.idRange
            let mark = x.Mark(idRange)
            x.ProcessActivePatternDecl(idRange, isLocal)
            x.Done(idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)

            let elementType = if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT
            x.Done(patMark, elementType)

        | _ ->

        let elementType =
            match pat with
            | SynPat.Named(SynIdentRange idRange as SynIdent(id, _), _, _, _) ->
                let mark = x.Mark(idRange)
                if IsActivePatternName id.idText then
                    x.ProcessActivePatternExpr(idRange) // todo
                x.Done(id.idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)
                if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT

            | SynPat.As(lhsPat, rhsPat, _) ->
                x.ProcessPat(lhsPat, isLocal, false)
                x.ProcessPat(rhsPat, isLocal, false)
                ElementType.AS_PAT

            | SynPat.LongIdent(lid, _, _, args, _, _) ->
                match lid.IdentsWithTrivia with
                | [ SynIdentRange idRange as SynIdent(id, _) ] ->
                    let mark = x.Mark(idRange)
                    if IsActivePatternName id.idText then
                        x.ProcessActivePatternExpr(idRange)
                    x.Done(idRange, mark, ElementType.EXPRESSION_REFERENCE_NAME)
                | lid ->
                    x.ProcessReferenceName(lid)

                if args.IsEmpty then
                    if isLocal then ElementType.LOCAL_REFERENCE_PAT else ElementType.TOP_REFERENCE_PAT
                else
                    x.ProcessPatternParams(args, isLocal, false)
                    ElementType.PARAMETERS_OWNER_PAT

            | SynPat.Typed(pat, synType, _) ->
                x.ProcessPat(pat, isLocal, false)
                x.ProcessType(synType)
                ElementType.TYPED_PAT

            | SynPat.Or(pat1, pat2, _, _) ->
                x.ProcessPat(pat1, isLocal, false)
                x.ProcessPat(pat2, isLocal, false)
                ElementType.OR_PAT

            | SynPat.Ands(pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat, isLocal, false)
                ElementType.ANDS_PAT

            | SynPat.Tuple(_, pats, _, _) ->
                x.ProcessListLikePat(pats, isLocal)
                ElementType.TUPLE_PAT

            | SynPat.ArrayOrList(isArray, pats, _) ->
                x.ProcessListLikePat(pats, isLocal)
                if isArray then ElementType.ARRAY_PAT else ElementType.LIST_PAT

            | SynPat.Const(SynConst.Unit, _)
            | SynPat.Paren(SynPat.Const(SynConst.Unit, _), _) ->
                ElementType.UNIT_PAT

            | SynPat.Paren(pat, _) ->
                x.ProcessPat(pat, isLocal, false)
                ElementType.PAREN_PAT

            | SynPat.Record(pats, _) ->
                for (lid, IdentRange range), _, pat in pats do
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

            | SynPat.QuoteExpr(expr, _) ->
                x.MarkChameleonExpression(expr)
                ElementType.QUOTE_EXPR_PAT

            | SynPat.Null _ -> ElementType.NULL_PAT
            | SynPat.DeprecatedCharRange _ -> ElementType.CHAR_RANGE_PAT

            // todo: mark inner pattern, assert ranges
            | SynPat.FromParseError _ -> ElementType.FROM_ERROR_PAT

            | SynPat.ListCons(lhsPat, rhsPat, _, _) ->
                x.ProcessPat(lhsPat, isLocal, false)
                x.ProcessPat(rhsPat, isLocal, false)
                ElementType.LIST_CONS_PAT

            | SynPat.InstanceMember _ -> failwith $"Unexpected pattern: {pat}"

        x.Done(range, patMark, elementType)

    member x.ProcessListLikePat(pats, isLocal) =
        for pat in pats do
            x.ProcessPat(pat, isLocal, false)

    member x.ProcessPatternParams(args: SynArgPats, isLocal, markMember) =
        match args with
        | SynArgPats.Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | SynArgPats.NamePatPairs(idsAndPats, argsRange, _) ->
            let argsMark = x.MarkTokenOrRange(FSharpTokenType.LPAREN, argsRange)

            for IdentRange range, _, pat in idsAndPats do
                let mark = x.Mark(range)
                x.MarkAndDone(range, ElementType.EXPRESSION_REFERENCE_NAME)
                x.ProcessParam(pat, isLocal, markMember)
                x.Done(range, mark, ElementType.FIELD_PAT)

            x.Done(argsRange, argsMark, ElementType.NAMED_UNION_CASE_FIELDS_PAT)

    member x.ProcessMemberParams(args: SynArgPats, isLocal, markMember) =
        match args with
        | SynArgPats.Pats pats ->
            for pat in pats do
                x.ProcessParam(pat, isLocal, markMember)

        | _ -> failwithf "args: %A" args

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
            if Position.posGt r.End outerRange.Start then attrs else
            x.SkipOuterAttrs(rest, outerRange)

    member x.ProcessTopLevelBinding(binding, isSecondary) =
        let (SynBinding(_, kind, _, _, attrs, XmlDoc xmlDoc, _ , headPat, returnInfo, expr, range, _, _)) = binding

        if isSecondary then x.AdvanceToTokenOrRangeStart(FSharpTokenType.AND, range)

        let expr = x.FixExpression(expr)
        let mark =
            if not xmlDoc.HasDeclaration then x.Mark()
            else x.MarkXmlDocOwner(xmlDoc, null, range)

        match kind with
        | SynBindingKind.StandaloneExpression
        | SynBindingKind.Do -> x.MarkChameleonExpression(expr)
        | _ ->

        x.ProcessAttributeLists(attrs)
        x.ProcessPat(headPat, false, true)
        x.ProcessReturnInfo(returnInfo)
        x.MarkChameleonExpression(expr)

        x.Done(binding.RangeOfBindingWithRhs, mark, ElementType.TOP_BINDING)

    member x.ProcessTopLevelBindings(bindings, range) =
        match bindings with
        | [] -> ()
        | [SynBinding(kind = SynBindingKind.Do; expr = expr)] ->
            let mark = x.Mark(range)
            x.AdvanceToTokenOrRangeStart(FSharpTokenType.DO, range)
            let expr = x.RemoveDoExpr(expr)
            x.MarkChameleonExpression(expr)
            x.Done(range, mark, ElementType.DO_STATEMENT)

        // `extern` declarations are represented as normal `let` bindings with fake rhs expressions in FCS AST.
        // This is a workaround to mark such declarations and not to mark the non-existent expressions inside it.
        | [SynBinding(attributes = attrs
                      headPat = headPat
                      returnInfo = returnInfo
                      trivia = { LeadingKeyword = SynLeadingKeyword.Extern _ }
                      xmlDoc = XmlDoc xmlDoc)] ->

            let mark = x.MarkXmlDocOwner(xmlDoc, null, range)
            x.ProcessAttributeLists(attrs)
            x.AdvanceToTokenOrRangeStart(FSharpTokenType.EXTERN, headPat.Range)
            Assertion.Assert(x.TokenType == FSharpTokenType.EXTERN, "Expecting EXTERN, got: {0}", x.TokenType)

            match returnInfo with
            | Some(SynBindingReturnInfo(attributes = attrs)) ->
                x.ProcessAttributeLists(attrs)
            | _ -> ()
            // todo: mark parameters
            x.Done(range, mark, ElementType.EXTERN_DECLARATION)

        | binding :: rest ->
        let mark = x.Mark(range)

        x.ProcessTopLevelBinding(binding, false)
        for binding in rest do
            x.ProcessTopLevelBinding(binding, true)

        x.Done(range, mark, ElementType.LET_BINDINGS_DECLARATION)

    member x.ProcessActivePatternExpr(range: range) =
        x.ProcessActivePatternId(range, ElementType.ACTIVE_PATTERN_NAMED_CASE_REFERENCE_NAME)

[<Struct>]
type BuilderStep =
    { Item: obj
      Processor: IBuilderStepProcessor }


and IBuilderStepProcessor =
    abstract Process: step: obj * builder: FSharpExpressionTreeBuilder -> unit


type FSharpExpressionTreeBuilder(lexer, document, lifetime, path, projectedOffset, lineShift) =
    inherit FSharpImplTreeBuilder(lexer, document, [], lifetime, path, projectedOffset, lineShift)

    let nextSteps = Stack<BuilderStep>()

    member x.ProcessLocalBinding(binding, isSecondary) =
        let (SynBinding(_, kind, _, _, attrs, XmlDoc xmlDoc, _, headPat, returnInfo, expr, range, _, _)) = binding

        if isSecondary then x.AdvanceToTokenOrRangeStart(FSharpTokenType.AND, range)

        let expr = x.FixExpression(expr)
        let mark =
            if not xmlDoc.HasDeclaration then x.Mark()
            else x.MarkXmlDocOwner(xmlDoc, null, range)

        match kind with
        | SynBindingKind.StandaloneExpression
        | SynBindingKind.Do -> x.ProcessExpression(expr)
        | _ ->

        x.ProcessAttributeLists(attrs)

        x.PushRangeForMark(binding.RangeOfBindingWithRhs, mark, ElementType.LOCAL_BINDING)
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
        | SynExpr.TraitCall(_, traitSig, expr, _)
        | SynExpr.Paren(expr = SynExpr.TraitCall(_, traitSig, expr, _)) ->
            // todo: fix trait range
            x.PushRange(range, ElementType.TRAIT_CALL_EXPR)

            match traitSig with
            | SynMemberSig.Member(SynValSig(synType = synType), _, _, _) -> x.ProcessType(synType)
            | _ -> ()

            x.ProcessExpression(expr)

        | SynExpr.Paren(expr = SynExpr.Ident(ident)) when IsActivePatternName ident.idText ->
            x.PushRange(range, ElementType.REFERENCE_EXPR)
            x.ProcessActivePatternExpr(ident.idRange) // todo: check this

        | SynExpr.Paren(expr, leftParenRange, _, _) ->
            let isParen = leftParenRange.EndColumn - leftParenRange.StartColumn = 1
            let elementType = if isParen then ElementType.PAREN_EXPR else ElementType.BEGIN_END_EXPR
            x.PushRangeAndProcessExpression(expr, range, elementType)

        | SynExpr.Quote(_, _, expr, _, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.QUOTE_EXPR)

        | SynExpr.Const(synConst, _) ->
            
            let getSynMeasureRange (synMeasure: SynMeasure) =
                match synMeasure with
                | SynMeasure.Named(range = range)
                | SynMeasure.Anon range
                | SynMeasure.Product(range = range)
                | SynMeasure.Seq(range = range)
                | SynMeasure.Divide(range = range)
                | SynMeasure.Power(range = range)
                | SynMeasure.Var(range = range)
                | SynMeasure.Paren(range = range) -> range
                | SynMeasure.One -> failwith "should not be reached"
            
            let processRatio (ratio: SynRationalConst) (overallRange: range) =
                
                let rec processRatConstCase (ratio: SynRationalConst) =
                    match ratio with
                    | SynRationalConst.Integer _ ->
                        x.MarkAndDone(overallRange, ElementType.INTEGER_RAT)
                    | SynRationalConst.Negate ratConst ->
                        let m = x.Mark()
                        processRatConstCase ratConst
                        x.Done(overallRange, m, ElementType.NEGATE_RAT)
                    | SynRationalConst.Rational(range = range) ->
                        x.AdvanceToTokenOrRangeEnd(FSharpTokenType.LPAREN, range)
                        x.MarkAndDone(range, ElementType.RATIONAL_RAT)

                x.AdvanceToTokenOrRangeEnd(FSharpTokenType.SYMBOLIC_OP, overallRange) // advance to ^ or ^-
                x.AdvanceLexer() // advanve beyond ^ or ^-
                let ratConstMark = x.Mark()
                processRatConstCase ratio
                x.Done(overallRange, ratConstMark, ElementType.RATIONAL_CONST)

            let rec processMeasure (synMeasure: SynMeasure) =
                match synMeasure with
                | SynMeasure.Named(longId, range) ->
                    let namedMark = x.Mark(range)
                    let namedTypeMark = x.Mark(range)
                    x.ProcessNamedTypeReference(longId)
                    x.Done(range, namedTypeMark, ElementType.NAMED_TYPE_USAGE)
                    x.Done(range, namedMark, ElementType.NAMED_MEASURE)
                | SynMeasure.Product(measure1, measure2, range) ->
                    let prodMark = x.Mark(range)
                    processMeasure measure1
                    processMeasure measure2
                    x.Done(range, prodMark, ElementType.PRODUCT_MEASURE)
                | SynMeasure.One ->
                    () // handled in SynMeasure.Seq to have the range
                | SynMeasure.Seq([SynMeasure.One], range) ->
                    x.MarkAndDone(range, ElementType.ONE_MEASURE)
                | SynMeasure.Seq(measures = [synMeasure]) ->
                    processMeasure synMeasure
                | SynMeasure.Seq(synMeasures, range) ->
                    let seqMark = x.Mark(range)
                    synMeasures |> List.iter processMeasure
                    x.Done(range, seqMark, ElementType.SEQ_MEASURE)
                | SynMeasure.Divide(measure1, measure2, range) ->
                    let divMark = x.Mark(range)
                    processMeasure measure1
                    processMeasure measure2
                    x.Done(range, divMark, ElementType.DIVIDE_MEASURE)
                | SynMeasure.Power(measure = synMeasure; power = ratio; range = range) ->
                    let powerMark = x.Mark(range)
                    processMeasure synMeasure
                    
                    let measureRange = getSynMeasureRange synMeasure
                    let ratioRange = Range.mkRange range.FileName measureRange.End range.End
                    processRatio ratio ratioRange
                    
                    x.Done(range, powerMark, ElementType.POWER_MEASURE)
                | SynMeasure.Anon range ->
                    // horrible workaround for a bug in FCS:
                    // currently the range of SynMeasure.Anon spans over all of the "<_>"
                    // construct a new range not including the GREATER
                    let endLine, endColumn =
                        if range.EndColumn > 0 then
                            range.EndLine, range.EndColumn - 1
                        else
                            range.EndLine - 1, range.EndColumn
                    let endBeforeGreater = Position.mkPos endLine endColumn
                    let r = Range.mkRange range.FileName range.Start endBeforeGreater
                    x.AdvanceToTokenOrRangeEnd(FSharpTokenType.IDENTIFIER, range)
                    x.MarkAndDone(r, ElementType.ANON_MEASURE)
                | SynMeasure.Paren(synMeasure, range) ->
                    let parenMark = x.Mark(range)
                    processMeasure synMeasure
                    x.Done(range, parenMark, ElementType.PAREN_MEASURE)
                | SynMeasure.Var(synTypar, range) ->
                    let mark = x.Mark(range)
                    x.ProcessTypeParameter(synTypar)
                    x.Done(range, mark, ElementType.VAR_MEASURE)

            let mark = x.Mark(range)

            match synConst with
            | SynConst.Measure(_synConst, _constantRange, synMeasure) ->
                x.AdvanceToTokenOrRangeEnd(FSharpTokenType.LESS, range)
                let typeMeasureMark = x.Mark(range)
                processMeasure synMeasure
                x.AdvanceTo(range.End)
                x.Done(typeMeasureMark, ElementType.UNIT_OF_MEASURE_CLAUSE)
            | _ -> ()

            x.Done(range, mark, x.GetConstElementType(synConst))

        | SynExpr.Typed(expr, synType, _) ->
            let typeRange = synType.Range
            Assertion.Assert(Range.rangeContainsRange range typeRange,
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

        | SynExpr.ArrayOrList(isArray, exprs, _) ->
            // SynExpr.ArrayOrList is currently only used for error recovery and empty lists in the parser.
            // Non-empty SynExpr.ArrayOrList is created in the type checker only.
            Assertion.Assert(List.isEmpty exprs, "Non-empty SynExpr.ArrayOrList: {0}", expr)
            x.MarkAndDone(range, if isArray then ElementType.ARRAY_EXPR else ElementType.LIST_EXPR)

        | SynExpr.AnonRecd(_, copyInfo, fields, _, _) ->
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

        | SynExpr.ObjExpr(synType, args, _, _, memberDefns, interfaceImpls, _, _) ->
            x.PushRange(range, ElementType.OBJ_EXPR)
            x.ProcessTypeAsTypeReferenceName(synType)
            x.PushStepList(interfaceImpls, interfaceImplementationListProcessor)
            x.PushStep((), finishObjectExpressionMemberListProcessor)
            x.PushStepList(memberDefns, objectExpressionMemberListProcessor)

            match args with
            | Some(expr, _) -> x.ProcessExpression(expr)
            | _ -> ()

        | SynExpr.While(_, whileExpr, doExpr, _) ->
            x.PushRange(range, ElementType.WHILE_EXPR)
            x.PushExpression(doExpr)
            x.ProcessExpression(whileExpr)

        | SynExpr.For(_, _, id, _, idBody, _, toBody, doBody, _) ->
            x.PushRange(range, ElementType.FOR_EXPR)
            x.PushExpression(doBody)
            x.PushExpression(toBody)
            x.ProcessLocalId(id)
            x.ProcessExpression(idBody)

        | SynExpr.ForEach(_, _, _, _, pat, enumExpr, bodyExpr, _) ->
            x.PushRange(range, ElementType.FOR_EACH_EXPR)
            x.ProcessPat(pat, true, false)
            x.PushExpression(bodyExpr)
            x.ProcessExpression(enumExpr)

        | SynExpr.ArrayOrListComputed(isArray, expr, _) ->
            let expr = match expr with | SynExpr.ComputationExpr(expr = expr) -> expr | _ -> expr
            x.PushRangeAndProcessExpression(expr, range, if isArray then ElementType.ARRAY_EXPR else ElementType.LIST_EXPR)

        | SynExpr.ComputationExpr(_, expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.COMPUTATION_EXPR)

        | SynExpr.Lambda(_, inLambdaSeq, _, bodyExpr, parsedData, _, _) ->
            Assertion.Assert(not inLambdaSeq, "Expecting non-generated lambda expression, got:\n{0}", expr)
            x.PushRange(range, ElementType.LAMBDA_EXPR)
            x.PushExpression(getLambdaBodyExpr bodyExpr)

            match parsedData with
            | Some(head :: _ as pats, _) ->
                let patsRange = Range.unionRanges head.Range (List.last pats).Range
                x.PushRange(patsRange, ElementType.LAMBDA_PARAMETERS_LIST)
                for pat in pats do
                    x.ProcessPat(pat, true, false)
            | _ -> ()

        | SynExpr.MatchLambda(_, _, clauses, _, _) ->
            x.PushRange(range, ElementType.MATCH_LAMBDA_EXPR)
            x.ProcessMatchClauses(clauses)

        | SynExpr.Match(_, expr, clauses, _, _) ->
            x.MarkMatchExpr(range, expr, clauses)

        | SynExpr.Do(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.DO_EXPR)

        | SynExpr.Assert(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.ASSERT_EXPR)

        | SynExpr.App(_, false, SynExpr.App(_, true, funcExpr, leftArg, _), rightArg, prefixAppRange) ->
            match funcExpr with
            | SynExpr.Ident(IdentText "op_Range") -> x.ProcessRangeExpr(leftArg, rightArg, prefixAppRange)
            | SynExpr.Ident(IdentText "op_RangeStep") -> x.ProcessRangeStepExpr(leftArg, rightArg)
            | _ ->

            x.PushRange(range, ElementType.BINARY_APP_EXPR)
            x.PushExpression(rightArg)
            x.PushExpression(funcExpr)
            x.ProcessExpression(leftArg)

        | SynExpr.App(_, true, (SynExpr.LongIdent(_, SynLongIdent([IdentText "op_ColonColon"], _, _), _, _) as funcExpr), SynExpr.Tuple(exprs = [first; second]), _) ->
            x.PushRange(range, ElementType.BINARY_APP_EXPR)
            x.PushExpression(second)
            x.PushExpression(funcExpr)
            x.ProcessExpression(first)

        | SynExpr.App(_, isInfix, funcExpr, argExpr, _) ->
            Assertion.Assert(not isInfix, "Expecting prefix app, got: {0}", expr)

            x.PushRange(range, ElementType.PREFIX_APP_EXPR)
            x.PushExpression(argExpr)
            x.ProcessExpression(funcExpr)

        | SynExpr.TypeApp(expr = expr) as typeApp ->
            // Process expression first, then inject type args into it in the processor.
            x.PushStep(typeApp, typeArgsInReferenceExprProcessor)
            x.ProcessExpression(expr)

        | SynExpr.LetOrUse(_, _, bindings, bodyExpr, _, _) ->
            x.PushRange(range, ElementType.LET_OR_USE_EXPR)
            x.PushExpression(bodyExpr)
            x.ProcessBindings(bindings)

        | SynExpr.TryWith(tryExpr, withCases, _, _, _, _) ->
            x.PushRange(range, ElementType.TRY_WITH_EXPR)
            x.PushStepList(withCases, matchClauseListProcessor)
            x.ProcessExpression(tryExpr)

        | SynExpr.TryFinally(tryExpr, finallyExpr, _, _, _, _) ->
            x.PushRange(range, ElementType.TRY_FINALLY_EXPR)
            x.PushExpression(finallyExpr)
            x.ProcessExpression(tryExpr)

        | SynExpr.Lazy(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.LAZY_EXPR)

        | SynExpr.IfThenElse(ifExpr, thenExpr, elseExprOpt, _, _, _, { IsElif = isElif }) ->
            // Nested ifExpr may have wrong range, e.g. `else` goes inside the nested expr range here:
            // `if true then "a" else if true then "b" else "c"`
            // However, elif expressions actually start this way.
            x.AdvanceToStart(range)
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
            x.ProcessLongIdentifierExpression(lid, range)

        | SynExpr.LongIdentSet(lid, expr, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.ProcessLongIdentifierExpression(lid, lid.Range)
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
            x.PushRange(Range.unionRanges lid.Range expr1.Range, ElementType.NAMED_INDEXER_EXPR)
            x.ProcessLongIdentifierExpression(lid, lid.Range)
            x.ProcessExpression(expr1)

        | SynExpr.DotNamedIndexedPropertySet(expr1, lid, expr2, expr3, _) ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr3)
            x.PushRange(Range.unionRanges expr1.Range expr2.Range, ElementType.NAMED_INDEXER_EXPR)
            x.PushExpression(expr2)
            x.ProcessLongIdentifierAndQualifierExpression(expr1, lid)

        | SynExpr.DotIndexedGet(expr, _, _, _) as get ->
            x.PushRange(range, ElementType.ITEM_INDEXER_EXPR)
            x.PushStep(get, indexerArgsProcessor)
            x.ProcessExpression(expr)

        | SynExpr.DotIndexedSet(expr1, _, expr2, leftRange, _, _) as set ->
            x.PushRange(range, ElementType.SET_EXPR)
            x.PushExpression(expr2)
            x.PushRange(leftRange, ElementType.ITEM_INDEXER_EXPR)
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

        | SynExpr.LetOrUseBang(_, _, _, pat, expr, ands, inExpr, range, _) ->
            x.PushRange(range, ElementType.LET_OR_USE_EXPR)
            x.PushExpression(inExpr)
            x.PushStepList(ands, andLocalBindingListProcessor)
            let exprWithPatRange = Range.unionRanges expr.Range pat.Range
            x.PushRangeForMark(exprWithPatRange, x.Mark(), ElementType.LOCAL_BINDING)
            x.ProcessPat(pat, true, false)
            x.ProcessExpression(expr)

        | SynExpr.MatchBang(_, expr, clauses, _, _) ->
            x.MarkMatchExpr(range, expr, clauses)

        | SynExpr.DoBang(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.DO_EXPR)

        | SynExpr.LibraryOnlyILAssembly _
        | SynExpr.LibraryOnlyStaticOptimization _
        | SynExpr.LibraryOnlyUnionCaseFieldGet _
        | SynExpr.LibraryOnlyUnionCaseFieldSet _
        | SynExpr.LibraryOnlyILAssembly _ ->
            x.MarkAndDone(range, ElementType.LIBRARY_ONLY_EXPR)

        | SynExpr.ArbitraryAfterError _ ->
            x.MarkAndDone(range, ElementType.FROM_ERROR_EXPR)

        // todo: convert to refExpr
        | SynExpr.DiscardAfterMissingQualificationAfterDot(expr, _, _)
        | SynExpr.FromParseError(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.FROM_ERROR_EXPR)

        | SynExpr.Fixed(expr, _) ->
            x.PushRangeAndProcessExpression(expr, range, ElementType.FIXED_EXPR)

        | SynExpr.Sequential(_, _, expr1, expr2, _) ->
            x.PushRange(range, ElementType.SEQUENTIAL_EXPR)
            x.PushSequentialExpression(expr2)
            x.ProcessExpression(expr1)

        | SynExpr.InterpolatedString(stringParts, _, _) ->
            x.PushRange(range, ElementType.INTERPOLATED_STRING_EXPR)
            x.PushStepList(stringParts, interpolatedStringProcessor)

        | SynExpr.IndexFromEnd (expr, _) ->
            x.PushRange(range, ElementType.END_SLICE_EXPR)
            x.ProcessExpression(expr)

        | SynExpr.IndexRange(expr1, _, expr2, _, _, _) ->
            match expr1, expr2 with
            | Some(SynExpr.IndexRange(Some(expr11), _, Some(expr12), _, _, _)), Some(expr2) ->
                x.PushRange(range, ElementType.RANGE_EXPR)
                x.PushExpression(expr2)
                x.PushExpression(expr12)
                x.ProcessExpression(expr11)

            | Some(expr1), Some(expr2) ->
                x.PushRange(range, ElementType.RANGE_EXPR)
                x.PushExpression(expr2)
                x.ProcessExpression(expr1)

            | Some(expr), _ ->
                x.PushRange(range, ElementType.END_SLICE_EXPR)
                x.ProcessExpression(expr)
            
            | _, Some(expr) ->
                x.PushRange(range, ElementType.BEGINNING_SLICE_EXPR)
                x.ProcessExpression(expr)

            | _ ->
                x.MarkAndDone(range, ElementType.WHOLE_RANGE_EXPR)

        | SynExpr.Dynamic(funExpr, _, argExpr, _) ->
            x.PushRange(range, ElementType.DYNAMIC_EXPR)
            x.PushExpression(argExpr)
            x.ProcessExpression(funExpr)

        | SynExpr.Typar _ ->
            x.MarkAndDone(range, ElementType.REFERENCE_EXPR, ElementType.TYPE_PARAMETER_ID)

        | SynExpr.DebugPoint _ -> failwithf $"Synthetic expression: {expr}"

    member x.ProcessAndBangLocalBinding(pat: SynPat, expr: SynExpr, range) =
        x.AdvanceToTokenOrRangeStart(FSharpTokenType.AND_BANG, range)
        let exprWithPatRange = Range.unionRanges expr.Range pat.Range
        x.PushRangeForMark(exprWithPatRange, x.Mark(), ElementType.LOCAL_BINDING)
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

        let rangeSeqRange = Range.unionRanges fromRange toRange

        // Range sequence expr also contains braces in the fake app expr, mark it as a separate expr node.
        if appRange <> rangeSeqRange then
            x.PushRange(appRange, ElementType.COMPUTATION_EXPR)

        let seqMark = x.Mark(fromRange)
        x.PushRangeForMark(toRange, seqMark, ElementType.RANGE_EXPR)
        x.PushExpression(toExpr)

        match stepExpr with
        | ValueSome stepExpr -> x.PushExpression(stepExpr)
        | _ -> ()

        x.ProcessExpression(fromExpr)

    member x.ProcessLongIdentifierExpression(lidWithDots: SynLongIdent, range) =
        // There may be dot at the end, consider unfinished expr like `System.`
        // todo: optimize checking extra dot in FCS, wrap in error node

        let (LidWithTrivia lid) = lidWithDots
        let marks = Stack()

        x.AdvanceToStart(range)
        for _ in lid do
            marks.Push(x.Mark())

        for id, trivia in lid do
            let isLastId = marks.Count = 1
            let range =
                if not isLastId then id.idRange else

                match trivia with
                | Some(IdentTrivia.HasParenthesis(lparen, rparen)) -> Range.unionRanges lparen rparen
                | _ -> range

            let elementType =
                if isLastId && IsActivePatternName id.idText then
                    x.ProcessActivePatternExpr(range)
                ElementType.REFERENCE_EXPR

            x.Done(range, marks.Pop(), elementType)

    member x.ProcessLongIdentifierAndQualifierExpression(ExprRange exprRange as expr, lid) =
        x.AdvanceToStart(exprRange)

        let mutable isFirstId = true
        for IdentRange idRange in List.rev lid.LongIdent do
            let range = if not isFirstId then idRange else lid.Range
            x.PushRangeForMark(range, x.Mark(), ElementType.REFERENCE_EXPR)
            isFirstId <- false

        x.ProcessExpression(expr)

    member x.MarkMatchExpr(range: range, expr: SynExpr, clauses) =
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
            x.ProcessLocalBinding(binding, false)

        | binding :: bindings ->
            x.PushStepList(bindings, secondaryBindingListProcessor)
            x.ProcessLocalBinding(binding, false)

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

    member x.ProcessInterfaceImplementation(SynInterfaceImpl(interfaceType, _, _, memberDefns, range)) =
        x.PushRange(range, ElementType.INTERFACE_IMPLEMENTATION)
        x.PushStep((), finishObjectExpressionMemberListProcessor)
        x.ProcessTypeAsTypeReferenceName(interfaceType)
        x.PushStepList(memberDefns, objectExpressionMemberListProcessor)

    member x.ProcessRecordFieldBindingList(fields: SynExprRecordField list) =
        let fieldsRange =
            match fields.Head, List.last fields with
            | SynExprRecordField((lid, _), _, _, _), SynExprRecordField(_, _, Some(fieldValue), _) ->
                Range.unionRanges lid.Range fieldValue.Range
            | SynExprRecordField((lid, _), _, _, _), _ -> lid.Range

        x.PushRange(fieldsRange, ElementType.RECORD_FIELD_BINDING_LIST)
        x.PushStepList(fields, recordFieldBindingListProcessor)

    member x.ProcessAnonRecordFieldBindingList(fields: (SynLongIdent * range option * SynExpr) list) =
        let fieldsRange =
            match fields.Head, List.last fields with
            | (lid, _, _), (_, _, value) -> Range.unionRanges lid.Range value.Range

        x.PushRange(fieldsRange, ElementType.RECORD_FIELD_BINDING_LIST)
        x.PushStepList(fields, anonRecordFieldBindingListProcessor)

    member x.ProcessAnonRecordFieldBinding(lid: SynLongIdent, _, (ExprRange range as expr)) =
        // Start node at id range, end at expr range.
        let mark = x.Mark(lid.Range)
        x.PushRangeForMark(range, mark, ElementType.RECORD_FIELD_BINDING)
        x.MarkAndDone(lid.Range, ElementType.EXPRESSION_REFERENCE_NAME)
        x.ProcessExpression(expr)

    member x.ProcessRecordFieldBinding(SynExprRecordField((lid, _), equalsRange, expr, blockSep)) =
        let (LidWithTrivia lid) = lid
        match lid, expr with
        | SynIdentWithTriviaRange headRange :: _, Some(ExprRange exprRange as expr) ->
            let mark = x.Mark(headRange)
            x.PushRangeForMark(exprRange, mark, ElementType.RECORD_FIELD_BINDING)
            x.PushRecordBlockSep(blockSep)
            x.ProcessReferenceName(lid)
            x.ProcessExpression(expr)

        | SynIdentWithTriviaRange headRange :: _, _ ->
            let mark = x.Mark(headRange)
            let bindingRange = 
                match equalsRange with
                | Some range -> range
                | _ -> headRange
            x.PushRangeForMark(bindingRange, mark, ElementType.RECORD_FIELD_BINDING)
            x.PushRecordBlockSep(blockSep)
            x.ProcessReferenceName(lid)

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

    member x.ProcessMatchClause(SynMatchClause(pat, whenExprOpt, expr, _, _, _) as clause) =
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


type MarkAndType =
    { Mark: int
      ElementType: NodeType }

type EndNodeProcessor() =
    inherit StepProcessorBase<MarkAndType>()

    override x.Process(item, builder) =
        builder.Done(item.Mark, item.ElementType)


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
    inherit StepProcessorBase<SynExprRecordField list>()

    override x.Process(fields, builder) =
        builder.ProcessRecordFieldBindingList(fields)


type AnonRecordBindingListRepresentationProcessor() =
    inherit StepProcessorBase<(SynLongIdent * range option * SynExpr) list>()

    override x.Process(fields, builder) =
        builder.ProcessAnonRecordFieldBindingList(fields)


type ExpressionListProcessor() =
    inherit StepListProcessorBase<SynExpr>()

    override x.Process(expr, builder) =
        builder.ProcessExpression(expr)


type SecondaryBindingListProcessor() =
    inherit StepListProcessorBase<SynBinding>()

    override x.Process(binding, builder) =
        builder.ProcessLocalBinding(binding, true)


type AndLocalBindingListProcessor() =
    inherit StepListProcessorBase<SynExprAndBang>()

    override x.Process(SynExprAndBang(_, _, _, pat, expr, range, _), builder) =
        builder.ProcessAndBangLocalBinding(pat, expr, range)


type RecordFieldBindingListProcessor() =
    inherit StepListProcessorBase<SynExprRecordField>()

    override x.Process(field, builder) =
        builder.ProcessRecordFieldBinding(field)


type AnonRecordFieldBindingListProcessor() =
    inherit StepListProcessorBase<SynLongIdent * range option * SynExpr>()

    override x.Process(field, builder) =
        builder.ProcessAnonRecordFieldBinding(field)


type MatchClauseListProcessor() =
    inherit StepListProcessorBase<SynMatchClause>()

    override x.Process(matchClause, builder) =
        builder.ProcessMatchClause(matchClause)


type ObjectExpressionMemberListProcessor() =
    inherit StepListProcessorBase<SynMemberDefn>()

    override x.Process(binding, builder) =
        builder.ProcessTypeMember(binding)


type FinishObjectExpressionMemberProcessor() =
    inherit StepProcessorBase<unit>()

    override x.Process(_, builder) =
        builder.EnsureMembersAreFinished()


type InterfaceImplementationListProcessor() =
    inherit StepListProcessorBase<SynInterfaceImpl>()

    override x.Process(interfaceImpl, builder) =
        builder.ProcessInterfaceImplementation(interfaceImpl)

type IndexerArgsProcessor() =
    inherit StepProcessorBase<SynExpr>()

    override x.Process(synExpr, builder) =
        match synExpr with
        | SynExpr.DotIndexedGet(_, args, dotRange, range)
        | SynExpr.DotIndexedSet(_, args, _, range, dotRange, _) ->
            let argsListRange = Range.unionRanges dotRange.EndRange range.EndRange
            builder.PushRange(argsListRange, ElementType.INDEXER_ARG_LIST)
            builder.PushExpression(args)

        | _ -> failwithf "Expecting dotIndexedGet/Set, got: %A" synExpr

type InterpolatedStringProcessor() =
    inherit StepListProcessorBase<SynInterpolatedStringPart>()

    override x.Process(stringPart, builder) =
        match stringPart with
        | SynInterpolatedStringPart.String _ -> ()
        | SynInterpolatedStringPart.FillExpr(expr, _) -> builder.ProcessExpression(expr)


[<AutoOpen>]
module BuilderStepProcessors =
    // We have to create these singletons this way instead of object expressions
    // due to compiler producing additional recursive init checks otherwise in this case.

    let expressionProcessor = ExpressionProcessor()
    let sequentialExpressionProcessor = SequentialExpressionProcessor()
    let wrapExpressionProcessor = WrapExpressionProcessor()
    let advanceToPosProcessor = AdvanceToPosProcessor()
    let endNodeProcessor = EndNodeProcessor()
    let endRangeProcessor = EndRangeProcessor()
    let synTypeProcessor = SynTypeProcessor()
    let typeArgsInReferenceExprProcessor = TypeArgsInReferenceExprProcessor()
    let indexerArgsProcessor = IndexerArgsProcessor()
    let recordBindingListRepresentationProcessor = RecordBindingListRepresentationProcessor()
    let anonRecordBindingListRepresentationProcessor = AnonRecordBindingListRepresentationProcessor()
    let finishObjectExpressionMemberListProcessor = FinishObjectExpressionMemberProcessor()

    let expressionListProcessor = ExpressionListProcessor()
    let secondaryBindingListProcessor = SecondaryBindingListProcessor()
    let andLocalBindingListProcessor = AndLocalBindingListProcessor()
    let recordFieldBindingListProcessor = RecordFieldBindingListProcessor()
    let anonRecordFieldBindingListProcessor = AnonRecordFieldBindingListProcessor()
    let matchClauseListProcessor = MatchClauseListProcessor()
    let objectExpressionMemberListProcessor = ObjectExpressionMemberListProcessor()
    let interfaceImplementationListProcessor = InterfaceImplementationListProcessor()
    let interpolatedStringProcessor = InterpolatedStringProcessor()
