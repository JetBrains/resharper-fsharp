module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties.FSharpProjectPropertiesFactory

open System
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.Util

let factoryGuid = Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}")
let fsProjectTypeGuid = Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")
let fsCpsProjectTypeGuid = Guid("{6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705}")

[<ProjectModelExtension>]
type Factory() =
    inherit UnknownProjectPropertiesFactory()

    override x.IsApplicable(parameters) = x.IsKnownProjectTypeGuid(parameters.ProjectTypeGuid)

    override x.IsKnownProjectTypeGuid(guid) = Factory.IsKnownProjectTypeGuid(guid)

    override x.FactoryGuid = factoryGuid

    override x.CreateProjectProperties(parameters) =
        FSharpProjectProperties(parameters.ProjectTypeGuids, factoryGuid, parameters.TargetFrameworkIds,
                                parameters.TargetPlatformData, parameters.DotNetCoreSDK) :> _

    static member CreateProjectProperties(targetFrameworkIds) =
        FSharpProjectProperties([fsProjectTypeGuid].AsCollection(), factoryGuid, targetFrameworkIds, null, null)

    override x.Read(reader) =
        let projectProperties = FSharpProjectProperties(factoryGuid)
        projectProperties.ReadProjectProperties(reader)
        projectProperties :> _

    static member IsKnownProjectTypeGuid(guid) = guid.Equals(fsProjectTypeGuid) || guid.Equals(fsCpsProjectTypeGuid)
