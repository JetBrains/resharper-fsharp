using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class BinaryAppExpr
  {
    private readonly CachedPsiValue<IList<IArgument>> myParameterArguments = new();

    public string ShortName => Operator?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    // TODO: remove logic from FSharpRegexNodeProvider.evalOptionsArg
    // ReSharper disable once RedundantOverriddenMember
    public override ConstantValue ConstantValue =>
      base.ConstantValue;

    public IList<IArgument> Arguments => ParameterArguments.WhereNotNull().ToList();

    public IList<IArgument> ParameterArguments => myParameterArguments.GetValue(this,
      static expr => expr.CalculateParameterArguments());
  }
}
