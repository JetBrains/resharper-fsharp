using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IActivePatternId
  {
    [CanBeNull] IActivePatternNamedCaseDeclaration GetCase(int index);
    TreeTextRange GetCasesRange();
  }
}
