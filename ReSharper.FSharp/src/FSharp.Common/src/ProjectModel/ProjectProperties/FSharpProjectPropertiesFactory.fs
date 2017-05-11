namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

open System
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common

[<ProjectModelExtension>]
type FSharpProjectPropertiesFactory() =
    inherit UnknownProjectPropertiesFactory()

    let factoryGuid = Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}")
    static member val FSharpProjectTypeGuid = Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")

    override x.IsApplicable(parameters) = parameters.ProjectTypeGuid.Equals(FSharpProjectPropertiesFactory.FSharpProjectTypeGuid)
    override x.IsKnownProjectTypeGuid(guid) = guid.Equals(FSharpProjectPropertiesFactory.FSharpProjectTypeGuid)
    override x.FactoryGuid = factoryGuid

    override x.CreateProjectProperties(parameters) =
        FSharpProjectProperties(parameters.ProjectTypeGuids, parameters.PlatformId, factoryGuid,
            parameters.TargetFrameworkIds, parameters.TargetPlatformData) :> IProjectProperties

    override x.Read(reader, index) =
        let projectProperties = FSharpProjectProperties(factoryGuid)
        projectProperties.ReadProjectProperties(reader, index)
        projectProperties :> IProjectProperties