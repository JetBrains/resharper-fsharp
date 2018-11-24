using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    private bool HasExplicitName => !LongIdentifier.IdentifiersEnumerable.IsEmpty();
    private string ImplicitName => GetSourceFile().GetLocation().NameWithoutExtension.Capitalize();

    public override string DeclaredName =>
      HasExplicitName
        ? LongIdentifier.QualifiedName
        : ImplicitName;

    public override string ShortName =>
      HasExplicitName
        ? LongIdentifier.GetCompiledName(Attributes)
        : ImplicitName;

    public override string SourceName =>
      HasExplicitName
        ? LongIdentifier.GetSourceName()
        : ImplicitName;

    public override TreeTextRange GetNameRange() =>
      HasExplicitName
        ? LongIdentifier.GetNameRange()
        : new TreeTextRange(TreeOffset.Zero);

    public bool IsModule => true;
  }
}
