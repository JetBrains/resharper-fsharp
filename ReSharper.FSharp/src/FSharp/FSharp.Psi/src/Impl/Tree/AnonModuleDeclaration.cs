using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AnonModuleDeclaration
  {
    private string CalcImplicitName() =>
      GetSourceFile().GetLocation().NameWithoutExtension.Capitalize();

    protected override string DeclaredElementName => CalcImplicitName();
    public override string SourceName => CompiledName;

    public override IFSharpIdentifier NameIdentifier => null;

    public override TreeTextRange GetNameRange() => new(TreeOffset.Zero);
  }
}
