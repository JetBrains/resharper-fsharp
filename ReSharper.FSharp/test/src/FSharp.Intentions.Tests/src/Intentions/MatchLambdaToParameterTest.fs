namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<AssertCorrectTreeStructure; DumpPsiTree>]
type MatchLambdaToParameterTest() =
    inherit FSharpContextActionExecuteTestBase<MatchLambdaExprToParameterAction>()

    override x.ExtraPath = "matchLambdaToParameter"

    [<Test>] member x.``Comment 01``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Comment 02``() = x.DoNamedTest()

    [<Test>] member x.``Names - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Names - String 01``() = x.DoNamedTest()
    [<Test>] member x.``Names - Used 01``() = x.DoNamedTest()

    [<Test>] member x.``Single line 01``() = x.DoNamedTest()
