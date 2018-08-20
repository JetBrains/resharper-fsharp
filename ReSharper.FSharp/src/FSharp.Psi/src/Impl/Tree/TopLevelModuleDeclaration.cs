using JetBrains.ReSharper.Plugins.FSharp.Common.Naming;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    private bool HasExplicitName => !LongIdentifier.IdentifiersEnumerable.IsEmpty();
    private string ImplicitName =>
      GetSourceFile().GetLocation().NameWithoutExtension.Capitalize();

    protected override FSharpName GetFSharpName() =>
      HasExplicitName
        ? LongIdentifier.GetModuleCompiledName(Attributes)
        : FSharpName.NewDeclaredName(ImplicitName);

    public override string SourceName =>
      HasExplicitName
        ? LongIdentifier.GetName()
        : ImplicitName;

    public override TreeTextRange GetNameRange() =>
      HasExplicitName
        ? LongIdentifier.GetNameRange()
        : new TreeTextRange(TreeOffset.Zero);

    public bool IsModule => true;
  }
}
