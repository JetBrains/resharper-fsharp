using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceName : IFSharpReferenceOwner, ITypeArgumentOwner
  {
    [NotNull] string ShortName { get; }
    [NotNull] string QualifiedName { get; }
    IList<string> Names { get; }
  }
}
