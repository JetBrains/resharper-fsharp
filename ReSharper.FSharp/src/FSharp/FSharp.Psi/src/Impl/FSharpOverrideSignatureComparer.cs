using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  internal class FSharpOverrideSignatureComparer : SignatureComparers.SignatureComparerBase
  {
    // todo: add tests for navigation/generation of overrides/implementations with out/in-ref params
    
    public static readonly FSharpOverrideSignatureComparer Instance = new();

    protected override StringComparer NameComparer => StringComparer.Ordinal;

    protected override int GetParameterCount(InvocableSignature signature) => signature.ParametersCount;

    protected override bool CompareParameterKind(ParameterKind kind1, ParameterKind kind2) =>
      !(kind1 == ParameterKind.VALUE ^ kind2 == ParameterKind.VALUE);

    protected override bool CompareParameterType(IType type1, IType type2) =>
      CLRTypeConversionUtil.IdentityConvertible(type1, type2);
  }
}
