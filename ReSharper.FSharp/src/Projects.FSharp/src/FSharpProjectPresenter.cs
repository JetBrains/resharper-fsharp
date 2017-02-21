using JetBrains.Platform.ProjectModel.FSharp.Properties;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.Platform.ProjectModel.FSharp
{
  [ProjectModelElementPresenter(Priority)]
  public class FSharpProjectPresenter : IProjectModelElementPresenter
  {
    private const int Priority = 2; // needs to be greater than ProjectModelElementPresenter priority (which is 1)

    public IconId GetIcon(IProjectModelElement projectModelElement)
    {
      var project = projectModelElement as IProject;
      if (project?.ProjectProperties is FSharpProjectProperties)
        return ProjectModelThemedIcons.FsharpProject.Id;
      return null;
    }

    public string GetPresentableLocation(IProjectModelElement projectModelElement)
    {
      return null;
    }
  }
}