namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

open JetBrains.ProjectModel.Properties

[<ProjectModelExtension>]
type FSharpProjectFilePropertiesProvider() =
    inherit ProjectFilePropertiesProviderBase()

    override x.IsApplicable(properties) = properties :? FSharpProjectProperties
    override x.CreateProjectFileProperties() = ProjectFileProperties() :> IProjectFileProperties