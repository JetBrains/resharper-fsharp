namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type NegateIfConditionTest() =
    inherit FSharpContextActionExecuteTestBase<NegateIfConditionAction>()

    override x.ExtraPath = "negateCondition"

    [<Test>] member x.``If 01``() = x.DoNamedTest()
    [<Test>] member x.``If 02 - Separate lines``() = x.DoNamedTest()

    [<Test>] member x.``Expr - = 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - = 02 - Not``() = x.DoNamedTest()
    [<Test>] member x.``Expr - = 03 - Nested``() = x.DoNamedTest()

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type NegateWhileConditionTest() =
    inherit FSharpContextActionExecuteTestBase<NegateWhileConditionAction>()

    override x.ExtraPath = "negateCondition"

    [<Test>] member x.``While 01``() = x.DoNamedTest()
