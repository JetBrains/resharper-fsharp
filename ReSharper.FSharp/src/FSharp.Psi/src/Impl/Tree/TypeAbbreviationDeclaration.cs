using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeAbbreviationDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }
  }
}