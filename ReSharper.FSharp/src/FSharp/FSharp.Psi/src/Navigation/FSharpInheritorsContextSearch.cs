using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Navigation;

[ShellFeaturePart]
public class FSharpInheritorsContextSearch : InheritorsContextSearch
{
  public override bool IsContextApplicable(IDataContext dataContext) =>
    FSharpContextNavigationUtil.SupportsNoHierarchyNavigation(dataContext, ReferencePreferenceKind);

  public override bool IsAvailable(IDataContext context) => false;
}
