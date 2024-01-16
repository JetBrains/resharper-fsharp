namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions
open NUnit.Framework

type ToPositionalFieldPatternsTest() =
    inherit FSharpContextActionExecuteTestBase<ToPositionalFieldPatternsAction>()

    override x.ExtraPath = "toPositionalFieldPatterns"
    
    [<Test>] member x.``Option 01``() = x.DoNamedTest()
    [<Test>] member x.``Option 02``() = x.DoNamedTest()
    [<Test>] member x.``Union 01``() = x.DoNamedTest()
    [<Test>] member x.``Union 02``() = x.DoNamedTest()
    [<Test; Explicit "Used name is shadowed">] member x.``Union 03 - Used name``() = x.DoNamedTest()
    [<Test>] member x.``ValueOption 01``() = x.DoNamedTest()
