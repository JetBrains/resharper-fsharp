namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.PrettyNaming
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

type internal FSharpSigTreeBuilder(sourceFile, lexer, sigs, lifetime) =
    inherit FSharpTreeBuilderBase(sourceFile, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for s in sigs do
            x.ProcessTopLevelSignature(s)
        x.FinishFile(mark, ElementType.F_SHARP_SIG_FILE)

    member x.ProcessTopLevelSignature(SynModuleOrNamespaceSig(lid, _, isModule, sigDecls, _, attrs, _, range)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, isModule, range)
        for sigDecl in sigDecls do
            x.ProcessModuleMemberSignature(sigDecl)
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberSignature(moduleMember) =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(ComponentInfo(attrs, _, _, lid, _, _, _, _), _, sigs, range) ->
            let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, List.tryHead lid, range)
            for s in sigs do x.ProcessModuleMemberSignature s
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleSigDecl.Types(typeSigs, range) ->
            let mark = x.Mark(typeSigGroupStartPos typeSigs range)
            match typeSigs with
            | [] -> ()
            | TypeDefnSig(ComponentInfo(attributes = attrs), _, _, _) :: _ ->
                x.ProcessOuterAttrs(attrs, range)

            for typeSig in typeSigs do
                x.ProcessTypeSignature(typeSig)
            x.Done(range, mark, ElementType.TYPE_DECLARATION_GROUP)

        | SynModuleSigDecl.Exception(SynExceptionSig(exn, members, range), _) ->
            let mark = x.StartException(exn)
            for m in members do x.ProcessTypeMemberSignature(m)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleSigDecl.ModuleAbbrev(IdentRange range, lid, _) ->
            x.ProcessNamedTypeReference(lid)
            x.MarkAndDone(range, ElementType.MODULE_ABBREVIATION_DECLARATION)

        | SynModuleSigDecl.Val(ValSpfn(attrs, id, _, synType, arity, _, _, _, _, exprOption, _), range) ->
            let valMark = x.MarkAndProcessAttributesOrIdOrRange(attrs, Some id, range)

            let patMark = x.Mark(id.idRange)
            let referenceNameMark = x.Mark()
            if IsActivePatternName id.idText then
                x.ProcessActivePatternId(id, false)
            else
                x.AdvanceToEnd(id.idRange)

            x.Done(referenceNameMark, ElementType.EXPRESSION_REFERENCE_NAME)
            x.Done(patMark, ElementType.TOP_REFERENCE_PAT)
            
            let (SynValInfo(_, SynArgInfo(returnAttrs, _, _))) = arity

            let returnInfoStart =
                match returnAttrs with
                | { Range = attrsRange } :: _ -> attrsRange
                | _ -> synType.Range

            let returnInfoStart = x.Mark(returnInfoStart)
            x.ProcessAttributeLists(returnAttrs)
            x.ProcessType(synType)
            x.Done(returnInfoStart, ElementType.RETURN_TYPE_INFO)

            match exprOption with
            | Some expr -> x.MarkChameleonExpression(expr)
            | _ -> ()

            x.Done(valMark, ElementType.BINDING_SIGNATURE)

        | SynModuleSigDecl.Open(openDeclTarget, range) ->
            x.ProcessOpenDeclTarget(openDeclTarget, range)

        | SynModuleSigDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | _ -> ()

    member x.ProcessTypeSignature(TypeDefnSig(info, repr, memberSigs, range)) =
        let (ComponentInfo(attrs, typeParams, constraints, lid, _, _, _, _)) = info

        let mark = x.StartType attrs typeParams constraints lid range
        match repr with
        | SynTypeDefnSigRepr.Simple(simpleRepr, _) ->
            x.ProcessSimpleTypeRepresentation(simpleRepr)

        | SynTypeDefnSigRepr.ObjectModel(kind, members, _) ->
            if x.AddObjectModelTypeReprNode(kind) then
                let mark = x.Mark(range)
                for memberSig in members do
                    x.ProcessTypeMemberSignature(memberSig)

                let elementType = x.GetObjectModelTypeReprElementType(kind)
                x.Done(range, mark, elementType)
            else
                for memberSig in members do
                    x.ProcessTypeMemberSignature(memberSig)

        | _ -> failwithf "Unexpected simple type representation: %A" repr

        for memberSig in memberSigs do
            x.ProcessTypeMemberSignature(memberSig)

        x.Done(range, mark, ElementType.F_SHARP_TYPE_DECLARATION)

    member x.ProcessTypeMemberSignature(memberSig) =
        match memberSig with
        | SynMemberSig.Member(ValSpfn(attrs, id, _, synType, _, _, _, _, _, _, _), flags, range) ->
            let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, Some id, range)
            x.ProcessType(synType)
            let elementType =
                if flags.IsDispatchSlot then
                    ElementType.ABSTRACT_MEMBER_DECLARATION
                else
                    match flags.MemberKind with
                    | MemberKind.Constructor -> ElementType.MEMBER_CONSTRUCTOR_DECLARATION
                    | _ -> ElementType.MEMBER_DECLARATION
            x.Done(range, mark, elementType)

        | SynMemberSig.ValField(Field(attrs, _, id, synType, _, _, _, _), range) ->
            if id.IsSome then
                let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, id, range)
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
