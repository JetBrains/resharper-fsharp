using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionCaseDeclaration
  {
    public override string DeclaredName => FSharpImplUtil.GetCompiledName(Identifier, Attributes);
    public override string SourceName => FSharpImplUtil.GetSourceName(Identifier);
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();
    public IAccessModifiers AccessModifiers => null;
    public IAccessModifiers SetAccessModifiers(IAccessModifiers param) => null;
  }
}