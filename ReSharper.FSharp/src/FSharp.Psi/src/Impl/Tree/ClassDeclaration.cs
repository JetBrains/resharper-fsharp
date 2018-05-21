using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ClassDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();
    public FSharpPartKind TypePartKind => FSharpPartKind.Class;
  }
}