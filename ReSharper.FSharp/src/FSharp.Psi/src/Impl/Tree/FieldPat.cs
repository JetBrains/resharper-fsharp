using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FieldPat
  {
    public FSharpSymbolReference Reference => ReferenceName?.Reference;
  }
}