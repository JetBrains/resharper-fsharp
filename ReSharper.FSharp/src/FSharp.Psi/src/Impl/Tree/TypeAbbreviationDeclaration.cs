using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeAbbreviationDeclaration
  {
    public override string DeclaredName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }
  }
}