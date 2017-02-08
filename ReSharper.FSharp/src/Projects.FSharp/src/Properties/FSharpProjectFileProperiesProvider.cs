using JetBrains.ProjectModel.Properties;

namespace JetBrains.Platform.ProjectModel.FSharp.Properties
{
  [ProjectModelExtension]
  public class FSharpProjectFilePropertiesProvider : ProjectFilePropertiesProviderBase
  {
    public override bool IsApplicable(IProjectProperties projectProperties)
    {
      return projectProperties is FSharpProjectProperties;
    }

    public override IProjectFileProperties CreateProjectFileProperties()
    {
      return new ProjectFileProperties();
    }
  }
}