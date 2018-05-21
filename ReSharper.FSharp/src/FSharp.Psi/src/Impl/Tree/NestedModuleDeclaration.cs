using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();
    public bool IsModule => true;
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();
  }
}