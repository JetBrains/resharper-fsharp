namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToRecursiveLetBindingsTest() =
    inherit FSharpQuickFixTestBase<ToRecursiveLetBindingsFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toRecursiveLetBindings"

    [<Test>] member x.``Module 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Alignment``() = x.DoNamedTest()
