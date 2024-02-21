namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open NUnit.Framework

// todo: add test with signature files

type ToMutableRecordFieldActionTest() =
    inherit FSharpContextActionExecuteTestBase<ToMutableAction>()

    override x.ExtraPath = "toMutable"

    [<Test>] member x.``Record field 01``() = x.DoNamedTest()


type ToImmutableRecordFieldActionTest() =
    inherit FSharpContextActionExecuteTestBase<ToImmutableFieldAction>()

    override x.ExtraPath = "toMutable"

    [<Test>] member x.``Record - To Immutable 01``() = x.DoNamedTest()
    [<Test>] member x.``Record - To Immutable 02``() = x.DoNamedTest()
    [<Test>] member x.``Record - To Immutable 03``() = x.DoNamedTest()


type ToMutableRecordFieldActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToMutableAction>()

    override x.ExtraPath = "toMutable"

    [<Test>] member x.``Record field 01 - Mutable``() = x.DoNamedTest()
