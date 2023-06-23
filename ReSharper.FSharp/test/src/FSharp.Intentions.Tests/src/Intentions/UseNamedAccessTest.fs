namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

type UseNamedAccess() =
    inherit FSharpContextActionExecuteTestBase<UseNamedAccessAction>()

    override x.ExtraPath = "useNamedAccess"
    
    [<Test>] member x.``WildCard 01``() = x.DoNamedTest()
    [<Test>] member x.``Constant 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Single Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Single Field 02``() = x.DoNamedTest()

type UseNamedAccessAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<UseNamedAccessAction>()

    override x.ExtraPath = "useNamedAccess"

    [<Test>] member x.``Multiline Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Nameless Fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Incomplete ReferenceName 01``() = x.DoNamedTest()
