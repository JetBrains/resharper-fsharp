using System;
using System.IO;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Common;

namespace JetBrains.Platform.ProjectModel.FSharp.Properties
{
  [ProjectModelExtension]
  public class FSharpProjectPropertiesFactory : UnknownProjectPropertiesFactory
  {
    private static readonly Guid FSharpPropertyFactoryGuid = new Guid("{7B32A26D-3EC5-4A2A-B40C-EC79FF38A223}");
    public static readonly Guid FSharpProjectTypeGuid = new Guid("{F2A71F9B-5D33-465A-A702-920D77279786}");

    public override bool IsApplicable(ProjectPropertiesFactoryParameters parameters)
    {
      return parameters.ProjectTypeGuid == FSharpProjectTypeGuid;
    }

    public override bool IsKnownProjectTypeGuid(Guid projectTypeGuid)
    {
      return projectTypeGuid == FSharpProjectTypeGuid;
    }

    public override IProjectProperties CreateProjectProperties(ProjectPropertiesFactoryParameters parameters)
    {
      return new FSharpProjectProperties(parameters.ProjectTypeGuids, parameters.PlatformId, FSharpPropertyFactoryGuid,
        parameters.TargetFrameworkIds, parameters.TargetPlatformData);
    }

    public override Guid FactoryGuid => FSharpPropertyFactoryGuid;

    public override IProjectProperties Read(BinaryReader reader, ProjectSerializationIndex index)
    {
      var projectProperties = new FSharpProjectProperties(FSharpPropertyFactoryGuid);
      projectProperties.ReadProjectProperties(reader, index);
      return projectProperties;
    }
  }
}