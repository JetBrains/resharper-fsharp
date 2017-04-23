using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpInterfaceDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public FSharpPartKind TypePartKind => FSharpPartKind.Interface;
  }
}