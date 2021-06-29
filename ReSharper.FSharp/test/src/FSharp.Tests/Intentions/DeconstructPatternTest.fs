namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type DeconstructPatternTest() =
    inherit FSharpContextActionExecuteTestBase<DeconstructPatternAction>()

    override this.ExtraPath = "deconstruct"

    [<Test>] member x.``Tuple - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 03 - Used``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Expr 01``() = x.DoNamedTest()

    [<Test; ActionNotAvailable>] member x.``Not available - Type 01``() = x.DoNamedTest()

