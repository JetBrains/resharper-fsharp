using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IActivePatternId
  {
    [CanBeNull] IActivePatternCaseDeclaration GetCase(int index);
  }
}
