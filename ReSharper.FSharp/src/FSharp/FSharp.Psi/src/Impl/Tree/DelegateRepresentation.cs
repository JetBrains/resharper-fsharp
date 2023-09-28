using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class DelegateRepresentation
  {
    public FSharpEntity Delegate => TypeDeclaration.GetFcsSymbol() as FSharpEntity;
    public FSharpDelegateSignature DelegateSignature => Delegate?.FSharpDelegateSignature;

    public IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations() =>
      EmptyList<ITypeMemberDeclaration>.Instance;

    public override PartKind TypePartKind => PartKind.Delegate;
  }
}
