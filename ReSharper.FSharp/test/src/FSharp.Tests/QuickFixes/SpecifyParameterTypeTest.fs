namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

[<FSharpTest>]
type SpecifyParameterTypeTest() =
    inherit FSharpQuickFixTestBase<SpecifyParameterTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/specifyParameterType"

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Ref 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Ref 03 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Constructor 01``() = x.DoNamedTest()
    [<Test>] member x.``Method 01``() = x.DoNamedTest()

    [<Test>] member x.``Qualified type 01 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Qualified type 02 - Not imported``() = x.DoNamedTest()
