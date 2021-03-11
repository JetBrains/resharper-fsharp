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
    public FSharpSymbolReference Reference { get; private set; }

    protected override void PreInit()
    {
      base.PreInit();
      Reference = new CtorReference(this);
    }

    public IFSharpIdentifier FSharpIdentifier => TypeName?.Identifier;

    public IFSharpReferenceOwner SetName(string name) => this;

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public IList<IArgument> ParameterArguments => myParameterArguments.GetValue(this,
      () => this.CalculateParameterArguments(new[] {ArgumentExpression}));

    public IList<IArgument> Arguments => ParameterArguments.WhereNotNull().ToList();
  }
}
