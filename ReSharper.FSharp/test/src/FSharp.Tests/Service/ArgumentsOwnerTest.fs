namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Utils
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl
open NUnit.Framework

[<FSharpTest>]
[<TestReferences("System.Drawing", "System", "System.Core")>]
[<TestProjectOutputType(ProjectOutputType.CONSOLE_EXE)>]
type ArgumentsOwnerTest() =
    inherit BaseTestWithTextControl()

    let treeNodeSelection = FSharpTreeNodeSelectionProvider.Instance

    override x.RelativeTestDataPath = "features/service/arguments"

    override x.DoTest(lifetime, _: IProject) =
        let textControl = x.OpenTextControl(lifetime)
        let fsFile = textControl.GetFSharpFile(x.Solution)

        let document = textControl.Document
        let filePath = fsFile.GetSourceFile().GetLocation()

        let selection = textControl.Selection.OneDocumentRangeWithCaret()
        let expr =
            treeNodeSelection
                .GetExpressionInRange<IArgumentsOwner>(fsFile, selection, true, null)
                .NotNull("Failed to find IArgumentsOwner at selection")

        x.ExecuteWithGold(fun writer ->
            let args = Array.ofSeq expr.Arguments

            let endTexts, ranges =
                args
                |> Array.indexed
                |> Array.collect (fun (i, expr) ->
                    let matchingParam = expr.MatchingParameter

                    let declRange =
                        if isNull matchingParam then None else

                        match matchingParam.Element with
                        | :? FSharpMethodParameter as param ->
                            let fcsParameter = param.FcsParameter
                            if isNull fcsParameter then None else

                            let declRange = fcsParameter.DeclarationLocation
                            let declPath =
                                VirtualFileSystemPath.TryParse(declRange.FileName, InteractionContext.SolutionContext)

                            if declPath = filePath then
                                Some(getTextRange document declRange)
                            else
                                None
                        | _ ->
                            None

                    let argRange = sprintf "|(arg #%d)" i, expr.GetDocumentRange().TextRange
                    match declRange with
                    | None -> [| argRange |]
                    | Some declRange -> [| argRange; sprintf "|(param #%d)" i, declRange |])
                |> Array.unzip

            let endText rangeIdx = endTexts[rangeIdx]

            DocumentRangeUtil.DumpRanges(textControl.Document, ranges, (fun _ -> "|"), Func<_,_>(endText)).ToString()
            |> writer.WriteLine

            writer.WriteLine("---------------------------------------------------------")
            for i, arg in Seq.indexed args do
                writer.Write(sprintf "(arg #%d) => " i)
                let param = arg.MatchingParameter
                if isNull param then writer.WriteLine("<no matching param>") else
                writer.WriteLine(param.Element.ShortName)) |> ignore

    [<Test>] member x.``Attribute 01 - Multiple args``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 02 - Single arg - no parens``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 03 - Single arg - parens``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 04 - No args - no parens``() = x.DoNamedTest()
    [<Test>] member x.``Attribute 05 - No args - parens``() = x.DoNamedTest()

    [<Test>] member x.``Compiled 01 - FSharpCore``() = x.DoNamedTest()
    [<Test>] member x.``Compiled 02 - BCL``() = x.DoNamedTest()

    [<Test>] member x.``Constructor 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Constructor 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Constructor 03 - Multiple args``() = x.DoNamedTest()

    [<Test>] member x.``New 01 - Multiple args``() = x.DoNamedTest()
    [<Test>] member x.``New 02 - No args - parens``() = x.DoNamedTest()
    [<Test>] member x.``New 03 - Single arg - no paren``() = x.DoNamedTest()
    [<Test>] member x.``New 04 - Single arg - parens``() = x.DoNamedTest()

    [<Test>] member x.``Extension BCL 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Extension BCL 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Extension BCL 03 - Tupled args``() = x.DoNamedTest()

    [<Test>] member x.``Extension BCL Direct 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Extension BCL Direct 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Extension BCL Direct 03 - Tupled args``() = x.DoNamedTest()

    [<Test>] member x.``Extension CSharp 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Extension CSharp 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Extension CSharp 03 - Tupled args``() = x.DoNamedTest()

    [<Test>] member x.``Extension CSharp Direct 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Extension CSharp Direct 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Extension CSharp Direct 03 - Tupled args``() = x.DoNamedTest()

    [<Test>] member x.``Extension FSharp 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Extension FSharp 02 - Single arg``() = x.DoNamedTest()
    [<Test>] member x.``Extension FSharp 03 - Curried args``() = x.DoNamedTest()
    [<Test>] member x.``Extension FSharp 04 - Tupled args``() = x.DoNamedTest()

    [<Test>] member x.``Generic 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Generic 02 - Static method``() = x.DoNamedTest()

    [<Test>] member x.``Misc 01 - No args``() = x.DoNamedTest()
    [<Test>] member x.``Misc 02 - Too many args``() = x.DoNamedTest()

    [<Test>] member x.``Multiple 01 - Curried``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 02 - Tupled``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 03 - Curried fun in paren``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 04 - Curried arg in paren``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 05 - Tupled - too few``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 06 - Tupled - too many``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 07 - Curried - partial``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 08 - Curried - too many``() = x.DoNamedTest()

    [<Test>] member x.``Named arg 01 - BCL``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 02 - Partial``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 03 - Unordered``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 04 - Invalid name``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 05 - Boolean``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 06 - Single parameter``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 07 - Setter and optional parameter``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 08 - Explicit, setters and optional``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 09 - Explicit, setter, optional, wrong name``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 10 - Setter for a return type``() = x.DoNamedTest()
    [<Test>] member x.``Named arg 11 - Multiple args for a single param``() = x.DoNamedTest()

    [<Test>] member x.``Nested 01 - Pipe as argument``() = x.DoNamedTest()
    [<Test>] member x.``Nested 02 - Pipe in argument - Inner``() = x.DoNamedTest()
    [<Test>] member x.``Nested 03 - Pipe as function``() = x.DoNamedTest()
    [<Test>] member x.``Nested 04 - Pipe as piped argument``() = x.DoNamedTest()
    [<Test>] member x.``Nested 05 - Curried and piped``() = x.DoNamedTest()
    [<Test>] member x.``Nested 06 - Curried and piped - Inner``() = x.DoNamedTest()
    [<Test>] member x.``Nested 07 - Application as argument``() = x.DoNamedTest()
    [<Test>] member x.``Nested 08 - Pipe in lambda``() = x.DoNamedTest()
    [<Test>] member x.``Nested 09 - Pipe in lambda - Inner``() = x.DoNamedTest()
    [<Test>] member x.``Nested 10 - Pipes on both sides``() = x.DoNamedTest()
    [<Test>] member x.``Nested 11 - Operator section``() = x.DoNamedTest()

    [<Test>] member x.``Non IArgument 01 - Obj expression``() = x.DoNamedTest()

    [<Test>] member x.``Pipe 01 - Forward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 02 - Backward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 03 - Double forward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 04 - Forward chain``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 05 - Backward chain``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 06 - Mixed``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 07 - Inner application``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 08 - Tuple param``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 09 - Returned function``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 10 - Compiled``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 11 - Double forward - Variable tuple``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 12 - Function - Forward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 13 - Function - Backward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 14 - Function - Double forward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 15 - Inner pipe``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 16 - Method``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 17 - Method - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 18 - Union case``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 19 - Double backward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 20 - Not a pipe``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 21 - Double backward - Variable tuple``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 22 - Applied pipe``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 23 - Double forward - Tuple param``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 24 - Custom operator``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 25 - Constructor``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 26 - Union case - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 27 - Method application``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 28 - Unit``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 29 - Lambda``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 30 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 31 - Missing argument``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 32 - Triple backward``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 33 - Tuple param element``() = x.DoNamedTest()
    [<Test>] member x.``Pipe 34 - Extension method``() = x.DoNamedTest()

    [<Test>] member x.``Single 01 - Paren``() = x.DoNamedTest()
    [<Test>] member x.``Single 02 - Space paren``() = x.DoNamedTest()
    [<Test>] member x.``Single 03 - Bare``() = x.DoNamedTest()

    [<Test>] member x.``Tuple param 01 - Deconstructed``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 02 - Variable tuple in arg``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 03 - Param not deconstructed``() = x.DoNamedTest()

    [<Test>] member x.``Tuple param 04 - Mismatch - too few``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 05 - Mismatch - too many``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 06 - Function - Not deconstructed``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 07 - Function - Not deconstructed, named-like arg``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 08 - Function - Deconstructed, arg with multiple parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 09 - Function - Not deconstructed, arg with multiple parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 10 - Function - Named-like args``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 11 - Method - In parens, named-like arg``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 12 - Method - Deconstructed, in multiple parens, named-like arg``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 13 - Method - Not deconstructed, in multiple parens, named-like arg``() = x.DoNamedTest()

    [<Test>] member x.``Union case 01 - Single field``() = x.DoNamedTest()
    [<Test>] member x.``Union case 02 - Named fields - Unordered``() = x.DoNamedTest()
    [<Test>] member x.``Union case 03 - Comparison``() = x.DoNamedTest()

    [<Test>] member x.``Unit 01 - Named``() = x.DoNamedTest()
    [<Test>] member x.``Unit 02 - Unnamed``() = x.DoNamedTest()
