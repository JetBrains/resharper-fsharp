namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type UseNamedAccess() =
    inherit FSharpContextActionExecuteTestBase<UseNamedAccessAction>()

    override x.ExtraPath = "useNamedAccess"
    
    [<Test>] member x.``WildCard 01``() = x.DoNamedTest()
    [<Test>] member x.``Constant 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()

type UseNamedAccessAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<UseNamedAccessAction>()

    override x.ExtraPath = "useNamedAccess"

    [<Test>] member x.``Multiline Pattern 01``() = x.DoNamedTest()
