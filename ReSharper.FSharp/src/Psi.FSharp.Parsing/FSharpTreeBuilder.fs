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

    let rec processModuleMember = function
        | SynModuleDecl.NestedModule(ComponentInfo(_,_,_,lid,_,_,_,_),_,decls,_,range) as decl ->
            let id = List.head lid
            id.idRange |> getStartOffset |> advanceToOffset
            let moduleMark = builder.Mark()
            processIdentifier id

            List.iter processModuleMember decls

            decl.Range |> getEndOffset |> advanceToOffset
            builder.Done(moduleMark, ElementType.NESTED_MODULE_DECLARATION, null)
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


