using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Navigation;

public static class FSharpContextNavigationUtil
{
  public static bool SupportsNoHierarchyNavigation(IDeclaredElement declaredElement)
  {
    return declaredElement is IFSharpModule or IFSharpRecordField or IFSharpUnionCase;
  }

  public static bool SupportsNoHierarchyNavigation(IDataContext dataContext, ReferencePreferenceKind kind) =>
    ContextNavigationUtil.GetCandidateElements(dataContext, kind, false).Any(SupportsNoHierarchyNavigation);
}
