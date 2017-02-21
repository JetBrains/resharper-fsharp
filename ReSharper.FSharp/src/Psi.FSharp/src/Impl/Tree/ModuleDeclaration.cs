namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class ModuleDeclaration
  {
    public override string DeclaredName => LongIdentifier.QualifiedName;
    public string ShortName => LongIdentifier.ShortName;

    public override TreeTextRange GetNameRange()
    {
      return LongIdentifier.GetNameRange();
    }

    public override void SetName(string name)
    {
    }
  }
}