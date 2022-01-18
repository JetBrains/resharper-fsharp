namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddExtensionAttributeTest() =
    inherit FSharpQuickFixTestBase<AddExtensionAttributeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addExtensionAttribute"

    [<Test>] member x.``Module - Nested 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Nested 02 - Add open``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Module - Top level 02``() = x.DoNamedTest()

    [<Test>] member x.``Type member 01``() = x.DoNamedTest()
    [<Test>] member x.``Type member 02 - Add open``() = x.DoNamedTest()
