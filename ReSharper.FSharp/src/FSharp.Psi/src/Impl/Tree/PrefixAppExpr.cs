using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class PrefixAppExpr
  {
    public FSharpSymbolReference InvokedFunctionReference
    {
      get
      {
        var argsCount = 0;
        var funExpr = (IPrefixAppExpr) this;
        while (funExpr.FunctionExpression.IgnoreInnerParens() is IPrefixAppExpr appExpr)
        {
          funExpr = appExpr;
          argsCount++;
        }

        if (!(funExpr.FunctionExpression.IgnoreInnerParens() is IReferenceExpr referenceExpr))
          return null;

        argsCount++;

        var reference = referenceExpr.Reference;
        var fsSymbol = reference.GetFSharpSymbol();

        // todo: union cases, exceptions
        if (!(fsSymbol is FSharpMemberOrFunctionOrValue mfv))
          return null;

        var paramGroups = mfv.CurriedParameterGroups;
        return paramGroups.Count >= argsCount ? reference : null;
      }
    }

    public IList<IExpression> Arguments
    {
      get
      {
        var args = new List<IExpression>();
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
