namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    public override string DeclaredName => LongIdentifier.QualifiedName;
    public string ShortName => LongIdentifier.Name;
    public bool IsModule => true;

    public override TreeTextRange GetNameRange()
    {
      return LongIdentifier.GetNameRange();
    }

    public override void SetName(string name)
    {
    }
  }
}