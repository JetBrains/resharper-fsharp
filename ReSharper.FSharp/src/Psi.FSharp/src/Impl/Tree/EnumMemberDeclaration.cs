using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class EnumMemberDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    protected override IDeclaredElement CreateDeclaredElement()
    {
      return new FSharpEnumMember(this);
    }
  }
}