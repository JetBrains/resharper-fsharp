using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Common.CheckerService;
using JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase;

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