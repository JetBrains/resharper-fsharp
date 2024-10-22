using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class FieldPat
{
  public FSharpSymbolReference Reference => ReferenceName?.Reference;
  public string ShortName => ReferenceName?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
}
