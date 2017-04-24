namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class TopLevelModuleDeclaration
  {
    public override string DeclaredName => LongIdentifier.QualifiedName;

    public override string ShortName => !LongIdentifier.IdentifiersEnumerable.IsEmpty()
      ? FSharpImplUtil.GetCompiledName(LongIdentifier, Attributes)
      : GetSourceFile().GetLocation().NameWithoutExtension;

    public override string SourceName => !LongIdentifier.IdentifiersEnumerable.IsEmpty()
      ? FSharpImplUtil.GetSourceName(LongIdentifier)
      : GetSourceFile().GetLocation().NameWithoutExtension;

    public bool IsModule => true;

    public override TreeTextRange GetNameRange()
    {
      return !LongIdentifier.IdentifiersEnumerable.IsEmpty()
        ? LongIdentifier.GetNameRange()
        : new TreeTextRange(TreeOffset.Zero);
    }
  }
}