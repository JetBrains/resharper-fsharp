namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Refactorings

open System.Collections.Generic
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Refactorings.Test.Common
open NUnit.Framework

[<FSharpTest>]
type IntroduceVarTest() =
    inherit IntroduceVariableTestBase()

    override x.RelativeTestDataPath = "features/refactorings/introduceVar"

    member val Occurrences = null with get, set

    override this.CreateRefactoringWorkflow(control, context) =
        this.Occurrences <- IntroduceVarTest.GetSettings(control.Document.Buffer, "OCCURRENCE") |> Queue
        this.TestLifetime.OnTermination(fun _ -> this.Occurrences <- null) |> ignore

        base.CreateRefactoringWorkflow(control, context)
    
    override this.ProvideOccurrencesData(occurrences, context, control, occurrencesSelectorName) =
        let baseOccurrence = base.ProvideOccurrencesData(occurrences, context, control, occurrencesSelectorName)

        if this.Occurrences.Count = 0 then baseOccurrence else

        match occurrences |> Array.tryFind (fun occurrence -> occurrence.Name.Text = this.Occurrences.Peek()) with
        | Some occurrence ->
            this.Occurrences.Dequeue() |> ignore
            occurrence
        | _ -> baseOccurrence

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03``() = x.DoNamedTest()

    [<Test>] member x.``App 01``() = x.DoNamedTest()
    [<Test>] member x.``App 02``() = x.DoNamedTest()

    [<Test>] member x.``Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Let 02 - Function``() = x.DoNamedTest()
    [<Test>] member x.``Let 03 - Inside other``() = x.DoNamedTest()
    [<Test>] member x.``Let 04 - After other``() = x.DoNamedTest()
    [<Test>] member x.``Let 05 - Nested indent``() = x.DoNamedTest()

    [<Test>] member x.``CompExpr - Computation 01``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Computation - Deconstruction - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Computation - Deconstruction - Union 01``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Computation - Deconstruction - Union 02 - Import``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Computation - Disposable 01``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Computation - Disposable 02``() = x.DoNamedTest()
    [<Test>] member x.``CompExpr - Value 01``() = x.DoNamedTest()

    [<Test>] member x.``Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Match 02 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Match 03 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Match 04 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``LetExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Seq 02``() = x.DoNamedTest()
    [<Test>] member x.``Seq 03 - Last``() = x.DoNamedTest()
    [<Test>] member x.``Seq 04``() = x.DoNamedTest()

    [<Test>] member x.``LetDecl - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl 01``() = x.DoNamedTest()
    [<Test>] member x.``LetDecl 02 - Indent``() = x.DoNamedTest()

    [<Test>] member x.``Do decl - Implicit 01``() = x.DoNamedTest()
    [<Test>] member x.``Do decl - Implicit 02 - App``() = x.DoNamedTest()

    [<Test>] member x.``Member - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Member - Auto property 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Binary app 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 04``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Binary app - Same indent 05``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Lambda 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Match 01``() = x.DoNamedTest()

    [<Test>] member x.``Expr - New 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - New 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - If 01 - Let``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Indexer - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Indexer - Expr 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Indexer - Named 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Indexer - Range 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Indexer - Range 02``() = x.DoNamedTest()

    [<Test>] member x.``Expr - Space - Not needed``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Both 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 02 - List``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 03 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 04 - Anon record``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 05 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - After 06 - Object expr``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 01 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 02 - List``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 03 - Record``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 04 - Anon record``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 05 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Space - Before 06 - Object expr``() = x.DoNamedTest()

    [<Test>] member x.``Record field binding - Anon 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding - Anon 02 - In app``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field binding 02 - In app``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Decl 02``() = x.DoNamedTest()

    [<Test>] member x.``Shift - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Shift - Expr 02``() = x.DoNamedTest()

    [<Test>] member x.``Single line - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Lambda 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Single line - Match 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Single line - When 01``() = x.DoNamedTest()

    [<Test>] member x.``Types 01 - Binary app``() = x.DoNamedTest()
    [<Test>] member x.``Types 02 - Method return``() = x.DoNamedTest()
    [<Test>] member x.``Types 03 - Type check``() = x.DoNamedTest()
    [<Test>] member x.``Types 04 - Disposable``() = x.DoNamedTest()

    [<Test>] member x.``Used names - For 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Seq 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Seq 02 - Before``() = x.DoNamedTest()
    [<Test>] member x.``Used names 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Member - Module 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Member - Module 02 - Nested pat``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Member - Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Member - Type 02 - Member``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - CompExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - CompExpr 02 - Yield``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - RangeSequenceExpr 01``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Indexer - Set 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer - Set 02``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer - Set 03``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer - Set 04``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer 01 - Item``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer 02 - Named``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Indexer 03``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Op 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Op 02 - Prefix app``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - Named arg 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Named arg 02 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - Named arg 03 - Union case field``() = x.DoNamedTest()

    [<Test>] member x.``Not allowed - NewExpr 01``() = x.DoNamedTest()
    [<Test>] member x.``Not allowed - NewExpr 02``() = x.DoNamedTest()
