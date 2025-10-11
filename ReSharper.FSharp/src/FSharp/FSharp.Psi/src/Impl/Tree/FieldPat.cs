using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class FieldPat
{
  public FSharpSymbolReference Reference => ReferenceName.Reference;
  public string ShortName => ReferenceName?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

  public override IEnumerable<IFSharpPattern> NestedPatterns =>
    Pattern?.NestedPatterns ?? EmptyList<IFSharpPattern>.Instance;

  IFSharpReferenceOwner IFSharpReferenceOwner.SetName(string name) => this.SetName(name);
  public FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Pattern;
}
