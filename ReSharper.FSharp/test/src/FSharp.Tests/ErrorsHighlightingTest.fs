namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open System
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestFileExtension(FSharpProjectFileType.FsExtension)>]
type ErrorsHighlightingTest() =
    inherit HighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/errorsHighlighting"

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>]
    member x.``Empty file``() = x.DoNamedTest()

    [<Test>]
    member x.``No errors 01``() = x.DoNamedTest()

    [<Test>]
    member x.``Syntax errors 01``() = x.DoNamedTest()

    [<Test>]
    member x.``Syntax errors 02``() = x.DoNamedTest()

    [<Test>]
    member x.``Type check errors 01 - type mismatch``() = x.DoNamedTest()

    [<Test>]
    member x.``Type check errors 02 - nested error``() = x.DoNamedTest()
