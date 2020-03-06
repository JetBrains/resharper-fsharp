namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

// todo: add test with signature files

type ToMutableRecordFieldActionTest() =
    inherit FSharpContextActionExecuteTestBase<ToMutableAction>()

    override x.ExtraPath = "toMutable"

    [<Test>] member x.``Record field 01``() = x.DoNamedTest()

type ToMutableRecordFieldActionAvailabilityTest() =
    inherit FSharpContextActionAvailabilityTestBase<ToMutableAction>()

    override x.ExtraPath = "toMutable"

    [<Test>] member x.``Record field 01 - Mutable``() = x.DoNamedTest()
