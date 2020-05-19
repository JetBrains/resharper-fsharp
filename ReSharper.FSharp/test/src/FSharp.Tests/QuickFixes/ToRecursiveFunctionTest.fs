namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
[<ExpectErrors 39>]
type ToRecursiveFunctionTest() =
    inherit QuickFixTestBase<ToRecursiveFunctionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toRecursiveFunction"

    [<Test>] member x.``Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Top level 02 - Alignment``() = x.DoNamedTest()
    [<Test>] member x.``Local 01``() = x.DoNamedTest()
