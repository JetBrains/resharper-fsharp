using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;

namespace JetBrains.Platform.ProjectModel.FSharp
{
  public class FSharpProjectLanguage : ProjectLanguage
  {
    public static readonly FSharpProjectLanguage Instance =
      new FSharpProjectLanguage("FSHARP", "F#", () => FSharpProjectFileType.Instance);

    public FSharpProjectLanguage([NotNull] string name, string presentableName,
      Func<ProjectFileType> projectFileType) : base(name, presentableName, projectFileType)
    {
    }
  }
}