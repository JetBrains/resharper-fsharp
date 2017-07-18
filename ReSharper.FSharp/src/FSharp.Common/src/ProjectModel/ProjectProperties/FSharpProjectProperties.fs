namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

open System.Runtime.InteropServices
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel.Impl.Build
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel

type FSharpProjectProperties =
    inherit ProjectPropertiesBase<ManagedProjectConfiguration>

    val mutable targetPlatformData: TargetPlatformData
    val buildSettings: ManagedProjectBuildSettings

    // todo: these constructors are seen as partial member
    new(projectTypeGuids, platformId, factoryGuid, targetFrameworkIds, targetPlatformData) =
        { inherit ProjectPropertiesBase<_>(projectTypeGuids, platformId, factoryGuid, targetFrameworkIds)
          buildSettings = ManagedProjectBuildSettings()
          targetPlatformData = null }

    new(factoryGuid, [<Optional; DefaultParameterValue(null: TargetPlatformData)>] tpd) =
        { inherit ProjectPropertiesBase<_>(factoryGuid)
          buildSettings = ManagedProjectBuildSettings()
          targetPlatformData = tpd }

    override x.BuildSettings = x.buildSettings :> IBuildSettings

    override x.ReadProjectProperties(reader, index) =
        base.ReadProjectProperties(reader, index)
        x.buildSettings.ReadBuildSettings(reader)
        let tpd = TargetPlatformData()
        tpd.Read(reader)
        if not tpd.IsEmpty then x.targetPlatformData <- tpd

    override x.WriteProjectProperties(writer) =
        base.WriteProjectProperties(writer)
        x.buildSettings.WriteBuildSettings(writer)
        if isNull x.targetPlatformData
        then TargetPlatformData.WriteEmpty(writer)
        else x.targetPlatformData.Write(writer)

    override x.Dump(writer, indent) =
        writer.Write(new string(' ', indent * 2))
        writer.WriteLine("F# properties:")
        x.DumpActiveConfigurations(writer, indent)
        writer.Write(new string(' ', 2 + indent * 2))
        x.buildSettings.Dump(writer, indent + 2)
        base.Dump(writer, indent + 1)

    interface ISdkConsumerProperties with
        member x.TargetPlatformData = x.targetPlatformData
        member x.ProjectKind = ProjectKind.REGULAR_PROJECT
        member x.DefaultLanguage = FSharpProjectLanguage.Instance :> ProjectLanguage

        member x.BuildSettings = x.BuildSettings
        member x.Dump(writer, indent) = x.Dump(writer, indent)
        member x.WriteProjectProperties(writer) = x.WriteProjectProperties(writer)

        member x.PlatformId = base.PlatformId
        member x.ProjectTypeGuids = base.ProjectTypeGuids
        member x.OwnerFactoryGuid = base.OwnerFactoryGuid
        member x.ActiveConfigurations = base.ActiveConfigurations
        member x.UpdateFrom(properties) = base.UpdateFrom(properties)