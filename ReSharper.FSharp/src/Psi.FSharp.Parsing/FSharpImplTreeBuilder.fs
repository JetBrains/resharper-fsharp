namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast

type FSharpImplTreeBuilder(file, lexer, parseTree, lifetime, logger : ILogger) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Builder.Mark()
        match parseTree with
        | Some (ParsedInput.ImplFile (ParsedImplFileInput(_,_,_,_,_,decls,_))) ->
            for decl in decls do x.ProcessTopLevelDeclaration decl
        | _ ->
            logger.LogMessage(LoggingLevel.ERROR, sprintf "FSharpImplTreeBuilder: got %A" parseTree)

        x.FinishFile mark ElementType.F_SHARP_IMPL_FILE

    member private x.ProcessTopLevelDeclaration (SynModuleOrNamespace(lid,_,isModule,decls,_,_,_,range)) =
        if not lid.IsEmpty then
            let mark = x.StartTopLevelDeclaration lid isModule
            for decl in decls do x.ProcessModuleMemberDeclaration decl
            x.FinishTopLevelDeclaration mark range isModule

    member internal x.ProcessModuleMemberDeclaration moduleMember =
        match moduleMember with
        | SynModuleDecl.NestedModule(ComponentInfo(attrs,_,_,lid,_,_,_,_),_,decls,_,range) ->
            let mark = x.StartNestedModule attrs lid range
            for d in decls do x.ProcessModuleMemberDeclaration d
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(types,_) ->
            for t in types do x.ProcessType t

        | SynModuleDecl.Exception(SynExceptionDefn(exn,_,_),_) ->
            x.ProcessException exn
            // todo: members

        | SynModuleDecl.Open(lidWithDots,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessLongIdentifier lidWithDots.Lid
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.OPEN)

        | SynModuleDecl.Let(_,bindings,_) ->
            for (Binding(_,_,_,_,attrs,_,_,headPat,_,expr,_,_)) in bindings do
                x.ProcessModuleLetPat headPat attrs
                x.ProcessLocalExpression expr

        | decl ->
            decl.Range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            decl.Range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.OTHER_MEMBER_DECLARATION)

    member internal x.ProcessType (TypeDefn(ComponentInfo(attrs, typeParams,_,lid,_,_,_,_), repr, members, range)) =
        let mark = x.StartType attrs typeParams lid range

        let elementType =
            match repr with
            | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                    for field in fields do
                        x.ProcessField field
                    ElementType.F_SHARP_RECORD_DECLARATION

                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
                    for case in enumCases do
                        x.ProcessEnumCase case
                    ElementType.F_SHARP_ENUM_DECLARATION

                | SynTypeDefnSimpleRepr.Union(_,cases,_) ->
                    for case in cases do
                        x.ProcessUnionCase case
                    ElementType.F_SHARP_UNION_DECLARATION

                | SynTypeDefnSimpleRepr.TypeAbbrev(_) ->
                    ElementType.F_SHARP_TYPE_ABBREVIATION_DECLARATION

                | SynTypeDefnSimpleRepr.None(_) ->
                    ElementType.F_SHARP_ABSTRACT_TYPE_DECLARATION

                | _ -> ElementType.F_SHARP_OTHER_SIMPLE_TYPE_DECLARATION

            | SynTypeDefnRepr.Exception(_) ->
                ElementType.F_SHARP_EXCEPTION_DECLARATION

            | SynTypeDefnRepr.ObjectModel(kind, members, _) ->
                for m in members do x.ProcessTypeMember m
                match kind with
                | TyconClass -> ElementType.F_SHARP_CLASS_DECLARATION
                | TyconInterface -> ElementType.F_SHARP_INTERFACE_DECLARATION
                | TyconStruct -> ElementType.F_SHARP_STRUCT_DECLARATION
                | _ -> ElementType.F_SHARP_OBJECT_TYPE_DECLARATION

        for m in members do x.ProcessTypeMember m
        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, elementType)

    member internal x.ProcessModuleLetPat (pat : SynPat) (attrs : SynAttributes) =
        match pat with
        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOption,memberParams,_,range) ->
            match lid with
            | [id] ->
                let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
                x.ProcessIdentifier id
                match typeParamsOption with
                | Some (SynValTyparDecls(typeParams,_,_)) ->
                    for p in typeParams do x.ProcessTypeParameter p ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION
                | _ -> ()
                x.ProcessLocalParams memberParams
                range |> x.GetEndOffset |> x.AdvanceToOffset
                x.Done(mark, ElementType.LET)
            | _ -> ()

        | SynPat.Named(_,id,_,_,range) ->
            let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
            x.ProcessIdentifier id
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LET)

        | SynPat.Tuple(patterns,_) ->
            for pattern in patterns do
                x.ProcessModuleLetPat pattern attrs

        | SynPat.Paren(pat,_) -> x.ProcessModuleLetPat pat attrs
        | _ -> ()