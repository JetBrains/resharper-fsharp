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
    
    let advanceToKeyword (keyword : string) =
        while not (builder.GetTokenType().IsKeyword) || builder.GetTokenText() <> keyword do
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

    let processLongIdentifier (lid : Ident list) =
        let head = List.head lid
        let tail = List.last lid

        head.idRange |> getStartOffset |> advanceToOffset
        let lidMark = builder.Mark()
        tail.idRange |> getEndOffset |> advanceToOffset
        builder.Done(lidMark, ElementType.LONG_IDENTIFIER, null)
    
    let processException (SynExceptionDefn(SynExceptionDefnRepr(_,(UnionCase(_,id,_,_,_,_)),_,_,_,_),_,range)) =
            range |> getStartOffset |> advanceToOffset
            let exnMark = builder.Mark()
            processIdentifier id

            range |> getEndOffset |> advanceToOffset
            builder.Done(exnMark, ElementType.F_SHARP_EXCEPTION_DECLARATION, null)

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

    let processType (TypeDefn(ComponentInfo(attributes,_,_,lid,_,_,_,_), repr, members, range)) =
        (if List.isEmpty attributes then range else (List.head attributes).TypeName.Range) |> getStartOffset |> advanceToOffset
        let typeMark = builder.Mark()
//        List.iter processAttribute attributes

        processIdentifier (List.head lid)

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
        builder.Done(typeMark, elementType , null)

    let rec processModuleMember = function
        | SynModuleDecl.NestedModule(ComponentInfo(_,_,_,lid,_,_,_,_),_,decls,_,range) as decl ->
            let id = List.head lid
            id.idRange |> getStartOffset |> advanceToOffset
            let moduleMark = builder.Mark()
            processIdentifier id

            List.iter processModuleMember decls

            decl.Range |> getEndOffset |> advanceToOffset
            builder.Done(moduleMark, ElementType.NESTED_MODULE_DECLARATION, null)
        | SynModuleDecl.Types(types,_) -> List.iter processType types
        | SynModuleDecl.Exception(exceptionDefn,_) -> processException exceptionDefn
        | decl ->
            decl.Range |> getStartOffset |> advanceToOffset
            let declMark = builder.Mark()
            decl.Range |> getEndOffset |> advanceToOffset
            builder.Done(declMark, ElementType.OTHER_MEMBER_DECLARATION, null)

    let processModuleOrNamespace (SynModuleOrNamespace(lid,_,isModule,decls,_,_,_,range) as ns) =
        //advanceToKeyword (if isModule then "module" else "namespace")
        let nsMark = builder.Mark() // whole module or namespace

        builder.AdvanceLexer() |> ignore
        let accessMark = builder.Mark()
        lid.Head.idRange |> getStartOffset |> advanceToOffset
        builder.Done(accessMark, ElementType.ACCESS_MODIFIERS, null)
        
        processLongIdentifier lid

        List.iter processModuleMember decls

        ns.Range |> getEndOffset |> advanceToOffset
        let declType = if isModule then ElementType.MODULE_DECLARATION else ElementType.F_SHARP_NAMESPACE_DECLARATION
        builder.Done(nsMark, declType, null)

    override x.Builder = builder
    override x.NewLine = FSharpTokenType.NEW_LINE
    override x.CommentsOrWhiteSpacesTokens = FSharpTokenType.CommentsOrWhitespaces
    override x.GetExpectedMessage(name) = NotImplementedException() |> raise

    member this.CreateFSharpFile() =
        let fileMark = builder.Mark()

        let elementType =
            match ast with
            | ParsedInput.ImplFile (ParsedImplFileInput(_,isScript,_,_,_,modulesAndNamespaces,_)) ->
                List.iter processModuleOrNamespace modulesAndNamespaces
                ElementType.F_SHARP_IMPL_FILE
            | ParsedInput.SigFile (ParsedSigFileInput(_)) -> ElementType.F_SHARP_SIG_FILE

        ast.Range |> getEndOffset |> advanceToOffset

        this.Done(fileMark, elementType)
        this.GetTree() :> ICompositeElement


