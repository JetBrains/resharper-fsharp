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
            range |> x.GetStartOffset |> x.AdvanceToTokenOrOffset FSharpTokenType.OPEN
            let mark = x.Mark()
            x.ProcessLongIdentifier lidWithDots.Lid
            x.Done(range, mark, ElementType.OPEN_STATEMENT)

        | SynModuleDecl.Let(_, bindings, range) ->
            let letStart = letStartPos bindings range
            let letMark = x.Mark(letStart)
            for binding in bindings do
                x.ProcessBinding(binding)
            x.Done(range, letMark, ElementType.LET)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.DoExpr (_, expr, range) ->
            let mark = x.Mark(range)
            x.ProcessLocalExpression(expr)
            x.Done(range, mark, ElementType.DO)

        | decl ->
            let range = decl.Range
            let mark = x.Mark(range)
            x.Done(range, mark, ElementType.OTHER_MEMBER_DECLARATION)

    member x.ProcessHashDirective(ParsedHashDirective (id, _, range)) =
        let mark = x.Mark(range)
        let elementType =
            match id with
            | "l" | "load" -> ElementType.LOAD_DIRECTIVE
            | "r" | "reference" -> ElementType.REFERENCE_DIRECTIVE
            | "I" -> ElementType.I_DIRECTIVE
            | _ -> ElementType.OTHER_DIRECTIVE
        x.Done(range, mark, elementType)

    member internal x.ProcessType (TypeDefn(ComponentInfo(attrs, typeParams,_,lid,_,_,_,_), repr, members, range)) =
        match repr with
        | SynTypeDefnRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation,_,_) ->
            let tryGetShortName (lid: LongIdent) =
                List.tryLast lid
                |> Option.map (fun id -> id.idText)

            let extensionOffset = x.GetStartOffset(range)
            match tryGetShortName lid with
            | Some name -> x.TypeExtensionsOffsets.Add(name, extensionOffset)
            | _ -> ()

            x.AdvanceToOffset(extensionOffset)
            let extensionMark = x.Mark()
            let typeExpressionMark = x.Mark()
            x.ProcessLongIdentifier lid
            x.Done(typeExpressionMark, ElementType.NAMED_TYPE_EXPRESSION)
            for m in members do
                x.ProcessTypeMember m
            x.Done(range, extensionMark, ElementType.TYPE_EXTENSION)
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

                | SynTypeDefnRepr.ObjectModel(kind, members, _) ->
                    for m in members do x.ProcessTypeMember m
                    match kind with
                    | TyconClass -> ElementType.CLASS_DECLARATION
                    | TyconInterface -> ElementType.INTERFACE_DECLARATION
                    | TyconStruct -> ElementType.STRUCT_DECLARATION
                    | _ -> ElementType.OBJECT_TYPE_DECLARATION

            for m in members do x.ProcessTypeMember m
            x.Done(range, mark, elementType)

    member x.ProcessPat(PatRange range as pat) =
        match pat with
        | SynPat.Wild _ -> ()
        | _ ->

        let mark = x.Mark(range)
        let elementType =
            match pat with
            | SynPat.Named (pat, id, _, _, _) ->
                x.ProcessPat(pat)
                if IsActivePatternName id.idText then x.ProcessActivePatternId(id) else x.ProcessIdentifier(id)
                ElementType.NAMED_PAT

            | SynPat.LongIdent (lid, _, typars, args, _, _) ->
                match lid.Lid with
                | [id] when id.idText = "op_ColonColon" ->
                    match args with
                    | Pats pats -> for p in pats do x.ProcessPat(p)
                    | NamePatPairs (pats, _) -> for _, p in pats do x.ProcessPat(p)
                    ElementType.CONS_PAT

                | [id] ->
                    if IsActivePatternName id.idText then x.ProcessActivePatternId id else x.ProcessIdentifier id

                    match typars with
                    | Some (SynValTyparDecls (typars, _, _)) ->
                        for p in typars do
                            x.ProcessTypeParameter(p, ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION)
                    | None -> ()

                    x.ProcessLocalParams(args)
                    ElementType.LONG_IDENT_PAT

                | _ ->
                    ElementType.LONG_IDENT_PAT

            | SynPat.Typed (pat, _, _) ->
                x.ProcessPat(pat)
                ElementType.TYPED_PAT

            | SynPat.Or (pat1, pat2, _) ->
                x.ProcessPat(pat1)
                x.ProcessPat(pat2)
                ElementType.OR_PAT

            | SynPat.Ands (pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat)
                ElementType.ANDS_PAT

            | SynPat.Tuple (pats, _)
            | SynPat.StructTuple (pats, _)
            | SynPat.ArrayOrList (_, pats, _) ->
                for pat in pats do
                    x.ProcessPat(pat)
                ElementType.LIST_PAT

            | SynPat.Paren (pat,_) ->
                x.ProcessPat(pat)
                ElementType.PAREN_PAT

            | SynPat.Record (pats, _) ->
                for _, pat in pats do
                    x.ProcessPat(pat)
                ElementType.RECORD_PAT

            | SynPat.IsInst _ ->
                ElementType.IS_INST_PAT

            | _ ->
                ElementType.OTHER_PAT

        x.Done(range, mark, elementType)
    
    member x.ProcessExpr(ExprRange range as expr) =
        let mark = x.Mark(range)
        x.ProcessLocalExpression expr
        x.Done(range, mark, ElementType.EXPR)

    member x.ProcessAttributes(attrs) =
        for attr in attrs do
            x.ProcessAttribute(attr)

    member x.ProcessBinding(Binding (_,_,_,_,attrs,_,_,headPat,_, expr,_,_) as binding) =
        // todo: add [< to range?
        x.AdvanceToPos(binding.StartPos)
        let bindingMark = x.Mark()

        x.ProcessAttributes(attrs)
        x.ProcessPat(headPat)
        x.ProcessExpr(expr)

        x.Done(binding.RangeOfBindingAndRhs, bindingMark, ElementType.BINDING)
