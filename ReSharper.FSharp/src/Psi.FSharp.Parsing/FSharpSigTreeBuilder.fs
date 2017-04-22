namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast

type FSharpSigTreeBuilder(file, lexer, parseTree, lifetime, logger : ILogger) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Builder.Mark()
        match parseTree with
        | Some (ParsedInput.SigFile (ParsedSigFileInput(_,_,_,_,sigs))) ->
            for s in sigs do x.ProcessTopLevelSignature s
        | _ ->
            logger.LogMessage(LoggingLevel.ERROR, sprintf "FSharpSigTreeBuilder: got %A" parseTree)

        x.FinishFile mark ElementType.F_SHARP_SIG_FILE

    member private x.ProcessTopLevelSignature (SynModuleOrNamespaceSig(lid,_,isModule,sigs,_,_,_,range)) =
        if not lid.IsEmpty then
            let mark = x.StartTopLevelDeclaration lid isModule
            for s in sigs do x.ProcessModuleMemberSignature s
            x.FinishTopLevelDeclaration mark range isModule

    member private x.ProcessModuleMemberSignature moduleMember =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(ComponentInfo(attrs,_,_,lid,_,_,_,_),_,sigs,range) ->
            let mark = x.StartNestedModule attrs lid range
            for s in sigs do x.ProcessModuleMemberSignature s
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleSigDecl.Types(types,_) ->
            for t in types do x.ProcessTypeSignature t

        | SynModuleSigDecl.Exception(SynExceptionSig(exn,_,_),range) ->
            x.ProcessException exn

        | SynModuleSigDecl.ModuleAbbrev(id,_,range) ->
            id |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessIdentifier id
            x.Done(mark, ElementType.F_SHARP_MODULE_ABBREVIATION)

        | SynModuleSigDecl.Val(ValSpfn(attrs,id,SynValTyparDecls(typeParams,_,_),_,_,_,_,_,_,_,_),range) ->
            let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
            x.ProcessIdentifier id
            for p in typeParams do x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LET)
        | _ -> ()

    member private x.ProcessTypeSignature (TypeDefnSig(ComponentInfo(attrs, typeParams,_,lid,_,_,_,_), typeSig, members, range)) =
        let mark = x.StartType attrs typeParams lid range
        let elementType =
            match typeSig with
            | SynTypeDefnSigRepr.Simple(simpleRepr,_) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                    for f in fields do x.ProcessField f
                    ElementType.F_SHARP_RECORD_DECLARATION

                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
                    for c in enumCases do x.ProcessEnumCase c
                    ElementType.F_SHARP_ENUM_DECLARATION

                | SynTypeDefnSimpleRepr.Union(_,cases,_) ->
                    for c in cases do x.ProcessUnionCase c
                    ElementType.F_SHARP_UNION_DECLARATION

                | SynTypeDefnSimpleRepr.TypeAbbrev(_) ->
                    ElementType.F_SHARP_TYPE_ABBREVIATION_DECLARATION

                | SynTypeDefnSimpleRepr.None(_) ->
                    ElementType.F_SHARP_ABSTRACT_TYPE_DECLARATION

                | _ -> ElementType.F_SHARP_OTHER_SIMPLE_TYPE_DECLARATION

            | SynTypeDefnSigRepr.Exception(_) ->
                ElementType.F_SHARP_EXCEPTION_DECLARATION

            | SynTypeDefnSigRepr.ObjectModel(kind, members,_) ->
                for m in members do x.ProcessTypeMemberSignature m
                match kind with
                | TyconClass -> ElementType.F_SHARP_CLASS_DECLARATION
                | TyconInterface -> ElementType.F_SHARP_INTERFACE_DECLARATION
                | TyconStruct -> ElementType.F_SHARP_STRUCT_DECLARATION
                | _ -> ElementType.F_SHARP_OBJECT_TYPE_DECLARATION

        for m in members do x.ProcessTypeMemberSignature m
        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, elementType)

    member private x.ProcessTypeMemberSignature memberSig =
        match memberSig with
        | SynMemberSig.Member(ValSpfn(_,id,_,_,_,_,_,_,_,_,_),flags,_) ->
            id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Mark()
            x.ProcessIdentifier id
            let elementType =
                if flags.IsDispatchSlot then
                    ElementType.ABSTRACT_SLOT
                else
                    match flags.MemberKind with
                    | MemberKind.Constructor -> ElementType.CONSTRUCTOR_DECLARATION
                    | _ -> ElementType.MEMBER_DECLARATION
            x.Done(mark,elementType)

        | SynMemberSig.ValField(Field(_,_,id,_,_,_,_,_),_) ->
            if id.IsSome then
                let id = id.Value
                id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
                let mark = x.Mark()
                x.ProcessIdentifier id
                x.Done(mark,ElementType.VAL_FIELD)

        | SynMemberSig.Inherit(SynType.LongIdent(lidWithDots),_) ->
            let lid = lidWithDots.Lid
            if not lid.IsEmpty then
                let id = lid.Head
                id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
                let mark = x.Mark()
                x.ProcessLongIdentifier lidWithDots.Lid
                x.Done(mark, ElementType.INTERFACE_INHERIT)

        | SynMemberSig.Interface(SynType.LongIdent(lidWithDots),_) ->
            let lid = lidWithDots.Lid
            if not lid.IsEmpty then
                let id = lid.Head
                id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
                let mark = x.Mark()
                x.ProcessLongIdentifier lidWithDots.Lid
                x.Done(mark, ElementType.INTERFACE_INHERIT)
        | _ -> ()