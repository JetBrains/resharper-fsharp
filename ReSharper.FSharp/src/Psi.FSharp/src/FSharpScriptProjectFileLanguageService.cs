using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [ProjectFileType(typeof(FSharpScriptProjectFileType))]
  public class FSharpScriptProjectFileLanguageService : FSharpProjectFileLanguageService
  {
    public FSharpScriptProjectFileLanguageService(ProjectFileType projectFileType,
      FSharpCheckerService checkerService) : base(projectFileType, checkerService)
    {
    }
  }
}