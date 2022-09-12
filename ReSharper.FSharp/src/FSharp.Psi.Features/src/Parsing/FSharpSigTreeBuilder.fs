namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open FSharp.Compiler.Syntax
open FSharp.Compiler.Syntax.PrettyNaming
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util

type internal FSharpSigTreeBuilder(sourceFile, lexer, sigs, lifetime, path) =
    inherit FSharpTreeBuilderBase(sourceFile, lexer, lifetime, path)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for s in sigs do
            x.ProcessTopLevelSignature(s)
        x.FinishFile(mark, ElementType.F_SHARP_SIG_FILE)

    member x.ProcessTopLevelSignature(SynModuleOrNamespaceSig(lid, _, isModule, sigDecls, XmlDoc xmlDoc, attrs, _, range, _)) =
        let mark, elementType = x.StartTopLevelDeclaration(lid, attrs, isModule, xmlDoc, range)
        for sigDecl in sigDecls do
            x.ProcessModuleMemberSignature(sigDecl)
        x.FinishTopLevelDeclaration(mark, range, elementType)

    member x.ProcessModuleMemberSignature(moduleMember) =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(SynComponentInfo(attrs, _, _, _, XmlDoc xmlDoc, _, _, _), _, memberSigs, range, _) ->
            let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)
            for memberSig in memberSigs do
                x.ProcessModuleMemberSignature(memberSig)
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleSigDecl.Types(typeSigs, range) ->
            let mark = x.Mark(range)

            match typeSigs with
            | [] -> ()
            | primary :: secondary ->
                x.ProcessTypeSignature(primary, FSharpTokenType.TYPE)
                for typeDefn in secondary do
                    x.ProcessTypeSignature(typeDefn, FSharpTokenType.AND)
            x.Done(range, mark, ElementType.TYPE_DECLARATION_GROUP)

        | SynModuleSigDecl.Exception(SynExceptionSig(exn, _, members, range), _) ->
            let mark = x.StartException(exn, range)
            x.ProcessTypeMembers(members)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleSigDecl.ModuleAbbrev(IdentRange range, lid, _) ->
            x.ProcessNamedTypeReference(lid)
            x.MarkAndDone(range, ElementType.MODULE_ABBREVIATION_DECLARATION)

        | SynModuleSigDecl.Val(SynValSig(attrs, (SynIdentRange idRange as SynIdent(id, _)), _, synType, arity, _, _, XmlDoc xmlDoc, _, exprOption, _, _), range) ->
            let valMark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)

            let patMark = x.Mark(idRange)
            let referenceNameMark = x.Mark()
            if IsActivePatternName id.idText then
                x.ProcessActivePatternDecl(idRange, false)
            else
                x.AdvanceToEnd(idRange)

            x.Done(referenceNameMark, ElementType.EXPRESSION_REFERENCE_NAME)
            x.Done(patMark, ElementType.TOP_REFERENCE_PAT)
            x.ProcessReturnTypeInfo(arity, synType)

            match exprOption with
            | Some expr -> x.MarkChameleonExpression(expr)
            | _ -> ()

            x.Done(valMark, ElementType.BINDING_SIGNATURE)

        | SynModuleSigDecl.Open(openDeclTarget, range) ->
            x.ProcessOpenDeclTarget(openDeclTarget, range)

        | SynModuleSigDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | _ -> ()

    member x.ProcessTypeSignature(SynTypeDefnSig(info, repr, memberSigs, range, _) as defnSig, typeKeywordType) =
        let (SynComponentInfo(attrs, typeParams, constraints, lid, XmlDoc xmlDoc, _, _, _)) = info

        // Representation for type augmentation in signatures
        // workaround over https://github.com/dotnet/fsharp/issues/13861
        match repr with
        | SynTypeDefnSigRepr.Simple(SynTypeDefnSimpleRepr.None _, _) when not memberSigs.IsEmpty ->
            x.ProcessTypeExtensionSignature(defnSig, attrs)
        | _ ->

        let mark = x.StartType(attrs, xmlDoc, typeParams, constraints, lid, range, typeKeywordType)
        match repr with
        | SynTypeDefnSigRepr.Simple(simpleRepr, _) ->
            x.ProcessSimpleTypeRepresentation(simpleRepr)

        | SynTypeDefnSigRepr.ObjectModel(kind, members, range) ->
            match kind with
            | SynTypeDefnKind.Delegate(synType, signatureInfo) ->
                let mark = x.Mark(range)
                x.ProcessSignatureType(signatureInfo, synType)
                x.Done(range, mark, ElementType.DELEGATE_REPRESENTATION)

            | _ ->

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

    member x.ProcessTypeExtensionSignature(SynTypeDefnSig(info, _, memberSigs, range, _), attrs) =
        let (SynComponentInfo(_, typeParams, constraints, lid , XmlDoc xmlDoc, _, _, _)) = info
        let mark = x.MarkAndProcessIntro(attrs, xmlDoc, null, range)

        x.ProcessTypeParametersAndConstraints(typeParams, constraints, lid)
        x.ProcessTypeMembers(memberSigs)
        x.Done(range, mark, ElementType.TYPE_EXTENSION_DECLARATION)
