using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class ParametersOwnerPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns =>
    Parameters.SelectMany(param => param.NestedPatterns);

  public FSharpSymbolReference Reference => ReferenceName.Reference;

  public IFSharpReferenceOwner SetName(string name) => FSharpImplUtil.SetName(this, name);
  public FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Pattern;

  // todo: refactor the F#-specific interface
  public IList<IArgument> Arguments => EmptyList<IArgument>.Instance;
  public IList<IArgument> ParameterArguments => EmptyList<IArgument>.Instance;
}
