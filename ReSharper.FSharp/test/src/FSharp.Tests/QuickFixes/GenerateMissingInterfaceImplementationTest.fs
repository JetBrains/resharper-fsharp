namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type GenerateMissingInterfaceImplementationTest() =
    inherit FSharpQuickFixTestBase<GenerateMissingInterfaceMembersFix>()

    override x.RelativeTestDataPath = "features/quickFixes/generateMissingInterfaceImplementation"

    [<Test>] member x.``Empty interface implementation - concrete``() = x.DoNamedTest()
    [<TestSetting(typeof<FSharpFormatSettingsKey>, "SpaceAfterComma", "false")>]
    [<Test>] member x.``Empty interface implementation - concrete with settings``() = x.DoNamedTest()
    [<Test>] member x.``Empty interface implementation - generics``() = x.DoNamedTest()
