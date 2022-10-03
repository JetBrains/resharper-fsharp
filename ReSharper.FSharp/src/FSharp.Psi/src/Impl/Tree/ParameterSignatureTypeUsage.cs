using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParameterSignatureTypeUsage
  {
    public override string CompiledName => SourceName;
    public override IDeclaredElement DeclaredElement { get; }
    public override IFSharpIdentifierLikeNode NameIdentifier { get; }
  }
}
