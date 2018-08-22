using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ClassDeclaration
  {
    protected override FSharpName GetFSharpName() => Identifier.GetFSharpName(Attributes);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();

    public FSharpPartKind TypePartKind => FSharpPartKind.Class;

    public override void SetName(string name) =>
      Identifier.ReplaceIdentifier(name);
  }
}
