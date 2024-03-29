﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Intentions

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type NegateIfConditionTest() =
    inherit FSharpContextActionExecuteTestBase<NegateIfConditionAction>()

    override x.ExtraPath = "negateCondition"

    [<Test>] member x.``If 01``() = x.DoNamedTest()
    [<Test>] member x.``If 02 - Separate lines``() = x.DoNamedTest()

    [<Test>] member x.``Expr - = 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - = 02 - Not``() = x.DoNamedTest()
    [<Test>] member x.``Expr - = 03 - Nested``() = x.DoNamedTest()

[<FSharpTest>]
type NegateWhileConditionTest() =
    inherit FSharpContextActionExecuteTestBase<NegateWhileConditionAction>()

    override x.ExtraPath = "negateCondition"

    [<Test>] member x.``While 01``() = x.DoNamedTest()
