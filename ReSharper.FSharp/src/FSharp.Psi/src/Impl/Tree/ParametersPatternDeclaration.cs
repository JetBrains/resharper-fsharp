using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParametersPatternDeclaration
  {
    public bool IgnoresIntermediateParens =>
      BindingNavigator.GetByParametersDeclaration(this) != null ||
      LambdaExprNavigator.GetByParametersDeclaration(this) != null;

    public IFSharpParameterDeclarationGroup ParameterDeclarationGroup =>
      (IFSharpParameterDeclarationGroup)DeclaredElement;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpPatternParameterGroup(this);

    public override IFSharpIdentifierLikeNode NameIdentifier => null;
    protected override string DeclaredElementName => null;

    public IList<IFSharpParameterDeclaration> ParameterDeclarations
    {
      get
      {
        var result = new List<IFSharpParameterDeclaration>();

        var pat = Pattern;
        if (pat is IParenPat parenPat)
        {
          var innerPat = IgnoresIntermediateParens ? parenPat.Pattern.IgnoreInnerParens() : parenPat.Pattern;
          if (innerPat is ITuplePat tuplePat)
            result.AddRange(tuplePat.PatternsEnumerable);
          else
            result.Add(innerPat);
        }
        else
        {
          result.Add(pat);
        }

        return result;
      }
    }
  }
}
