using JetBrains.Util;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class DummyProvidedExpr : ProvidedExpr
  {
    private readonly ProvidedExprType myExprType;

    public DummyProvidedExpr(ProvidedMethodInfo info) : base(FSharpExpr.Value(0), info.Context)
    {
      // Is unit of measure e.g. decimal<m>
      if (info.ReturnType is { IsGenericType: true } &&
          info.ReturnType.GetGenericTypeDefinition() is { IsGenericType: false } unitOfMeasureUnderlyingType)
      {
        Type = unitOfMeasureUnderlyingType;
        myExprType = ProvidedExprType.NewProvidedConstantExpr(null, unitOfMeasureUnderlyingType);
      }
      else
      {
        Type = info.ReturnType;
        myExprType = ProvidedExprType.NewProvidedCallExpr(null, info, EmptyArray<ProvidedExpr>.Instance);
      }
    }

    public DummyProvidedExpr(ProvidedConstructorInfo info) : base(FSharpExpr.Value(0), info.Context)
    {
      Type = info.DeclaringType;
      myExprType = ProvidedExprType.NewProvidedConstantExpr(null, info.DeclaringType);
    }

    public override ProvidedType Type { get; }

    public override FSharpOption<ProvidedExprType> GetExprType() => myExprType;

    public override string UnderlyingExpressionString => "Fake provided expr";
  }
}
