using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class Attribute
  {
    private readonly CachedPsiValue<IList<IArgument>> myParameterArguments = new FileCachedPsiValue<IList<IArgument>>();

    protected override FSharpSymbolReference CreateReference() =>
      new CtorReference(this);

    public override IFSharpIdentifier FSharpIdentifier => ReferenceName?.Identifier;

    public IList<IArgument> ParameterArguments => myParameterArguments.GetValue(this, () =>
      this.CalculateParameterArguments(new[] {ArgExpression?.Expression}));

    public IList<IArgument> Arguments => ParameterArguments.WhereNotNull().ToList();
  }
}
