using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeParameterOfMethodDeclaration
  {
    public override string DeclaredName => Identifier.GetCompiledName(Attributes);
    public override TreeTextRange GetNameRange() => Identifier?.GetTreeTextRange() ?? TreeTextRange.InvalidRange;
    public override IDeclaredElement DeclaredElement => null;
  }
}