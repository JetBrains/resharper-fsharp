namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ValField
  {
    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);

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