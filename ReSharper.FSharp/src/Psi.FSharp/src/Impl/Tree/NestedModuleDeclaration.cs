namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);
    public bool IsModule => true;

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }
  }
}