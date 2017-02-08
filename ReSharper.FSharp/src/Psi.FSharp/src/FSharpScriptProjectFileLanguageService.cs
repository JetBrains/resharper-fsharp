using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [ProjectFileType(typeof(FSharpScriptProjectFileType))]
  public class FSharpScriptProjectFileLanguageService : FSharpProjectFileLanguageService
  {
    public FSharpScriptProjectFileLanguageService(ProjectFileType projectFileType) : base(projectFileType)
    {
    }
  }
}