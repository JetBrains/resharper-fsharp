using JetBrains.ReSharper.Plugins.FSharp.Common.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpTypeDeclaration : IFSharpTypeElementDeclaration
  {
    PartKind TypePartKind { get; }
  }
}