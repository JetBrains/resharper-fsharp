namespace rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open JetBrains.Application
open System
open System.Collections.Generic
open System.Runtime.InteropServices
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Impl.Build
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic.Components
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.Util
open JetBrains.Util.PersistentMap

[<AllowNullLiteral>]
type IFSharpProjectConfiguration =
    inherit IManagedProjectConfiguration

    abstract LanguageVersion: FSharpLanguageVersion with get, set

type FSharpProjectConfiguration() as this =
    inherit ManagedProjectConfigurationBase()

    let configuration = this :> IFSharpProjectConfiguration

    interface IFSharpProjectConfiguration with
        member val LanguageVersion = FSharpLanguageVersion.Default with get, set

    override this.WriteConfiguration(writer, stringIntern) =
        base.WriteConfiguration(writer, stringIntern)
        writer.WriteEnum(configuration.LanguageVersion)

    override this.ReadConfiguration(reader, stringIntern) =
        base.ReadConfiguration(reader, stringIntern)
        configuration.LanguageVersion <- reader.ReadEnum(FSharpLanguageVersion.Default)

    override this.UpdateFrom(otherConfiguration) =
        let fsConfiguration = otherConfiguration.As<IFSharpProjectConfiguration>()
        if isNull fsConfiguration then false else

        configuration.LanguageVersion <- fsConfiguration.LanguageVersion
        base.UpdateFrom(otherConfiguration)


type FSharpProjectProperties =
    inherit ProjectPropertiesBase<FSharpProjectConfiguration>

    val mutable targetPlatformData: TargetPlatformData
    val buildSettings: FSharpBuildSettings

    new (projectTypeGuids: ICollection<_>, factoryGuid, targetFrameworkIds, targetPlatformData, dotNetCoreSDK) =
        { inherit ProjectPropertiesBase<_>(projectTypeGuids, factoryGuid, targetFrameworkIds, dotNetCoreSDK)
          buildSettings = FSharpBuildSettings()
          targetPlatformData = targetPlatformData }

    new (factoryGuid, [<Optional; DefaultParameterValue(null: TargetPlatformData)>] targetPlatformData) =
        { inherit ProjectPropertiesBase<_>(factoryGuid)
          buildSettings = FSharpBuildSettings()
          targetPlatformData = targetPlatformData }

    override x.BuildSettings = x.buildSettings :> _
    override x.DefaultLanguage = FSharpProjectLanguage.Instance

    override x.ReadProjectProperties(reader, stringIntern) =
        base.ReadProjectProperties(reader, stringIntern)
        x.buildSettings.ReadBuildSettings(reader, stringIntern)
        let tpd = TargetPlatformData()
        tpd.Read(reader)
        if not tpd.IsEmpty then x.targetPlatformData <- tpd

    override x.WriteProjectProperties(writer, stringIntern) =
        base.WriteProjectProperties(writer, stringIntern)
        x.buildSettings.WriteBuildSettings(writer, stringIntern)
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

    override x.WriteBuildSettings(writer, stringIntern) =
        base.WriteBuildSettings(writer, stringIntern)
        writer.Write(x.TailCalls)

    override x.ReadBuildSettings(reader, stringIntern) =
        base.ReadBuildSettings(reader, stringIntern)
        x.TailCalls <- reader.ReadBool()

    override x.Dump(writer, indent) =
        writer.Write(String(' ', 2 + indent * 2))
        writer.WriteLine(sprintf "TailCalls:%b" x.TailCalls)

        base.Dump(writer, indent)

    override this.UpdateFrom(settings) =
        match settings with
        | :? FSharpBuildSettings -> base.UpdateFrom(settings)
        | _ -> false


[<ShellComponent>]
type FSharpProjectApplicableProvider() =
    interface ProjectConfigurationValidator.IApplicableProvider with
        member x.IsApplicable(projectMark) =
            isFSharpProject projectMark.Location projectMark.TypeGuid


[<ShellFeaturePart>]
type FSharpProjectMarkTypeGuidProvider() =
    inherit ProjectMarkTypeGuidProvider()

    override x.IsApplicable(projectMark) =
        projectMark.Location.ExtensionNoDot = FsprojExtension

    override x.GetActualTypeGuid _ = fsProjectTypeGuid


[<ProjectModelExtension>]
type FSharpProjectPropertiesFactory() =
    inherit UnknownProjectPropertiesFactory()

    static let factoryGuid = Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}")
    static let projectTypeGuids = [| fsProjectTypeGuid |]

    override x.FactoryGuid = factoryGuid

    override x.IsApplicable(parameters) =
        isFSharpProject parameters.ProjectFilePath parameters.ProjectTypeGuid

    override x.IsKnownProjectTypeGuid(projectTypeGuid) =
        FSharpProjectPropertiesFactory.IsKnownProjectTypeGuid(projectTypeGuid)

    override x.CreateProjectProperties(parameters) =
        FSharpProjectProperties(parameters.ProjectTypeGuids, factoryGuid, parameters.TargetFrameworkIds,
            parameters.TargetPlatformData, parameters.DotNetCorePlatform) :> _

    static member CreateProjectProperties(targetFrameworkIds): IProjectProperties =
        FSharpProjectProperties(projectTypeGuids, factoryGuid, targetFrameworkIds, null, null) :> _

    override x.Read(reader, stringIntern) =
        let projectProperties = FSharpProjectProperties(factoryGuid)
        projectProperties.ReadProjectProperties(reader, stringIntern)
        projectProperties :> _

    static member IsKnownProjectTypeGuid(guid) = isFSharpGuid guid


[<AutoOpen>]
module Util =
    let [<Literal>] FsprojExtension = "fsproj"

    let fsProjectTypeGuid = Guid("{F2A71F9B-5D33-465A-A702-920D77279786}")
    let fsCpsProjectTypeGuid = Guid("{6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705}")

    let isFSharpProjectFile (path: VirtualFileSystemPath) =
        path.ExtensionNoDot.Equals(FsprojExtension, StringComparison.OrdinalIgnoreCase)

    let isFSharpGuid (guid: Guid) =
        guid = fsProjectTypeGuid || guid = fsCpsProjectTypeGuid

    let isFSharpProject (path: VirtualFileSystemPath) (guid: Guid) =
        isFSharpProjectFile path || isFSharpGuid guid

    let (|FSharpProject|_|) (projectModelElement: IProjectModelElement) =
        match projectModelElement with
        | :? IProject as project when project.IsFSharp -> Some project
        | _ -> None

    let (|FSharpProjectMark|_|) (mark: IProjectMark) =
        if isFSharpProject mark.Location mark.Guid then someUnit else None

    type IProject with
        member x.IsFSharp =
            x.ProjectProperties :? FSharpProjectProperties ||
            isFSharpProjectFile x.ProjectFileLocation


module FSharpProperties =
    let [<Literal>] DotnetFscCompilerPath = "DotnetFscCompilerPath"
    let [<Literal>] FscToolPath = "FscToolPath"
    let [<Literal>] FscToolExe = "FscToolExe"
    let [<Literal>] LangVersion = "LangVersion"
    let [<Literal>] NoWarn = "NoWarn"
    let [<Literal>] OtherFlags = "OtherFlags"
    let [<Literal>] TailCalls = "TailCalls"
    let [<Literal>] TargetProfile = "TargetProfile"
    let [<Literal>] WarnOn = "WarnOn"


[<ShellComponent>]
type FSharpProjectPropertiesRequest() =
    let properties =
        [| FSharpProperties.DotnetFscCompilerPath
           FSharpProperties.FscToolPath
           FSharpProperties.FscToolExe
           FSharpProperties.LangVersion
           FSharpProperties.OtherFlags
           FSharpProperties.NoWarn
           FSharpProperties.TailCalls
           FSharpProperties.TargetProfile
           FSharpProperties.WarnOn |]

    interface IProjectPropertiesRequest with
        member x.RequestedProperties = properties :> _
