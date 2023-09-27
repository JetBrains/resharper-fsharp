using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ActivePatternId
  {
    [CanBeNull] private volatile string myCachedName;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myCachedName = null;
    }

    public string Name
    {
      get
      {
        lock (this)
          return myCachedName ??= GetName();
      }
    }

    protected string GetName()
    {
      var cases = Cases;
      if (cases.IsEmpty)
        return SharedImplUtil.MISSING_DECLARATION_NAME;

      var sb = new StringBuilder("|");
      foreach (var activePatternIdCase in cases)
      {
        sb.Append(activePatternIdCase is IActivePatternCaseName caseName ? caseName.Identifier?.Name : "_");
        sb.Append("|");
      }

      return sb.ToString();
    }

    public ITokenNode IdentifierToken => null;
    public TreeTextRange NameRange => GetCasesRange();

    public IActivePatternNamedCaseDeclaration GetCase(int index)
    {
      var cases = Cases;
      return index >= 0 && index < cases.Count
        ? cases[index] as IActivePatternNamedCaseDeclaration
        : null;
    }

    public TreeTextRange GetCasesRange() =>
      Bars is { Count: > 0 } bars
        ? new TreeTextRange(bars.First().GetTreeStartOffset(), bars.Last().GetTreeEndOffset())
        : this.GetTreeTextRange();
  }
}
