namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ImplementAbstractMemberTest() =
    inherit FSharpContextActionExecuteTestBase<ImplementAbstractMemberAction>()

    override x.ExtraPath = "implementAbstractMember"

    [<Test>] member x.``Class - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Class - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Class - Property 02 - Getter implemented``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Method 02 - Multiple inheritors``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Method 03 - Implemented in one``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Property 02 - Setter``() = x.DoNamedTest()
    [<Test>] member x.``Interface - Property 03 - Getter implemented``() = x.DoNamedTest()


type ImplementAbstractMemberAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ImplementAbstractMemberAction>()

    override x.ExtraPath = "implementAbstractMember"

    [<Test>] member x.``Members 01``() = x.DoNamedTest()
