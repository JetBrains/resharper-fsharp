using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ActivePatternId : IFSharpIdentifier
  {

    public ITokenNode IdentifierToken => null;

    public string Name
    {
      get
      {
        var cases = Cases;
        if (cases.IsEmpty)
          return SharedImplUtil.MISSING_DECLARATION_NAME;

        var sb = new StringBuilder("|");
        foreach (var @case in cases)
        {
          sb.Append(@case is IActivePatternCaseDeclaration caseDeclaration1 ? caseDeclaration1.SourceName : "_");
          sb.Append("|");
        }
        return sb.ToString();
      }
    }

    public IActivePatternCaseDeclaration GetCase(int index)
    {
      var cases = Cases;
      return index >= 0 && index < cases.Count
        ? null
        : cases[index] as IActivePatternCaseDeclaration;
    }

    public IList<IActivePatternCaseDeclaration> NamedCases =>
      Cases.OfType<IActivePatternCaseDeclaration>().AsList();

    public TreeTextRange GetCasesRange()
    {
      var nameRange = this.GetTreeTextRange();
      var cases = NamedCases;
      if (cases.IsEmpty())
        return nameRange;

      var firstRange = cases[0].NameIdentifier.GetNameRange();
      var lastRange = cases.Last().NameIdentifier.GetNameRange();

      return firstRange.IsValid() && lastRange.IsValid()
        ? new TreeTextRange(firstRange.StartOffset, lastRange.EndOffset)
        : nameRange;
    }
  }
}
