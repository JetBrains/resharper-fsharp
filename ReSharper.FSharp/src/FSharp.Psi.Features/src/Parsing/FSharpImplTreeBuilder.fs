namespace JetBrains.ReSharper.Plugins.FSharp.Psi.LanguageService.Parsing

open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.PrettyNaming

type internal FSharpImplTreeBuilder(file, lexer, decls, lifetime) =
    inherit FSharpTreeBuilderBase(file, lexer, lifetime)

    override x.CreateFSharpFile() =
        let mark = x.Builder.Mark()
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
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(types,_) ->
            for t in types do x.ProcessType t

        | SynModuleDecl.Exception(SynExceptionDefn(exn, members, range),_) ->
            let mark = x.StartException(exn)
            for m in members do x.ProcessTypeMember(m)
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.EXCEPTION_DECLARATION)

        | SynModuleDecl.Open(lidWithDots,range) ->
            range |> x.GetStartOffset |> x.AdvanceToTokenOrOffset FSharpTokenType.OPEN
            let mark = x.Builder.Mark()
            x.ProcessLongIdentifier lidWithDots.Lid
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.OPEN_STATEMENT)

        | SynModuleDecl.Let(_,bindings,_) ->
            for (Binding(_,_,_,_,attrs,_,_,headPat,_,expr,_,_)) in bindings do
                x.ProcessModuleLetPat headPat attrs
                let bodyRange = expr.Range
                bodyRange |> x.GetStartOffset |> x.AdvanceToOffset
                let mark = x.Mark()
                x.ProcessLocalExpression expr
                bodyRange |> x.GetEndOffset |> x.AdvanceToOffset
                x.Done(mark, ElementType.BODY)

        | SynModuleDecl.HashDirective(hashDirective, _) ->
            x.ProcessHashDirective(hashDirective)

        | SynModuleDecl.DoExpr (_, expr, range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            x.ProcessLocalExpression(expr)
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.DO)

        | decl ->
            decl.Range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = x.Builder.Mark()
            decl.Range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.OTHER_MEMBER_DECLARATION)

    member internal x.ProcessHashDirective (ParsedHashDirective(id, args, range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Builder.Mark()

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.HASH_DIRECTIVE)

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
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(extensionMark, ElementType.TYPE_EXTENSION)
        | _ ->
            let mark = x.StartType attrs typeParams lid range
            let elementType =
                match repr with
                | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                    match simpleRepr with
                    | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                        for field in fields do
                            x.ProcessField field
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
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, elementType)

    member internal x.ProcessModuleLetPat (pat: SynPat) (attrs: SynAttributes) =
        match pat with
        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOption,memberParams,_,range) ->
            match lid with
            | [id] ->
                let mark = x.ProcessAttributesAndStartRange attrs (Some id) range
                let idText = id.idText
                let isActivePattern = IsActivePatternName id.idText 
                if isActivePattern then x.ProcessActivePatternId id else x.ProcessIdentifier id

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
            let isActivePattern = IsActivePatternName id.idText 
            if isActivePattern then x.ProcessActivePatternId id else x.ProcessIdentifier id

            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LET)

        | SynPat.Tuple(patterns,_) ->
            for pattern in patterns do
                x.ProcessModuleLetPat pattern attrs

        | SynPat.Paren(pat,_) -> x.ProcessModuleLetPat pat attrs
        | _ -> x.ProcessLocalPat(pat)