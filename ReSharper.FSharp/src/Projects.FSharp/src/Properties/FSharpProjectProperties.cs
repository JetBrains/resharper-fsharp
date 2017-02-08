using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Impl.Build;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Common;
using JetBrains.ProjectModel.Properties.Managed;
using PlatformID = JetBrains.Application.platforms.PlatformID;

namespace JetBrains.Platform.ProjectModel.FSharp.Properties
{
  public class FSharpProjectProperties : ProjectPropertiesBase<ManagedProjectConfiguration>, ISdkConsumerProperties
  {
    private readonly ManagedProjectBuildSettings myBuildSettings;
    private TargetPlatformData myTargetPlatformData;

    public FSharpProjectProperties(ICollection<Guid> projectTypeGuids, PlatformID platformId, Guid factoryGuid,
      IReadOnlyCollection<TargetFrameworkId> targetFrameworkIds, TargetPlatformData targetPlatformData)
      : base(projectTypeGuids, platformId, factoryGuid, targetFrameworkIds)
    {
      myTargetPlatformData = targetPlatformData;
      myBuildSettings = new ManagedProjectBuildSettings();
    }

    public FSharpProjectProperties(Guid factoryGuid, TargetPlatformData targetPlatformData = null) : base(factoryGuid)
    {
      myTargetPlatformData = targetPlatformData;
      myBuildSettings = new ManagedProjectBuildSettings();
    }

    public override IBuildSettings BuildSettings => myBuildSettings;
    public ProjectLanguage DefaultLanguage => ProjectLanguage.UNKNOWN;
    public ProjectKind ProjectKind => ProjectKind.REGULAR_PROJECT;
    public TargetPlatformData TargetPlatformData => myTargetPlatformData;

    public override void ReadProjectProperties(BinaryReader reader, ProjectSerializationIndex index)
    {
      base.ReadProjectProperties(reader, index);
      myBuildSettings.ReadBuildSettings(reader);
      var targetPlatformData = new TargetPlatformData();
      targetPlatformData.Read(reader);
      if (!targetPlatformData.IsEmpty)
        myTargetPlatformData = targetPlatformData;
    }

    public override void WriteProjectProperties(BinaryWriter writer)
    {
      base.WriteProjectProperties(writer);
      myBuildSettings.WriteBuildSettings(writer);
      if (TargetPlatformData == null)
        TargetPlatformData.WriteEmpty(writer);
      else
        TargetPlatformData.Write(writer);
    }

    public override void Dump(TextWriter to, int indent = 0)
    {
      to.Write(new string(' ', indent * 2));
      to.WriteLine("F# Properties:");
      DumpActiveConfigurations(to, indent);
      to.Write(new string(' ', 2 + indent * 2));
      to.WriteLine("Build Settings:");
      myBuildSettings.Dump(to, indent + 2);
      base.Dump(to, indent + 1);
    }
  }
}