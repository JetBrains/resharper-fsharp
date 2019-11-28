using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeTestExpr
  {
    public override IType Type() =>
      GetPsiModule().GetPredefinedType().Bool;
  }
}
