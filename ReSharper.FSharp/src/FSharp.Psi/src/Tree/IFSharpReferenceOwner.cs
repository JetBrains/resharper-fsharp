using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpReferenceOwner : IFSharpTreeNode
  {
    FSharpSymbolReference Reference { get; }

    [CanBeNull] IFSharpIdentifier FSharpIdentifier { get; }

    [NotNull]
    IFSharpReferenceOwner SetName([NotNull] string name);
  }

  public interface IFSharpQualifiableReferenceOwner : IFSharpReferenceOwner
  {
    IList<string> Names { get; }
    FSharpSymbolReference QualifierReference { get; }
    bool IsQualified { get; }
    void SetQualifier([NotNull] IClrDeclaredElement declaredElement);

  }
}
