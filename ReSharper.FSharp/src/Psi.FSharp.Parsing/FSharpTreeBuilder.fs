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

    override x.Builder = builder
    override x.NewLine = FSharpTokenType.NEW_LINE
    override x.CommentsOrWhiteSpacesTokens = FSharpTokenType.CommentsOrWhitespaces
    override x.GetExpectedMessage(name) = NotImplementedException() |> raise

    member this.CreateFSharpFile() =
        let fileMark = builder.Mark()

        let elementType =
            match ast with
            | ParsedInput.ImplFile (ParsedImplFileInput(_)) -> ElementType.F_SHARP_IMPL_FILE
            | ParsedInput.SigFile (ParsedSigFileInput(_)) -> ElementType.F_SHARP_SIG_FILE

        ast.Range |> getEndOffset |> advanceToOffset

        this.Done(fileMark, elementType)
        this.GetTree() :> ICompositeElement


