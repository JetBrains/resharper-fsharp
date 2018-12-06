namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

open JetBrains.Application
open System
open System.Runtime.InteropServices
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Impl.Build
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic.Components
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.Util


type FSharpProjectProperties =
    inherit ProjectPropertiesBase<ManagedProjectConfiguration>

    val mutable targetPlatformData: TargetPlatformData
    val buildSettings: FSharpBuildSettings

    new(projectTypeGuids, factoryGuid, targetFrameworkIds, targetPlatformData, dotNetCoreSDK) =
        { inherit ProjectPropertiesBase<_>(projectTypeGuids, factoryGuid, targetFrameworkIds, dotNetCoreSDK)
          buildSettings = FSharpBuildSettings()
          targetPlatformData = targetPlatformData }

    new(factoryGuid, [<Optional; DefaultParameterValue(null: TargetPlatformData)>] targetPlatformData) =
        { inherit ProjectPropertiesBase<_>(factoryGuid)
          buildSettings = FSharpBuildSettings()
          targetPlatformData = targetPlatformData }

    override x.BuildSettings = x.buildSettings :> _
    override x.DefaultLanguage = FSharpProjectLanguage.Instance

    override x.ReadProjectProperties(reader) =
        base.ReadProjectProperties(reader)
        x.buildSettings.ReadBuildSettings(reader)
        let tpd = TargetPlatformData()
        tpd.Read(reader)
        if not tpd.IsEmpty then x.targetPlatformData <- tpd

    override x.WriteProjectProperties(writer) =
        base.WriteProjectProperties(writer)
        x.buildSettings.WriteBuildSettings(writer)
        match x.targetPlatformData with
        | null -> TargetPlatformData.WriteEmpty(writer)
        | _ -> x.targetPlatformData.Write(writer)

    override x.Dump(writer, indent) =
        writer.Write(new string(' ', indent * 2))
        writer.WriteLine("F# properties:")
        x.DumpActiveConfigurations(writer, indent)
        writer.Write(new string(' ', 2 + indent * 2))
        x.buildSettings.Dump(writer, indent + 2)
        base.Dump(writer, indent + 1)


and FSharpBuildSettings() =
    inherit ManagedProjectBuildSettings()

    member val TailCalls = Unchecked.defaultof<bool> with get, set

    override x.WriteBuildSettings(writer) =
        base.WriteBuildSettings(writer)
        writer.Write(x.TailCalls)

    override x.ReadBuildSettings(reader) =
        base.ReadBuildSettings(reader)
        x.TailCalls <- reader.ReadBool()

    override x.Dump(writer, indent) =
        writer.Write(String(' ', 2 + indent * 2))
        writer.WriteLine(sprintf "TailCalls:%b" x.TailCalls)

        base.Dump(writer, indent)

[<ShellComponent>]
type FSharpProjectApplicableProvider() =
    interface ProjectConfigurationValidator.IApplicableProvider with
        member x.IsApplicable(projectMark) =
            projectMark.Location.ExtensionNoDot.Equals("fsproj", StringComparison.OrdinalIgnoreCase)


[<ProjectModelExtension>]
type FSharpProjectPropertiesFactory() =
    inherit UnknownProjectPropertiesFactory()

    static let factoryGuid = Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}")
    static let fsProjectTypeGuid = Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")
    static let fsCpsProjectTypeGuid = Guid("{6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705}")

    override x.FactoryGuid = factoryGuid

    override x.IsApplicable(parameters) =
        FSharpProjectPropertiesFactory.IsKnownProjectTypeGuid(parameters.ProjectTypeGuid)

    override x.IsKnownProjectTypeGuid(projectTypeGuid) =
        FSharpProjectPropertiesFactory.IsKnownProjectTypeGuid(projectTypeGuid)

    override x.CreateProjectProperties(parameters) =
        FSharpProjectProperties(parameters.ProjectTypeGuids, factoryGuid, parameters.TargetFrameworkIds,
                                parameters.TargetPlatformData, parameters.DotNetCoreSDK) :> _

    static member CreateProjectProperties(targetFrameworkIds): IProjectProperties =
        FSharpProjectProperties([fsProjectTypeGuid].AsCollection(), factoryGuid, targetFrameworkIds, null, null) :> _

    override x.Read(reader) =
        let projectProperties = FSharpProjectProperties(factoryGuid)
        projectProperties.ReadProjectProperties(reader)
        projectProperties :> _

    static member IsKnownProjectTypeGuid(guid) =
        guid.Equals(fsProjectTypeGuid) || guid.Equals(fsCpsProjectTypeGuid)


[<ProjectModelExtension>]
type FSharpProjectFilePropertiesProvider() =
    inherit ProjectFilePropertiesProvider()

    override x.IsApplicable(properties) = properties :? FSharpProjectProperties
    override x.CreateProjectFileProperties() = ProjectFileProperties() :> IProjectFileProperties


[<AutoOpen>]
module Util =
    type IProject with
        member x.IsFSharp =
            x.ProjectProperties :? FSharpProjectProperties

    let (|FSharpProject|_|) (projectModelElement: IProjectModelElement) =
        match projectModelElement with
        | :? IProject as project when project.IsFSharp -> Some project
        | _ -> None
