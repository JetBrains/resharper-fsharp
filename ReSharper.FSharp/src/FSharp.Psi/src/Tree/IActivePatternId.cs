using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IActivePatternId
  {
    [CanBeNull] IActivePatternCaseDeclaration GetCase(int index);
    TreeTextRange GetCasesRange();
  }
}
