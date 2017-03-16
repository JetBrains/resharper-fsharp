using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpEnumMemberDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public override void SetName(string name)
    {
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      return new FSharpEnumMember(this);
    }
  }
}