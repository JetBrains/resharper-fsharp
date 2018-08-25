namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open JetBrains.Application.platforms
open JetBrains.ProjectModel.MSBuild
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework.Projects
open JetBrains.Util.Dotnet.TargetFrameworkIds

type FSharpTestAttribute() =
    inherit TestProjectFilePropertiesProvider(FSharpProjectFileType.FsExtension, MSBuildProjectUtil.CompileElement)

    let targetFramework =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 5), ProfileIdentifier.Default)

    interface ITestFileExtensionProvider with
        member x.Extension = FSharpProjectFileType.FsExtension

    interface ITestPlatformProvider with
        member x.GetTargetFrameworkId() = targetFramework
