namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    public override string DeclaredName => LongIdentifier.QualifiedName;

    public override string ShortName => !LongIdentifier.IdentifiersEnumerable.IsEmpty()
      ? LongIdentifier.Name
      : GetSourceFile().GetLocation().NameWithoutExtension;

    public bool IsModule => true;

    public override TreeTextRange GetNameRange()
    {
      return !LongIdentifier.IdentifiersEnumerable.IsEmpty()
        ? LongIdentifier.GetNameRange()
        : new TreeTextRange(TreeOffset.Zero);
    }

    public override void SetName(string name)
    {
    }
  }
}