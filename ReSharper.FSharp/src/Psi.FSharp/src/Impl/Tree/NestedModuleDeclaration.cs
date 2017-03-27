namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    public override string DeclaredName => Identifier.GetName();
    public bool IsModule => true;

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }
  }
}