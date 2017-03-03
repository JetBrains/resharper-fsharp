namespace JetBrains.ReSharper.Psi.FSharp.Parsing

open System
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.FSharp.Impl.Tree
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.TreeBuilder
open JetBrains.Util.dataStructures.TypedIntrinsics
open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler

type FSharpTreeBuilder(file : IPsiSourceFile, lexer : ILexer, ast : ParsedInput, lifetime) =
    inherit TreeStructureBuilderBase(lifetime)

    let document = file.Document
    let tokenFactory = FSharpPsiBuilderTokenFactory()
    let builder = PsiBuilder(lexer, ElementType.F_SHARP_IMPL_FILE, tokenFactory, lifetime)

    let getLineOffset line = document.GetLineStartOffset(line - 1 |> Int32.op_Explicit)
    let getStartOffset (range : Range.range) = getLineOffset range.StartLine + range.StartColumn
    let getEndOffset (range : Range.range) = getLineOffset range.EndLine + range.EndColumn

    let advanceToOffset offset =
        while builder.GetTokenOffset() < offset && not(isNull(builder.GetTokenType())) do
            builder.AdvanceLexer() |> ignore

    let isTypedCase (UnionCase(_,_,fieldType,_,_,_)) =
        match fieldType with
        | UnionCaseFields([]) -> false
        | _ -> true

    let processIdentifier (id : Ident) =
        id.idRange |> getStartOffset |> advanceToOffset
        let idMark = builder.Mark()
        id.idRange |> getEndOffset |> advanceToOffset
        builder.Done(idMark, ElementType.F_SHARP_IDENTIFIER, null)
    
    /// Should be called on access modifiers start offset.
    /// Always goes right before identifier.
    let processAccessModifiers (endOffset : int) =
        let accessModifiersMark = builder.Mark()
        advanceToOffset endOffset
        builder.Done(accessModifiersMark, ElementType.ACCESS_MODIFIERS, null)

    let processException (SynExceptionDefn(SynExceptionDefnRepr(_,(UnionCase(_,id,_,_,_,_)),_,_,_,_),_,range)) =
            range |> getStartOffset |> advanceToOffset
            let mark = builder.Mark()
            builder.AdvanceLexer() |> ignore // ignore keyword token

            processAccessModifiers (getStartOffset id.idRange)
            processIdentifier id

            range |> getEndOffset |> advanceToOffset
            builder.Done(mark, ElementType.F_SHARP_EXCEPTION_DECLARATION, null)

    let processUnionCase (UnionCase(_,id,caseType,_,_,range) as case) =
        if isTypedCase case then
            range |> getStartOffset |> advanceToOffset
            let exnMark = builder.Mark()
            processIdentifier id

    //        processUnionCaseTypes caseType

            range |> getEndOffset |> advanceToOffset
    //        let elementType = if isTypedCase case
    //                          then ElementType.F_SHARP_TYPED_UNION_CASE_DECLARATION
    //                          else ElementType.F_SHARP_SINGLETON_UNION_CASE_DECLARATION
            builder.Done(exnMark, ElementType.F_SHARP_TYPED_UNION_CASE_DECLARATION, null)

    member private x.AdvanceToKeywordOrOffset (keyword : string) (maxOffset : int) =
        while builder.GetTokenOffset() < maxOffset &&
              (not (builder.GetTokenType().IsKeyword) || builder.GetTokenText() <> keyword) do
            builder.AdvanceLexer() |> ignore

    member private x.ProcessLongIdentifier (lid : Ident list) =
        let startOffset = getStartOffset (List.head lid).idRange
        let endOffset = getEndOffset (List.last lid).idRange

        advanceToOffset startOffset
        let mark = builder.Mark()
        advanceToOffset endOffset
        x.Done(mark, ElementType.LONG_IDENTIFIER)

    member private x.ProcessType (TypeDefn(ComponentInfo(attributes,_,_,lid,_,_,_,_), repr, members, range)) =
//        advanceToOffset (getStartOffset (if List.isEmpty attributes then range else (List.head attributes).TypeName.Range))
//        List.iter processAttribute attributes
        advanceToOffset (getStartOffset range)
        let mark = builder.Mark()

        let id = lid.Head
        processAccessModifiers (getStartOffset id.idRange)
        processIdentifier id

        let elementType =
            match repr with
            | SynTypeDefnRepr.Simple(simpleRepr, _) ->
                match simpleRepr with
                | SynTypeDefnSimpleRepr.Record(_,fields,_) ->
//                    List.iter processField fields
                    ElementType.F_SHARP_RECORD_DECLARATION
                | SynTypeDefnSimpleRepr.Enum(enumCases,_) ->
//                    List.iter processEnumCase enumCases
                    ElementType.F_SHARP_ENUM_DECLARATION
                | SynTypeDefnSimpleRepr.Union(_,cases,_) ->
                    List.iter processUnionCase cases
                    ElementType.F_SHARP_UNION_DECLARATION
                | SynTypeDefnSimpleRepr.TypeAbbrev(_) ->
                    ElementType.F_SHARP_TYPE_ABBREVIATION_DECLARATION
                | _ -> ElementType.F_SHARP_OTHER_SIMPLE_TYPE_DECLARATION
            | SynTypeDefnRepr.Exception(_) ->
                ElementType.F_SHARP_EXCEPTION_DECLARATION
//            | SynTypeDefnRepr.ObjectModel(kind, members, range) ->
//                List.iter processTypeMember members
//                match kind with
//                | TyconClass -> ElementType.F_SHARP_CLASS_DECLARATION
//                | TyconInterface -> ElementType.F_SHARP_INTERFACE_DECLARATION
//                | TyconStruct -> ElementType.F_SHARP_STRUCT_DECLARATION
//                | _ -> ElementType.F_SHARP_UNSPECIFIED_OBJECT_TYPE_DECLARATION
            | _ -> ElementType.OTHER_MEMBER_DECLARATION
//        List.iter processTypeMember members

        range |> getEndOffset |> advanceToOffset
        builder.Done(mark, elementType , null)

    member private x.ProcessModuleMember = function
        | SynModuleDecl.NestedModule(ComponentInfo(_,_,_,lid,_,_,_,_),_,decls,_,range) as decl ->
            range |> getStartOffset |> advanceToOffset
            let mark = builder.Mark()
            builder.AdvanceLexer() |> ignore // ignore keyword token

            let id = List.head lid 
            processAccessModifiers (getStartOffset id.idRange)
            processIdentifier id // always single identifier or not parsed at all instead

            List.iter x.ProcessModuleMember decls

            decl.Range |> getEndOffset |> advanceToOffset
            builder.Done(mark, ElementType.NESTED_MODULE_DECLARATION, null)
        | SynModuleDecl.Types(types,_) -> List.iter x.ProcessType types
        | SynModuleDecl.Exception(exceptionDefn,_) -> processException exceptionDefn
        | decl ->
            decl.Range |> getStartOffset |> advanceToOffset
            let declMark = builder.Mark()
            decl.Range |> getEndOffset |> advanceToOffset
            builder.Done(declMark, ElementType.OTHER_MEMBER_DECLARATION, null)

    member private x.ProcessModuleOrNamespace (SynModuleOrNamespace(lid,_,isModule,decls,_,_,_,range)) =
        // When top level namespace or module identifier is missing
        // its ident name is replaced with file name and the range is 1,0-1,0.
        
        let lidStartOffset = getStartOffset lid.Head.idRange
        x.AdvanceToKeywordOrOffset (if isModule then "module" else "namespace") lidStartOffset
        let mark = builder.Mark()
        builder.AdvanceLexer() |> ignore // ignore keyword token

        if isModule then processAccessModifiers lidStartOffset
        x.ProcessLongIdentifier lid
        List.iter x.ProcessModuleMember decls

        range |> getEndOffset |> advanceToOffset
        x.Done(mark, if isModule then ElementType.MODULE_DECLARATION else ElementType.F_SHARP_NAMESPACE_DECLARATION)

    override x.Builder = builder
    override x.NewLine = FSharpTokenType.NEW_LINE
    override x.CommentsOrWhiteSpacesTokens = FSharpTokenType.CommentsOrWhitespaces
    override x.GetExpectedMessage(name) = NotImplementedException() |> raise

    member x.CreateFSharpFile() =
        let fileMark = builder.Mark()

        let elementType =
            match ast with
            | ParsedInput.ImplFile (ParsedImplFileInput(_,isScript,_,_,_,modulesAndNamespaces,_)) ->
                List.iter x.ProcessModuleOrNamespace modulesAndNamespaces
                ElementType.F_SHARP_IMPL_FILE
            | ParsedInput.SigFile (ParsedSigFileInput(_)) -> ElementType.F_SHARP_SIG_FILE

        ast.Range |> getEndOffset |> advanceToOffset

        x.Done(fileMark, elementType)
        x.GetTree() :> ICompositeElement


