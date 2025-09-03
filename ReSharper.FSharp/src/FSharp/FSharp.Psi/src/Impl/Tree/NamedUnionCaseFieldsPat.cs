using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class NamedUnionCaseFieldsPat
{
  public override IEnumerable<IFSharpPattern> NestedPatterns =>
    FieldPatterns.SelectMany(param => param.NestedPatterns);
}
