namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open System
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util.dataStructures.TypedIntrinsics
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

type FSharpTreeBuilder(file : IPsiSourceFile, lexer : ILexer, parseResults : FSharpParseFileResults, lifetime) as this =
    inherit TreeStructureBuilderBase(lifetime)

    let document = file.Document
    let builder = PsiBuilder(lexer, ElementType.F_SHARP_IMPL_FILE, this, lifetime)

    member private x.GetLineOffset line = document.GetLineStartOffset(line - 1 |> Int32.op_Explicit)
    member private x.GetStartOffset (range : Range.range) = x.GetLineOffset range.StartLine + range.StartColumn
    member private x.GetEndOffset (range : Range.range) = x.GetLineOffset range.EndLine + range.EndColumn
    member private x.GetStartOffset (id : Ident) = x.GetStartOffset id.idRange
    member private x.IsPastEndOfFile = builder.GetTokenType() |> isNull

    member private x.AdvanceToFileEnd () =
        while not x.IsPastEndOfFile do builder.AdvanceLexer() |> ignore

    member private x.AdvanceToOffset offset =
        while builder.GetTokenOffset() < offset && not x.IsPastEndOfFile do builder.AdvanceLexer() |> ignore


    member x.CreateFSharpFile() =
        let fileMark = builder.Mark()

        let elementType =
            match parseResults.ParseTree with
            | Some (ParsedInput.ImplFile (ParsedImplFileInput(_,_,_,_,_,modulesAndNamespaces,_))) ->
                List.iter x.ProcessModuleOrNamespaceDeclaration modulesAndNamespaces
                ElementType.F_SHARP_IMPL_FILE
            | Some (ParsedInput.SigFile (ParsedSigFileInput(_,_,_,_,modulesAndNamespacesSignatures))) ->
                List.iter x.ProcessModuleOrNamespaceSignature modulesAndNamespacesSignatures
                ElementType.F_SHARP_SIG_FILE

            | None ->
                ElementType.F_SHARP_IMPL_FILE
                // FCS couldn't parse file but we need to return correct IFile
                // and want at least basic syntax highlighting

        x.AdvanceToFileEnd()
        x.Done(fileMark, elementType)
        x.GetTree() :> ICompositeElement


    // Top level modules and namespaces

    member private x.ProcessModuleOrNamespaceDeclaration (SynModuleOrNamespace(lid,_,isModule,decls,_,_,_,range)) =
        x.ProcessModuleOrNamespace(lid, isModule, range, (fun () -> List.iter x.ProcessModuleMemberDeclaration decls))

    member private x.ProcessModuleOrNamespaceSignature (SynModuleOrNamespaceSig(lid,_,isModule,sigs,_,_,_,range)) =
        x.ProcessModuleOrNamespace(lid, isModule, range, (fun () -> List.iter x.ProcessModuleMemberSignature sigs))

    member private x.ProcessModuleOrNamespace(lid, isModule, range, processModuleDeclsFun) =
        let idRange = lid.Head.idRange
        if idRange.Start <> idRange.End then
            // When top level namespace or module identifier is missing
            // its ident name is replaced with file name and the range is 1,0-1,0.

            // Namespace range starts after its identifier for some reason,
            // try to locate a keyword after which there may be access modifiers
            let keywordTokenType = if isModule then FSharpTokenType.MODULE else FSharpTokenType.NAMESPACE
            x.GetStartOffset lid.Head |> x.AdvanceToKeywordOrOffset keywordTokenType

        let mark = builder.Mark()
        if idRange.Start <> idRange.End then builder.AdvanceLexer() |> ignore // ignore keyword token

        if isModule then x.ProcessAccessModifiers lid.Head
        x.ProcessLongIdentifier lid
        processModuleDeclsFun()

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, if isModule then ElementType.TOP_LEVEL_MODULE_DECLARATION else ElementType.F_SHARP_NAMESPACE_DECLARATION)

    // Module members

    member private x.ProcessLetPat (pat : SynPat) =
        match pat with
        | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOption,memberParams,_,range) ->
            match lid with
            | [id] ->
                range |> x.GetStartOffset |> x.AdvanceToOffset
                let mark = builder.Mark()
                x.ProcessIdentifier id
                match typeParamsOption with
                | Some (SynValTyparDecls(typeParams,_,_)) ->
                    List.iter x.ProcessTypeParameterOfMethod typeParams
                | _ -> ()
                x.ProcessLocalConstructorArgs memberParams
                range |> x.GetEndOffset |> x.AdvanceToOffset
                x.Done(mark, ElementType.LET)
            | _ -> ()
        | SynPat.Named(_,id,_,_,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = builder.Mark()
            x.ProcessIdentifier id
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.LET)
        | SynPat.Tuple(patterns,_) ->
            List.iter x.ProcessLetPat patterns
        | SynPat.Paren(pat,_) -> x.ProcessLetPat pat
        | _ -> ()

    member private x.ProcessModuleLet (Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_)) =
        x.ProcessLetPat headPat
        x.ProcessLocalExpression expr

    member private x.ProcessModuleMemberDeclaration moduleMember =
        match moduleMember with
        | SynModuleDecl.NestedModule(ComponentInfo(_,_,_,lid,_,_,_,_),_,decls,_,range) as decl ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = builder.Mark()
            builder.AdvanceLexer() |> ignore // ignore keyword token

            let id = lid.Head  // single id or not parsed
            x.ProcessAccessModifiers id
            x.ProcessIdentifier id
            List.iter x.ProcessModuleMemberDeclaration decls

            decl.Range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)

        | SynModuleDecl.Types(types,_) ->
            List.iter x.ProcessType types

        | SynModuleDecl.Exception(exceptionDefn,_) ->
            x.ProcessException exceptionDefn

        | SynModuleDecl.Open(lidWithDots,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let openMark = builder.Mark()
            x.ProcessLongIdentifier lidWithDots.Lid
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(openMark, ElementType.OPEN)

        | SynModuleDecl.Let(_,bindings,range) ->
            List.iter x.ProcessModuleLet bindings

        | decl ->
            decl.Range |> x.GetStartOffset |> x.AdvanceToOffset
            let declMark = builder.Mark()
            decl.Range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(declMark, ElementType.OTHER_MEMBER_DECLARATION)

    member private x.ProcessModuleMemberSignature moduleMember =
        match moduleMember with
        | SynModuleSigDecl.NestedModule(ComponentInfo(_,_,_,lid,_,_,_,_),_,sigs,range) as decl ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = builder.Mark()
            builder.AdvanceLexer() |> ignore // ignore keyword

            let id = lid.Head // single id or not parsed
            x.ProcessAccessModifiers id
            x.ProcessIdentifier id
            List.iter x.ProcessModuleMemberSignature sigs

            decl.Range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark, ElementType.NESTED_MODULE_DECLARATION)
        | _ -> ()
        ()

    member private x.ProcessIdentifier (id : Ident) =
        let range = id.idRange
        x.AdvanceToOffset (x.GetStartOffset range)
        let mark = builder.Mark()
        x.AdvanceToOffset (x.GetEndOffset range)
        x.Done(mark, ElementType.F_SHARP_IDENTIFIER)

    /// Should be called on access modifiers start offset.
    /// Modifiers always go right before an identifier or type parameters.
    member private x.ProcessAccessModifiers (id : Ident) =
        x.ProcessAccessModifiers (x.GetStartOffset id)

    /// Should be called on access modifiers start offset.
    /// Modifiers always go right before an identifier or type parameters.
    member private x.ProcessAccessModifiers (endOffset : int) =
        let accessModifiersMark = builder.Mark()
        x.AdvanceToOffset endOffset
        builder.Done(accessModifiersMark, ElementType.ACCESS_MODIFIERS, null)

    member private x.ProcessTypeParameterOfType p =
        x.ProcessTypeParameter ElementType.TYPE_PARAMETER_OF_TYPE_DECLARATION p

    member private x.ProcessTypeParameterOfMethod p =
        x.ProcessTypeParameter ElementType.TYPE_PARAMETER_OF_METHOD_DECLARATION p

    member private x.ProcessTypeParameter elementType (TyparDecl(_,(Typar(id,_,_)))) =
        id |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()
        x.ProcessIdentifier id
        x.Done(mark, elementType)

    member private x.ProcessException (SynExceptionDefn(SynExceptionDefnRepr(_,(UnionCase(_,id,_,_,_,_)),_,_,_,_),_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()
        builder.AdvanceLexer() |> ignore // ignore keyword token

        x.ProcessAccessModifiers id
        x.ProcessIdentifier id

        range |> x.GetEndOffset |> x.AdvanceToOffset
        builder.Done(mark, ElementType.F_SHARP_EXCEPTION_DECLARATION, null)

    member private x.ProcessUnionCaseType caseType =
        match caseType with
        | UnionCaseFields(fields) ->
            List.iter x.ProcessField fields

        | UnionCaseFullType(_) -> () // todo: used in FSharp.Core only, otherwise warning

    member private x.ProcessUnionCase (UnionCase(_,id,caseType,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()

        x.ProcessIdentifier id
        x.ProcessUnionCaseType caseType

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_UNION_CASE_DECLARATION)

    member private x.AdvanceToKeywordOrOffset (keyword : TokenNodeType) (maxOffset : int) =
        while builder.GetTokenOffset() < maxOffset &&
              (not (builder.GetTokenType().IsKeyword) || builder.GetTokenType() <> keyword) do
            builder.AdvanceLexer() |> ignore

    member private x.ProcessLongIdentifier (lid : Ident list) =
        let startOffset = x.GetStartOffset (List.head lid).idRange
        let endOffset = x.GetEndOffset (List.last lid).idRange

        x.AdvanceToOffset startOffset
        let mark = builder.Mark()
        x.AdvanceToOffset endOffset
        x.Done(mark, ElementType.LONG_IDENTIFIER)

    member private x.ProcessAttribute (attr : SynAttribute) =
        x.AdvanceToOffset (x.GetStartOffset attr.Range)
        let mark = builder.Mark()
        x.ProcessLongIdentifier attr.TypeName.Lid
        x.AdvanceToOffset (x.GetEndOffset attr.Range)
        x.Done(mark, ElementType.F_SHARP_ATTRIBUTE)

    member private x.ProcessEnumCase (EnumCase(_,id,_,_,range)) =
        range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()
        x.ProcessIdentifier id

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_ENUM_MEMBER_DECLARATION)

    member private x.ProcessField (Field(_,_,id,_,_,_,_,range)) =
        let mark =
            match id with
            | Some id ->
                let startOffset = min (x.GetStartOffset id) (x.GetStartOffset range)
                x.AdvanceToOffset startOffset
                let mark = builder.Mark()
                x.ProcessIdentifier id
                mark
            | None ->
                range |> x.GetStartOffset |> x.AdvanceToOffset
                builder.Mark()

        range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark, ElementType.F_SHARP_FIELD_DECLARATION)

    member private x.ProcessLocalId (id : Ident) =
        id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = x.Mark()
        x.ProcessIdentifier id
        x.Done(mark, ElementType.LOCAL_DECLARATION)

    member private x.ProcessSimplePattern (pat : SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,_)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),_,_) ->
            x.ProcessLocalId id
        | _ -> ()

    member private x.ProcessImplicitCtorParam (pat : SynSimplePat) =
        match pat with
        | SynSimplePat.Id(id,_,_,_,_,range)
        | SynSimplePat.Typed(SynSimplePat.Id(id,_,_,_,_,_),_,range) ->
            range |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = builder.Mark()
            x.ProcessLocalId id
            range |> x.GetEndOffset |> x.AdvanceToOffset
            x.Done(mark,ElementType.MEMBER_PARAM)
        | _ -> ()

    member private x.ProcessTypeMemberTypeParams (SynValTyparDecls(typeParams,_,_)) =
        List.iter x.ProcessTypeParameterOfMethod typeParams

    member private x.ProcessMemberDeclaration id (typeParamsOpt : SynValTyparDecls option) memberParams expr =
        x.ProcessIdentifier id
        if typeParamsOpt.IsSome then x.ProcessTypeMemberTypeParams typeParamsOpt.Value
        x.ProcessMemberParams memberParams
        x.ProcessLocalExpression expr

    member private x.ProcessTypeMember (typeMember : SynMemberDefn) =
        typeMember.Range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()

        let memberType =
            match typeMember with
            | SynMemberDefn.ImplicitCtor(_,_,args,selfId,_) ->
                List.iter x.ProcessImplicitCtorParam args
                if selfId.IsSome then x.ProcessLocalId selfId.Value
                ElementType.IMPLICIT_CONSTRUCTOR_DECLARATION

            | SynMemberDefn.ImplicitInherit(SynType.LongIdent(lidWithDots),_,_,_) ->
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.TYPE_INHERIT

            | SynMemberDefn.Interface(SynType.LongIdent(lidWithDots),interfaceMembersOpt,_) ->
                if interfaceMembersOpt.IsSome then
                    List.iter x.ProcessTypeMember interfaceMembersOpt.Value
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.INTERFACE_IMPLEMENTATION

            | SynMemberDefn.Inherit(SynType.LongIdent(lidWithDots),_,_) ->
                x.ProcessLongIdentifier lidWithDots.Lid
                ElementType.INTERFACE_INHERIT

            | SynMemberDefn.Member(Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_),_) ->
                match headPat with
                | SynPat.LongIdent(LongIdentWithDots(lid,_),_,typeParamsOpt,memberParams,_,_) ->
                    match lid with
                    | [id] when id.idText = "new" ->
                        x.ProcessLocalConstructorArgs memberParams
                        x.ProcessLocalExpression expr
                        ElementType.CONSTRUCTOR_DECLARATION

                    | [id] ->
                        x.ProcessMemberDeclaration id typeParamsOpt memberParams expr
                        ElementType.MEMBER_DECLARATION

                    | selfId :: id :: _ ->
                        x.ProcessLocalId selfId
                        x.ProcessMemberDeclaration id typeParamsOpt memberParams expr
                        ElementType.MEMBER_DECLARATION

                    | _ -> ElementType.OTHER_TYPE_MEMBER
                | _ -> ElementType.OTHER_TYPE_MEMBER

            | SynMemberDefn.LetBindings(bindings,_,_,_) ->
                List.iter x.ProcessLocalBinding bindings
                ElementType.TYPE_LET_BINDINGS

            | SynMemberDefn.AbstractSlot(ValSpfn(_,id,typeParams,_,_,_,_,_,_,_,_),_,_) as slot ->
                x.ProcessIdentifier id
                x.ProcessTypeMemberTypeParams typeParams
                ElementType.ABSTRACT_SLOT

            | SynMemberDefn.ValField(Field(_,_,id,_,_,_,_,_),_) ->
                if id.IsSome then x.ProcessIdentifier id.Value
                ElementType.VAL_FIELD

            | SynMemberDefn.AutoProperty(_,_,id,_,_,_,_,_,_,_,_) ->
                x.ProcessIdentifier id
                ElementType.AUTO_PROPERTY

            | _ -> ElementType.OTHER_TYPE_MEMBER

        x.AdvanceToOffset (x.GetEndOffset typeMember.Range)
        x.Done(mark, memberType)

    member private x.ProcessType (TypeDefn(ComponentInfo(attributes, typeParams,_,lid,_,_,_,_), repr, members, range)) =
        let startOffset = x.GetStartOffset (if List.isEmpty attributes then range else (List.head attributes).TypeName.Range)
        x.AdvanceToOffset startOffset
        let mark = builder.Mark()
        List.iter x.ProcessAttribute attributes

        let id = lid.Head
        let idOffset = x.GetStartOffset id

        let typeParamsOffset =
            match List.tryHead typeParams with
            | Some (TyparDecl(_,(Typar(id,_,_)))) -> x.GetStartOffset id
            | None -> idOffset

        x.ProcessAccessModifiers (min idOffset typeParamsOffset)

        if idOffset < typeParamsOffset then
            x.ProcessIdentifier id
            List.iter x.ProcessTypeParameterOfType typeParams
        else
            List.iter x.ProcessTypeParameterOfType typeParams
            x.ProcessIdentifier id

        let elementType =
            match repr with
            | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
                    List.iter x.ProcessField fields
                    ElementType.F_SHARP_RECORD_DECLARATION
                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
                    List.iter x.ProcessEnumCase enumCases
                    ElementType.F_SHARP_ENUM_DECLARATION
                | SynTypeDefnSimpleRepr.Union(_,cases,_) ->
                    List.iter x.ProcessUnionCase cases
                    ElementType.F_SHARP_UNION_DECLARATION
                | SynTypeDefnSimpleRepr.TypeAbbrev(_) ->
                    ElementType.F_SHARP_TYPE_ABBREVIATION_DECLARATION
                | SynTypeDefnSimpleRepr.None(_) ->
                    ElementType.F_SHARP_ABSTRACT_TYPE_DECLARATION
                | _ -> ElementType.F_SHARP_OTHER_SIMPLE_TYPE_DECLARATION
            | SynTypeDefnRepr.Exception(_) ->
                ElementType.F_SHARP_EXCEPTION_DECLARATION
            | SynTypeDefnRepr.ObjectModel(kind, members, _) ->
                List.iter x.ProcessTypeMember members
                match kind with
                | TyconClass -> ElementType.F_SHARP_CLASS_DECLARATION
                | TyconInterface -> ElementType.F_SHARP_INTERFACE_DECLARATION
                | TyconStruct -> ElementType.F_SHARP_STRUCT_DECLARATION
                | _ -> ElementType.F_SHARP_OBJECT_TYPE_DECLARATION
        List.iter x.ProcessTypeMember members

        range |> x.GetEndOffset |> x.AdvanceToOffset
        builder.Done(mark, elementType , null)

    member private x.ProcessMemberParams (memberParams : SynConstructorArgs) =
        match memberParams with
        | Pats(pats) -> List.iter x.ProcessMemberParamPat pats
        | NamePatPairs(idsAndPats,_) -> List.iter (snd >> x.ProcessMemberParamPat) idsAndPats

    member private x.ProcessMemberParamPat (pat : SynPat) =
        pat.Range |> x.GetStartOffset |> x.AdvanceToOffset
        let mark = builder.Mark()
        x.ProcessLocalPat pat
        pat.Range |> x.GetEndOffset |> x.AdvanceToOffset
        x.Done(mark,ElementType.MEMBER_PARAM)

    member private x.ProcessLocalConstructorArgs (args : SynConstructorArgs) =
        match args with
        | Pats(pats) -> List.iter x.ProcessLocalPat pats
        | NamePatPairs(idsAndPats,_) -> List.iter (snd >> x.ProcessLocalPat) idsAndPats

    member private x.ProcessLocalPat (pat : SynPat) =
        match pat with
        | SynPat.LongIdent(_,_,_,patParams,_,range) ->
            x.ProcessLocalConstructorArgs patParams
        | SynPat.Named(_,id,_,_,_)
        | SynPat.OptionalVal(id,_) ->
            id.idRange |> x.GetStartOffset |> x.AdvanceToOffset
            let mark = builder.Mark()
            x.ProcessIdentifier id
            x.Done(mark, ElementType.LOCAL_DECLARATION)
        | SynPat.Tuple(patterns,_)
        | SynPat.StructTuple(patterns,_) ->
            List.iter x.ProcessLocalPat patterns
        | SynPat.Paren(pat,_)
        | SynPat.Typed(pat,_,_) ->
            x.ProcessLocalPat pat
        | SynPat.Ands(pats,_)
        | SynPat.ArrayOrList(_,pats,_) ->
            List.iter x.ProcessLocalPat pats
        | SynPat.Or(pat1,pat2,_) ->
            x.ProcessLocalPat pat1
            x.ProcessLocalPat pat2
        | SynPat.QuoteExpr(expr,_) ->
            x.ProcessLocalExpression expr
        | _ -> ()

    member private x.ProcessLocalBinding (Binding(_,_,_,_,_,_,_,headPat,_,expr,_,_)) =
        x.ProcessLocalPat headPat
        x.ProcessLocalExpression expr

    member private x.ProcessMatchClause (Clause(pat,exprOpt,expr,_,_)) =
        x.ProcessLocalPat pat
        x.ProcessLocalExpression expr

    member private x.ProcessSimplePatterns (pats : SynSimplePats) =
        match pats with
        | SynSimplePats.SimplePats(pats,_) ->
            List.iter x.ProcessSimplePattern pats
        | SynSimplePats.Typed(pats,_,_) ->
            x.ProcessSimplePatterns pats

    member private x.ProcessLocalExpression (expr : SynExpr) =
        match expr with
        | SynExpr.Paren(expr,_,_,_)
        | SynExpr.Quote(_,_,expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Const(_) -> ()

        | SynExpr.Typed(expr,_,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Tuple(exprs,_,_)
        | SynExpr.StructTuple(exprs,_,_)
        | SynExpr.ArrayOrList(_,exprs,_) ->
            List.iter x.ProcessLocalExpression exprs

        | SynExpr.Record(_) -> () // todo

        | SynExpr.New(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.ObjExpr(_) -> () // todo

        | SynExpr.While(_,whileExpr,doExpr,_) ->
            x.ProcessLocalExpression whileExpr
            x.ProcessLocalExpression doExpr

        | SynExpr.For(_,id,idBody,_,toBody,doBody,_) ->
            x.ProcessLocalId id
            x.ProcessLocalExpression idBody
            x.ProcessLocalExpression toBody
            x.ProcessLocalExpression doBody

        | SynExpr.ForEach(_,_,_,pat,enumExpr,bodyExpr,_) ->
            x.ProcessLocalPat pat
            x.ProcessLocalExpression enumExpr
            x.ProcessLocalExpression bodyExpr

        | SynExpr.ArrayOrListOfSeqExpr(_,expr,_)
        | SynExpr.CompExpr(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Lambda(_,_,pats,expr,_) ->
            x.ProcessSimplePatterns pats
            x.ProcessLocalExpression expr

        | SynExpr.MatchLambda(_,_,cases,_,_) ->
            List.iter x.ProcessMatchClause cases

        | SynExpr.Match(_,expr,cases,_,_) ->
            x.ProcessLocalExpression expr
            List.iter x.ProcessMatchClause cases

        | SynExpr.Do(expr,_)
        | SynExpr.Assert(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.App(_,_,funExpr,argExpr,_) ->
            x.ProcessLocalExpression funExpr
            x.ProcessLocalExpression argExpr

        | SynExpr.TypeApp(_) -> ()

        | SynExpr.LetOrUse(_,_,bindings,bodyExpr,_) ->
            List.iter x.ProcessLocalBinding bindings
            x.ProcessLocalExpression bodyExpr

        | SynExpr.TryWith(tryExpr,_,withCases,_,_,_,_) ->
            x.ProcessLocalExpression tryExpr
            List.iter x.ProcessMatchClause withCases

        | SynExpr.TryFinally(tryExpr,finallyExpr,_,_,_) ->
            x.ProcessLocalExpression tryExpr
            x.ProcessLocalExpression finallyExpr

        | SynExpr.Lazy(expr,_) -> x.ProcessLocalExpression expr

        | SynExpr.Sequential(_,_,expr1,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | SynExpr.IfThenElse(ifExpr,thenExpr,elseExprOpt,_,_,_,_) ->
            x.ProcessLocalExpression ifExpr
            x.ProcessLocalExpression thenExpr
            if elseExprOpt.IsSome then
                x.ProcessLocalExpression elseExprOpt.Value

        | SynExpr.Ident(_)
        | SynExpr.LongIdent(_)
        | SynExpr.LongIdentSet(_)
        | SynExpr.DotGet(_)
        | SynExpr.DotSet(_)
        | SynExpr.DotIndexedGet(_)
        | SynExpr.DotIndexedSet(_)
        | SynExpr.NamedIndexedPropertySet(_)
        | SynExpr.DotNamedIndexedPropertySet(_) -> ()

        | SynExpr.TypeTest(expr,_,_)
        | SynExpr.Upcast(expr,_,_)
        | SynExpr.Downcast(expr,_,_)
        | SynExpr.InferredUpcast(expr,_)
        | SynExpr.InferredDowncast(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.Null(_) -> ()

        | SynExpr.AddressOf(_,expr,_,_)
        | SynExpr.TraitCall(_,_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.JoinIn(expr1,_,expr2,_) ->
            x.ProcessLocalExpression expr1
            x.ProcessLocalExpression expr2

        | SynExpr.ImplicitZero(_) -> ()

        | SynExpr.YieldOrReturn(_,expr,_)
        | SynExpr.YieldOrReturnFrom(_,expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.LetOrUseBang(_,_,_,pat,expr,inExpr,_) ->
            x.ProcessLocalPat pat
            x.ProcessLocalExpression expr
            x.ProcessLocalExpression inExpr

        | SynExpr.DoBang(expr,_) ->
            x.ProcessLocalExpression expr

        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.LibraryOnlyStaticOptimization(_)
        | SynExpr.LibraryOnlyUnionCaseFieldGet(_)
        | SynExpr.LibraryOnlyUnionCaseFieldSet(_)
        | SynExpr.LibraryOnlyILAssembly(_)
        | SynExpr.ArbitraryAfterError(_)
        | SynExpr.FromParseError(_)
        | SynExpr.DiscardAfterMissingQualificationAfterDot(_) -> ()

        | SynExpr.Fixed(expr,_) ->
            x.ProcessLocalExpression expr


    override x.Builder = builder
    override x.NewLine = FSharpTokenType.NEW_LINE
    override x.CommentsOrWhiteSpacesTokens = FSharpTokenType.CommentsOrWhitespaces
    override x.GetExpectedMessage name  = NotImplementedException() |> raise

    interface IPsiBuilderTokenFactory with
        member x.CreateToken(tokenType, buffer, startOffset, endOffset) =
            tokenType.Create(buffer, TreeOffset(startOffset), TreeOffset(endOffset))
