namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type UseNamedAccess() =
    inherit FSharpContextActionExecuteTestBase<UseNamedAccessAction>()

    override x.ExtraPath = "useNamedAccess"
    
    [<Test>] member x.``ParametersOwnerPat 01``() = x.DoNamedTest()
