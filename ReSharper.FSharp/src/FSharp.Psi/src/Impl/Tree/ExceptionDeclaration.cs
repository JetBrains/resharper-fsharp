using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ExceptionDeclaration
  {
    protected override FSharpName GetFSharpName() => Identifier.GetFSharpName(Attributes);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    public override void SetName(string name) =>
      Identifier.ReplaceIdentifier(name);
  }
}
