using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrefixAppExpr
  {
    private readonly CachedPsiValue<IList<IArgument>> myParameterArguments = new FileCachedPsiValue<IList<IArgument>>();

    public FSharpSymbolReference Reference => InvokedReferenceExpression?.Reference;

    public IReferenceExpr InvokedReferenceExpression
    {
      get
      {
        var funExpr = (IPrefixAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IPrefixAppExpr appExpr)
        {
          funExpr = appExpr;
        }

        if (!(funExpr.FunctionExpression.IgnoreInnerParens() is IReferenceExpr referenceExpr))
        {
          return null;
        }

        return referenceExpr;
      }
    }

    public IFSharpIdentifier FSharpIdentifier => InvokedReferenceExpression?.Identifier;

    public IFSharpReferenceOwner SetName(string name) => this;

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public FSharpSymbolReference InvokedFunctionReference
    {
      get
      {
        var referenceExpr = InvokedReferenceExpression;
        if (referenceExpr == null)
          return null;

        var reference = referenceExpr.Reference;
        var fsSymbol = reference.GetFSharpSymbol();

        // todo: union cases, exceptions
        if (!(fsSymbol is FSharpMemberOrFunctionOrValue mfv))
          return null;

        var paramGroups = mfv.CurriedParameterGroups;
        return paramGroups.Count >= AppliedExpressions.Count ? reference : null;
      }
    }

    public IList<IFSharpExpression> AppliedExpressions
    {
      get
      {
        var args = new List<IFSharpExpression>();
        var funExpr = (IPrefixAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IPrefixAppExpr appExpr)
        {
          args.Add(funExpr.ArgumentExpression);
          funExpr = appExpr;
        }

        args.Add(funExpr.ArgumentExpression);
        args.Reverse();
        return args;
      }
    }

    public IList<IArgument> Arguments => ParameterArguments.Where(arg => arg != null).ToList();

    public IList<IArgument> ParameterArguments => myParameterArguments.GetValue(this,
      () => FSharpArgumentsOwnerUtil.CalculateParameterArguments(this, AppliedExpressions));

    public override IType Type()
    {
      var reference = InvokedFunctionReference;
      if (reference == null)
        return TypeFactory.CreateUnknownType(GetPsiModule());

      var mfv = (FSharpMemberOrFunctionOrValue) reference.GetFSharpSymbol();
      return !mfv.IsConstructor && mfv.CurriedParameterGroups.Count == Arguments.Count
        ? mfv.ReturnParameter.Type.MapType(reference.GetElement())
        : TypeFactory.CreateUnknownType(GetPsiModule());
    }
  }
}
