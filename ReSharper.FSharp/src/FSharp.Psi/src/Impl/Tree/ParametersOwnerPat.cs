using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ParametersOwnerPat
  {
    public override IEnumerable<IFSharpPattern> NestedPatterns =>
      Parameters.SelectMany(param => param.NestedPatterns);

    public FSharpSymbolReference Reference => ReferenceName?.Reference;
    
    public bool IsParameterDeclaration => this.IsParameterDeclaration();
  }
}
