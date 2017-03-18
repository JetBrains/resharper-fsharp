using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ImplicitConstructorDeclaration
  {
    // ReSharper disable once PossibleNullReferenceException
    public override string DeclaredName => GetContainingTypeDeclaration().DeclaredName;

    public override TreeTextRange GetNameRange()
    {
      // ReSharper disable once PossibleNullReferenceException
      return GetContainingTypeDeclaration().GetNameRange();
    }

    public override void SetName(string name)
    {
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      return new FSharpImplicitConstructor(this);
    }
  }
}