module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties.FSharpProjectPropertiesFactory

open System
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.Util

let factoryGuid = Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}")
let fsProjectTypeGuid = Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")

[<ProjectModelExtension>]
type Factory() =
    inherit UnknownProjectPropertiesFactory()

    override x.IsApplicable(parameters) = parameters.ProjectTypeGuid.Equals(fsProjectTypeGuid)
    override x.IsKnownProjectTypeGuid(guid) = guid.Equals(fsProjectTypeGuid)
    override x.FactoryGuid = factoryGuid

    override x.CreateProjectProperties(parameters) =
        FSharpProjectProperties(parameters.ProjectTypeGuids, parameters.PlatformId, factoryGuid,
                                parameters.TargetFrameworkIds, parameters.TargetPlatformData) :> _

    static member CreateProjectProperties(platformId, targetFrameworkIds) =
        FSharpProjectProperties([fsProjectTypeGuid].AsCollection(), platformId, factoryGuid, targetFrameworkIds, null)

    override x.Read(reader, index) =
        let projectProperties = FSharpProjectProperties(factoryGuid)
        projectProperties.ReadProjectProperties(reader, index)
        projectProperties :> _
