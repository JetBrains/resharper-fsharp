using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrefixAppExpr
  {
    private readonly CachedPsiValue<IList<IArgument>> myParameterArguments = new FileCachedPsiValue<IList<IArgument>>();

    public bool IsHighPrecedence =>
      FunctionExpression is { } funExpr && 
      ArgumentExpression is { } argExpr && funExpr.NextSibling == argExpr;

    public bool IsIndexerLike =>
      IsHighPrecedence && ArgumentExpression is IListExpr;

    public IReferenceExpr InvokedReferenceExpression =>
      InvokedExpression.IgnoreInnerParens() as IReferenceExpr;

    public IFSharpExpression InvokedExpression
    {
      get
      {
        var funExpr = (IPrefixAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IPrefixAppExpr appExpr) 
          funExpr = appExpr;

        return funExpr.FunctionExpression;
      }
    }

    public IFSharpIdentifier FSharpIdentifier => InvokedReferenceExpression?.Identifier;

    public FSharpSymbolReference InvokedFunctionReference
    {
      get
      {
        var referenceExpr = InvokedReferenceExpression;
        if (referenceExpr == null)
          return null;

        var reference = referenceExpr.Reference;
        var fcsSymbol = reference.GetFcsSymbol();

        // todo: exceptions
        var isValid = fcsSymbol switch
        {
          FSharpMemberOrFunctionOrValue mfv => mfv.CurriedParameterGroups.Count >= AppliedExpressions.Count,
          FSharpUnionCase unionCase => unionCase.HasFields && AppliedExpressions.Count <= 1,
          _ => false
        };

        return isValid ? reference : null;
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
      static expr => expr.InvokedReferenceExpression is { } invokedReferenceExpression
        ? invokedReferenceExpression.CalculateParameterArguments(expr.AppliedExpressions)
        : EmptyList<IArgument>.InstanceList);
  }
}
