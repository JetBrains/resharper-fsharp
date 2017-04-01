namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ValField
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      return null;
    }
  }
}