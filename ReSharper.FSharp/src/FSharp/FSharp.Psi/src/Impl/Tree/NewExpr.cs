using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NewExpr
  {
    private readonly CachedPsiValue<IList<IArgument>> myParameterArguments = new FileCachedPsiValue<IList<IArgument>>();

    public override IFSharpIdentifier FSharpIdentifier => TypeName?.Identifier;

    protected override FSharpSymbolReference CreateReference() =>
      new CtorReference(this);

    public override IFSharpReferenceOwner SetName(string name) => this;
    public override FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Type;

    public IList<IArgument> ParameterArguments =>
      myParameterArguments.GetValue(this, expr => expr.CalculateParameterArguments(new[] { expr.ArgumentExpression }));

    public IList<IArgument> Arguments => ParameterArguments.WhereNotNull().ToList();
  }
}
