using Microsoft.FSharp.Core;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class DummyProvidedExpr : ProvidedExpr
  {
    private readonly ProvidedExprType myExprType;

    public DummyProvidedExpr(ProvidedType type, ProvidedTypeContext context)
      : base(FSharpExpr.Value(0), context)
    {
      myExprType = ProvidedExprType.NewProvidedConstantExpr(null, type);
      Type = type;
    }

    public override ProvidedType Type { get; }

    public override FSharpOption<ProvidedExprType> GetExprType() => myExprType;

    public override string UnderlyingExpressionString => "Fake provided expr";
  }
}
