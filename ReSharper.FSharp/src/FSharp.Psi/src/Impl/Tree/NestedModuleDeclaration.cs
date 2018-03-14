using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedModuleDeclaration
  {
    public override string DeclaredName => Identifier.GetCompiledName(Attributes);
    public override string SourceName => Identifier.GetSourceName();
    public bool IsModule => true;
    public override TreeTextRange GetNameRange() => Identifier.GetNameRange();
    public IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations { get; }
  }
}