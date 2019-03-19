using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    private bool HasExplicitName => !LongIdentifier.IdentifiersEnumerable.IsEmpty();
    private string ImplicitName => GetSourceFile().GetLocation().NameWithoutExtension.Capitalize();

    protected override string DeclaredElementName =>
      HasExplicitName
        ? LongIdentifier.GetModuleCompiledName(Attributes)
        : ImplicitName;

    public override string SourceName =>
      HasExplicitName
        ? LongIdentifier.GetSourceName()
        : ImplicitName;

    public override TreeTextRange GetNameRange() =>
      HasExplicitName
        ? LongIdentifier.GetNameRange()
        : new TreeTextRange(TreeOffset.Zero);

    public override IFSharpIdentifier NameIdentifier => LongIdentifier;

    public bool IsModule => true;
  }
}
