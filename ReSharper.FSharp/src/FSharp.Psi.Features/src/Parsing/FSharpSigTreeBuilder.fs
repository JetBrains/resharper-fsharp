namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.PrettyNaming

type internal FSharpSigTreeBuilder(file, lexer, sigs, lifetime) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Mark()
        for s in sigs do
            x.ProcessTopLevelSignature(s)
        x.FinishFile(mark, ElementType.F_SHARP_SIG_FILE)

    member x.ProcessTopLevelSignature (SynModuleOrNamespaceSig(lid,_,isModule,sigs,_,attrs,_,range)) =
        let mark, elementType = x.StartTopLevelDeclaration lid attrs isModule range
        for s in sigs do x.ProcessModuleMemberSignature s
        x.FinishTopLevelDeclaration mark range elementType

    member x.ProcessModuleMemberSignature moduleMember =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(ComponentInfo(attrs,_,_,lid,_,_,_,_),_,sigs,range) ->
            let mark = x.StartNestedModule attrs lid range
            for s in sigs do x.ProcessModuleMemberSignature s
            x.Done(range, mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleSigDecl.Types(types,_) ->
            for t in types do x.ProcessTypeSignature t

        | SynModuleSigDecl.Exception(SynExceptionSig(exn, members, range),_) ->
            let mark = x.StartException(exn)
            for m in members do x.ProcessTypeMemberSignature(m)
            x.Done(range, mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleSigDecl.ModuleAbbrev(IdentRange idRange as id,_,range) ->
            let mark = x.Mark(idRange)
            x.ProcessIdentifier id
            x.Done(idRange, mark, ElementType.MODULE_ABBREVIATION)

        | SynModuleSigDecl.Val(ValSpfn(attrs,id,SynValTyparDecls(typeParams,_,_),_,_,_,_,_,_,_,_),range) ->
            let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
            let isActivePattern = IsActivePatternName id.idText 
            if isActivePattern then x.ProcessActivePatternId id else x.ProcessIdentifier id
            for p in typeParams do x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION
            x.Done(range, mark, ElementType.LET) // todo: replace with proper pattern
        | _ -> ()

    member x.ProcessTypeSignature (TypeDefnSig(ComponentInfo(attrs, typeParams,_,lid,_,_,_,_), typeSig, members, range)) =
        let mark = x.StartType attrs typeParams lid range
        let elementType =
            match typeSig with
            | SynTypeDefnSigRepr.Simple(simpleRepr,_) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                    for f in fields do x.ProcessField f ElementType.RECORD_FIELD_DECLARATION
                    ElementType.RECORD_DECLARATION

                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
                    for c in enumCases do x.ProcessEnumCase c
                    ElementType.ENUM_DECLARATION

                | SynTypeDefnSimpleRepr.Union(_,cases, range) ->
                    x.ProcessUnionCases(cases, range)
                    ElementType.UNION_DECLARATION

                | SynTypeDefnSimpleRepr.TypeAbbrev(_) ->
                    ElementType.TYPE_ABBREVIATION_DECLARATION

                | SynTypeDefnSimpleRepr.None(_) ->
                    ElementType.ABSTRACT_TYPE_DECLARATION

                | _ -> ElementType.OTHER_SIMPLE_TYPE_DECLARATION

            | SynTypeDefnSigRepr.Exception(_) ->
                ElementType.EXCEPTION_DECLARATION

            | SynTypeDefnSigRepr.ObjectModel(kind, members,_) ->
                for m in members do x.ProcessTypeMemberSignature m
                match kind with
                | TyconClass -> ElementType.CLASS_DECLARATION
                | TyconInterface -> ElementType.INTERFACE_DECLARATION
                | TyconStruct -> ElementType.STRUCT_DECLARATION
                | _ -> ElementType.OBJECT_TYPE_DECLARATION

        for m in members do x.ProcessTypeMemberSignature m
        x.Done(range, mark, elementType)

    member x.ProcessTypeMemberSignature memberSig =
        match memberSig with
        | SynMemberSig.Member(ValSpfn(attrs,id,_,_,_,_,_,_,_,_,_),flags,range) ->
            let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
            x.ProcessIdentifier id
            let elementType =
                if flags.IsDispatchSlot then
                    ElementType.ABSTRACT_SLOT
                else
                    match flags.MemberKind with
                    | MemberKind.Constructor -> ElementType.CONSTRUCTOR_DECLARATION
                    | _ -> ElementType.MEMBER_DECLARATION
            x.Done(mark,elementType)

        | SynMemberSig.ValField(Field(attrs,_,id,_,_,_,_,_),range) ->
            if id.IsSome then
                let mark = x.ProcessAttributesAndStartRange attrs id range
                x.Done(mark,ElementType.VAL_FIELD)

        | SynMemberSig.Inherit(SynType.LongIdent(lidWithDots),_) ->
            let lid = lidWithDots.Lid
            if not lid.IsEmpty then
                let mark = x.Mark(lid.Head.idRange)
                x.ProcessLongIdentifier lid
                x.Done(mark, ElementType.INTERFACE_INHERIT)

        | SynMemberSig.Interface(SynType.LongIdent(lidWithDots),_) ->
            let lid = lidWithDots.Lid
            if not lid.IsEmpty then
                let mark = x.Mark(lid.Head.idRange)
                x.ProcessLongIdentifier lid
                x.Done(mark, ElementType.INTERFACE_INHERIT)
        | _ -> ()
