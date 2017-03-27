using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpUnionCaseDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public IAccessModifiers AccessModifiers => null;

    public IAccessModifiers SetAccessModifiers(IAccessModifiers param)
    {
      return null;
    }
  }
}