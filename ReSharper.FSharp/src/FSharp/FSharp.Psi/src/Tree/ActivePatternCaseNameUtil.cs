using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class ActivePatternCaseNameUtil
  {
    public static int GetIndex(this IActivePatternCaseName activePatternCaseName)
    {
      var activePatternId = ActivePatternIdNavigator.GetByCase(activePatternCaseName).NotNull();
      return activePatternId.Cases.IndexOf(activePatternCaseName);
    }
  }
}
