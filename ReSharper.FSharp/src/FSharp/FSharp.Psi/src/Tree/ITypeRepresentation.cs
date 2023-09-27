using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ITypeRepresentation
  {
    PartKind TypePartKind { get; }
    IFSharpTypeDeclaration TypeDeclaration { get; }
    IReadOnlyList<ITypeMemberDeclaration> GetMemberDeclarations();
  }
}
