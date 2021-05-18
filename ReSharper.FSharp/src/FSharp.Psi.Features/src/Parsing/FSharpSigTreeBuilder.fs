namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.Syntax
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util

type internal FSharpSigTreeBuilder(sourceFile, lexer, sigs, lifetime) =
    inherit FSharpTreeBuilderBase(sourceFile, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for s in sigs do
            x.ProcessTopLevelSignature(s)
        x.FinishFile(mark, ElementType.F_SHARP_SIG_FILE)

    member x.ProcessTopLevelSignature(SynModuleOrNamespaceSig(lid, _, isModule, sigDecls, XmlDoc xmlDoc, attrs, _, range)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, isModule, xmlDoc, range)
        for sigDecl in sigDecls do
            x.ProcessModuleMemberSignature(sigDecl)
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberSignature(moduleMember) =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(SynComponentInfo(attrs, _, _, lid, XmlDoc xmlDoc, _, _, _), _, memberSigs, range) ->
            let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, xmlDoc, List.tryHead lid, range)
            for memberSig in memberSigs do
                x.ProcessModuleMemberSignature(memberSig)
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleSigDecl.Types(typeSigs, range) ->
            let mark = x.Mark(typeSigGroupStartRange typeSigs range |> fst)

            match typeSigs with
            | [] -> ()
            | primary :: secondary ->
                x.ProcessTypeSignature(primary, FSharpTokenType.TYPE)
                for typeDefn in secondary do
                    x.ProcessTypeSignature(typeDefn, FSharpTokenType.AND)
            x.Done(range, mark, ElementType.TYPE_DECLARATION_GROUP)

        | SynModuleSigDecl.Exception(SynExceptionSig(exn, members, range), _) ->
            let mark = x.StartException(exn)
            x.ProcessTypeMembers(members)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleSigDecl.ModuleAbbrev(IdentRange range, lid, _) ->
            x.ProcessNamedTypeReference(lid)
            x.MarkAndDone(range, ElementType.MODULE_ABBREVIATION_DECLARATION)

        | SynModuleSigDecl.Val(SynValSig(attrs, id, _, synType, arity, _, _, XmlDoc xmlDoc, _, exprOption, _), range) ->
            let valMark = x.MarkAndProcessAttributesOrIdOrRange(attrs, xmlDoc, Some id, range)

            let patMark = x.Mark(id.idRange)
            let referenceNameMark = x.Mark()
            if IsActivePatternName id.idText then
                x.ProcessActivePatternDecl(id, false)
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

    member x.ProcessTypeSignature(SynTypeDefnSig(info, repr, memberSigs, range), typeKeywordType) =
        let (SynComponentInfo(attrs, typeParams, constraints, lid, XmlDoc xmlDoc, _, _, _)) = info

        let mark = x.StartType(attrs, xmlDoc, typeParams, constraints, lid, range, typeKeywordType)
        match repr with
        | SynTypeDefnSigRepr.Simple(simpleRepr, _) ->
            x.ProcessSimpleTypeRepresentation(simpleRepr)

        | SynTypeDefnSigRepr.ObjectModel(kind, members, range) ->
            if x.AddObjectModelTypeReprNode(kind) then
                let mark = x.Mark(range)
                x.ProcessTypeMembers(members)
                let elementType = x.GetObjectModelTypeReprElementType(kind)
                x.Done(range, mark, elementType)
            else
                x.ProcessTypeMembers(members)

        | _ -> failwithf "Unexpected simple type representation: %A" repr

        x.ProcessTypeMembers(memberSigs)
        x.Done(range, mark, ElementType.F_SHARP_TYPE_DECLARATION)

    member x.ProcessTypeMembers(members: SynMemberSig list) =
        for m in members do
            x.ProcessTypeMemberSignature(m)

    member x.ProcessTypeMemberSignature(memberSig) =
        match memberSig with
        | SynMemberSig.Member(SynValSig(attrs, id, _, synType, arity, _, _, XmlDoc xmlDoc, _, _, _), flags, range) ->
            let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, xmlDoc, Some id, range)
            x.ProcessSignatureType(arity, synType)
            let elementType =
                if flags.IsDispatchSlot then
                    ElementType.ABSTRACT_MEMBER_DECLARATION
                else
                    match flags.MemberKind with
                    | SynMemberKind.Constructor -> ElementType.CONSTRUCTOR_SIGNATURE
                    | _ -> ElementType.MEMBER_SIGNATURE
            x.Done(range, mark, elementType)

        | SynMemberSig.ValField(SynField(attrs, _, id, synType, _, XmlDoc xmlDoc, _, _), range) ->
            if id.IsSome then
                let mark = x.MarkAndProcessAttributesOrIdOrRange(attrs, xmlDoc, id, range)
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
