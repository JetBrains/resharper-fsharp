namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ConvertNamespaceToModuleTest() =
    inherit QuickFixTestBase<ConvertNamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/convertNamespaceToModule"

    [<Test>] member x.``Simple``() = x.DoNamedTest()
