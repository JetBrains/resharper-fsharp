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
    public FSharpSymbolReference Reference => InvokedReferenceExpression?.Reference;

    public IReferenceExpr InvokedReferenceExpression
    {
      get
      {
        // todo: can we init this once then cache it?
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

    public IList<ISynExpr> AppliedExpressions
    {
      get
      {
        var args = new List<ISynExpr>();
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

    // todo: this should be a Lazy/cache its result
    public IList<IArgument> Arguments
    {
      get
      {
        if (!(InvokedFunctionReference?.GetFSharpSymbol() is FSharpMemberOrFunctionOrValue mfv))
          return EmptyList<IArgument>.Instance;

        var paramGroups = mfv.CurriedParameterGroups;
        var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnit;

        if (isVoidReturn)
          return EmptyArray<IArgument>.Instance;

        return paramGroups
          .Zip(AppliedExpressions, (paramGroup, argExpr) => (paramGroup, argExpr.IgnoreInnerParens()))
          .SelectMany(pair =>
          {
            var (paramGroup, argExpr) = pair;

            switch (paramGroup.Count)
            {
              case 0:
                // e.g. F# extension methods with 0 parameters
                return EmptyList<IArgument>.Instance;
              case 1:
                return new[] {argExpr as IArgument};
              default:
                if (!(argExpr is ITupleExpr tupleExpr))
                  return new[] {argExpr as IArgument};

                return tupleExpr.Expressions.Count <= paramGroup.Count
                  ? tupleExpr.Expressions.Select(expr => expr as IArgument)
                  : EmptyList<IArgument>.Instance;
            }
          })
          .Where(argExpr => argExpr != null)
          .ToList();
      }
    }

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
