using JetBrains.Platform.ProjectModel.FSharp;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Common.CheckerService;

namespace JetBrains.ReSharper.Psi.FSharp
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