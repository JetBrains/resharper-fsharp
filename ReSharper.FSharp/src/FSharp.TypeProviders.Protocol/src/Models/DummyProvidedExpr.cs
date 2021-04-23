using Microsoft.FSharp.Core;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class DummyProvidedExpr : ProvidedExpr
  {
    private readonly ProvidedExprType myExprType;

    public DummyProvidedExpr(ProvidedType type, ProvidedExprType exprType, ProvidedTypeContext context)
      : base(FSharpExpr.Value(0), context)
    {
      myExprType = exprType;
      Type = type;
    }

    public DummyProvidedExpr(ProvidedExprType exprType, ProvidedTypeContext context)
      : this(null, exprType, context)
    {
    }

    public override ProvidedType Type { get; }

    public override FSharpOption<ProvidedExprType> GetExprType() => myExprType;

    public override string UnderlyingExpressionString => "Fake provided expr";
  }
}
