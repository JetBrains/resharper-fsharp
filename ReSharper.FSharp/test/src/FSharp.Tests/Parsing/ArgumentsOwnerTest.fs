namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open FSharp.Compiler.SourceCodeServices
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ExpressionSelection
open JetBrains.ReSharper.FeaturesTestFramework.Utils
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.TestFramework
open JetBrains.TextControl
open NUnit.Framework

[<AttributeUsage(AttributeTargets.Method, AllowMultiple=false)>]
type ExpectErrorsAttribute () =
    inherit Attribute()

[<FSharpTest>]
[<TestPackages("FSharp.Core")>]
[<TestReferences("System.Drawing", "System", "System.Core")>]
[<TestProjectOutputType(ProjectOutputType.CONSOLE_EXE)>]
type ArgumentsOwnerTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "parsing/arguments"

    override x.DoTest(lifetime, project) =
        let textControl = x.OpenTextControl(lifetime)

        let expectErrors = x.TestMethod.GetCustomAttributes(typeof<ExpectErrorsAttribute>, true).Length > 0

        let fsFile = textControl.GetFSharpFile(x.Solution)

        if not expectErrors then
            match fsFile.GetParseAndCheckResults(true, "ArgumentsOwnerTest") with
            | None -> failwithf "GetParseAndCheckResults failed"
            | Some results ->

            let errors =
                results.CheckResults.Errors
                |> Array.filter (fun err -> err.Severity = FSharpErrorSeverity.Error)

            match errors with
            | [||] -> ()
            | errors ->
                errors
                |> Array.map (fun err -> sprintf "(%d,%d)-(%d,%d): %s %d: %s" err.Range.StartLine err.Range.StartColumn err.Range.EndLine err.Range.EndColumn err.Subcategory err.ErrorNumber err.Message)
                |> String.concat "\n"
                |> failwithf "Errors occurred while type checking:\n\n%s"

        let expr = FSharpTreeNodeSelectionProvider.Instance.GetExpressionInRange<IArgumentsOwner>(fsFile, textControl.Selection.OneDocumentRangeWithCaret(), true, null)
        if isNull expr then failwithf "Failed to find IArgumentsOwner at selection"

        x.ExecuteWithGold(fun writer ->
            let args = Array.ofSeq expr.Arguments

            let endTexts, ranges =
                args
                |> Array.indexed
                |> Array.collect (fun (i, expr) ->
                    let argRange = sprintf "|(arg #%d)" i, expr.GetDocumentRange().TextRange

                    let matchingParam = expr.MatchingParameter

                    let declRange =
                        if isNull matchingParam then None else

                        match matchingParam.Element with
                        | :? FSharpMethodParameter as param ->
                            // todo: assert document name
                            Some (FSharpRangeUtil.getTextRange textControl.Document param.FSharpSymbol.DeclarationLocation)
                        | :? FSharpExtensionMemberParameter as extParam ->
                            failwith "todo: support extension params"
                        | _ ->
                            None

                    match declRange with
                    | None -> [| argRange |]
                    | Some declRange -> [| argRange; sprintf "|(param #%d)" i, declRange |]
                )
                |> Array.unzip

            let endText rangeIdx = endTexts.[rangeIdx]

            DocumentRangeUtil.DumpRanges(textControl.Document, ranges, (fun _ -> "|"), Func<_,_> endText).ToString()
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

    [<Test>] member x.``Multiple 01 - Curried``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 02 - Tupled``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 03 - Curried fun in paren``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 04 - Curried arg in paren``() = x.DoNamedTest()
    [<Test; ExpectErrors>] member x.``Multiple 05 - Tupled - too few``() = x.DoNamedTest()
    [<Test; ExpectErrors>] member x.``Multiple 06 - Tupled - too many``() = x.DoNamedTest()
    [<Test>] member x.``Multiple 07 - Curried - partial``() = x.DoNamedTest()
    [<Test; ExpectErrors>] member x.``Multiple 08 - Curried - too many``() = x.DoNamedTest()

    [<Test>] member x.``No args 01``() = x.DoNamedTest()

    [<Test>] member x.``Single 01 - Paren``() = x.DoNamedTest()
    [<Test>] member x.``Single 02 - Space paren``() = x.DoNamedTest()
    [<Test>] member x.``Single 03 - Bare``() = x.DoNamedTest()

    [<Test>] member x.``Tuple param 01 - Deconstructed``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 02 - Variable tuple in arg``() = x.DoNamedTest()
    [<Test>] member x.``Tuple param 03 - Param not deconstructed``() = x.DoNamedTest()

    [<Test; ExpectErrors>] member x.``Tuple param 04 - Mismatch - too few``() = x.DoNamedTest()
    [<Test; ExpectErrors>] member x.``Tuple param 05 - Mismatch - too many``() = x.DoNamedTest()
