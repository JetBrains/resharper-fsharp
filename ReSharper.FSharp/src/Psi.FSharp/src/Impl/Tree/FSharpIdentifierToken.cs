using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken
  {
    public FSharpSymbol FSharpSymbol { get; set; }

    public FSharpIdentifierToken(NodeType nodeType, [NotNull] IBuffer buffer, TreeOffset startOffset,
      TreeOffset endOffset) : base(nodeType, buffer, startOffset, endOffset)
    {
    }

    public override ReferenceCollection GetFirstClassReferences()
    {
      if (FSharpSymbol != null)
        return new ReferenceCollection(new FSharpResolvedReference(this, FSharpSymbol));

      var fsFile = this.GetContainingFile() as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");
      return fsFile.ReferencesResolved
        ? ReferenceCollection.Empty
        : new ReferenceCollection(new FSharpUnresolvedReference(this));
    }
  }
}