namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpTypeAbbreviationDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public override void SetName(string name)
    {
    }
  }
}