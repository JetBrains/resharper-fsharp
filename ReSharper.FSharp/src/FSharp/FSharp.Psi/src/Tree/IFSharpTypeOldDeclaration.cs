using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpTypeOldDeclaration : IFSharpTypeElementDeclaration
  {
    PartKind TypePartKind { get; }
    IList<ITypeParameterDeclaration> TypeParameterDeclarations { get; }
  }
}
