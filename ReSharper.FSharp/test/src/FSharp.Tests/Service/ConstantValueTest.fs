namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<AbstractClass>]
type TopBindingExpressionTestBase() =
    inherit BaseTestWithTextControl()

    abstract Present: IFSharpExpression -> string

    override x.DoTest(lifetime, _: IProject) =
        let textControl = x.OpenTextControl(lifetime)
        let fsFile = textControl.GetFSharpFile(x.Solution)

        x.ExecuteWithGold(fun writer ->
            for binding in fsFile.Descendants<ITopBinding>() do
                writer.WriteLine($"{binding.HeadPattern.GetText()}: {x.Present(binding.Expression)}")) |> ignore


[<FSharpTest>]
type ConstantValueTest() =
    inherit TopBindingExpressionTestBase()

    override x.RelativeTestDataPath = "features/service/constantValue"

    override x.Present(expr) =
        let constantValue = expr.ConstantValue
        if constantValue.IsString() then
            let charCodes = constantValue.StringValue |> Seq.map (fun c -> string (int c))
            "string " + String.Join(" ", charCodes)
        elif constantValue.IsErrorOrNonCompileTimeConstantValue() then
            "not constant"
        else
            string constantValue.Value

    [<Test>] member x.``Int32 01``() = x.DoNamedTest()
    [<Test>] member x.``String 01``() = x.DoNamedTest()
