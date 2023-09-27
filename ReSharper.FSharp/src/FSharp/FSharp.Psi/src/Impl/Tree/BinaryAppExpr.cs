using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class BinaryAppExpr
  {
    public string ShortName => Operator?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    // TODO: remove logic from FSharpRegexNodeProvider.evalOptionsArg
    // ReSharper disable once RedundantOverriddenMember
    public override ConstantValue ConstantValue =>
      base.ConstantValue;
  }
}
