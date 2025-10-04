namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host

open System
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.Platform.MsBuildHost.ProjectModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.MSBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Extensions
open JetBrains.RdBackend.Common.Env
open JetBrains.RdBackend.Common.Features.ProjectModel.View.EditProperties.Projects.MsBuild
open JetBrains.RdBackend.Common.Features.ProjectModel.View.EditProperties.Projects.MsBuild.Providers
open JetBrains.RdBackend.Common.Features.ProjectModel.View.EditProperties.Utils
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.Util

[<ShellFeaturePart>]
type FSharpProjectPropertiesBuilder(projectPropertiesRequests) =
    inherit ManagedProjectPropertiesBuilder(projectPropertiesRequests)

    override x.IsApplicable(projectProperties) =
        projectProperties :? FSharpProjectProperties && base.IsApplicable(projectProperties)

    override x.BuildProjectBuildSettings(project, buildSettings) =
        let fsBuildSettings = buildSettings :?> FSharpBuildSettings
        fsBuildSettings.TailCalls <-
            let mutable value = Unchecked.defaultof<bool>
            match bool.TryParse(project.GetPropertyValue(FSharpProperties.TailCalls), &value) with
            | true -> value
            | _ -> false

        base.BuildProjectBuildSettings(project, buildSettings)

    override this.BuildProjectConfiguration(rdProjectDescriptor, project, configuration) =
        base.BuildProjectConfiguration(rdProjectDescriptor, project, configuration)

        let languageVersion =
            let languageVersion = project.GetPropertyValue(MSBuildProjectUtil.LanguageVersionProperty, true)
            FSharpLanguageVersion.parseCompilationOption languageVersion

        let nullable =
            project.GetPropertyValueOrNull(MSBuildProjectUtil.NullableProperty)
            |> Option.ofObj
            |> Option.map ((=) "enable")

        let configuration = configuration.As<IFSharpProjectConfiguration>()
        configuration.LanguageVersion <- languageVersion
        configuration.Nullable <- nullable


[<SolutionComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<IReSharperHostNetFullFeatureZone>)>]
type FSharpLanguageSpecificItemsProvider() =
    interface IMsBuildConfigurationTabProvider with
        member x.Order = Int32.MaxValue

        member x.CreateSections(_, project, properties, _) =
            if project.ProjectProperties :? FSharpProjectProperties then
                [| EditPropertyItemBuilder
                       .Section(ConfigurationTabProvider.CompileSectionTitle)
                       .AddCheckBox(properties, FSharpProperties.TailCalls, "Generate tail calls") |] :> _

            else EmptyList.Instance :> _

        member x.GetTabTitle _ = "General"
