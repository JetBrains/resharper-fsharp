using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
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
    public override ConstantValue ConstantValue
    {
      get
      {
        if (ShortName != "+")
          return ConstantValue.NOT_COMPILE_TIME_CONSTANT;

        if (LeftArgument?.ConstantValue.AsString() is not { } leftString ||
            RightArgument?.ConstantValue.AsString() is not { } rightString)
          return ConstantValue.NOT_COMPILE_TIME_CONSTANT;

        return Operator.IsPredefinedFunctionRef("+")
          ? ConstantValue.String(leftString + rightString, GetPsiModule())
          : ConstantValue.NOT_COMPILE_TIME_CONSTANT;
      }
    }

    public IList<IArgument> Arguments => ParameterArguments.WhereNotNull().ToList();

    public IList<IArgument> ParameterArguments => myParameterArguments.GetValue(this,
      static expr => expr.CalculateParameterArguments());
  }
}
