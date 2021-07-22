namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

type DeconstructPatternTest() =
    inherit FSharpContextActionExecuteTestBase<DeconstructPatternAction>()

    override this.ExtraPath = "deconstruct"

    [<Test>] member x.``Tuple - Lambda 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Lambda 03 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Decl 03 - Abbreviation``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Let - Expr 03 - Used``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Match 02 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Member 01 - Used names``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Parameter owner 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Decl 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple - Wild - Expr 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case - Single - Import 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Let 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 02 - Import``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single - Rqa 03 - Rqa module``() = x.DoNamedTest()
    [<Test>] member x.``Union case - Single 01``() = x.DoNamedTest()

    [<Test>] member x.``Union case fields - Generic 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields - Generic 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields - Generic 03 - Array``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 02``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 03``() = x.DoNamedTest()

    [<Test; ActionNotAvailable>] member x.``Not available - Constructor param 01``() = x.DoNamedTest()
    [<Test; ActionNotAvailable>] member x.``Not available - Type 01``() = x.DoNamedTest()
    [<Test; ActionNotAvailable>] member x.``Not available - Union case - Single no fields``() = x.DoNamedTest()
