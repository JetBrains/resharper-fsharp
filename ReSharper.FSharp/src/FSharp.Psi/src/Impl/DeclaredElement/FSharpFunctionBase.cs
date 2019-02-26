using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFunctionBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IFunction
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpFunctionBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override IList<IParameter> Parameters
    {
      get
      {
        var mfv = Mfv;
        if (mfv == null)
          return EmptyList<IParameter>.Instance;

        var mfvCurriedParams = mfv.CurriedParameterGroups;
        if (mfvCurriedParams.Count == 1 && mfvCurriedParams[0].Count == 1 && mfvCurriedParams[0][0].Type.IsUnit)
          return EmptyArray<IParameter>.Instance;

        var paramsCount = GetElementsCount(mfvCurriedParams);
        if (paramsCount == 0)
          return EmptyList<IParameter>.Instance;

        var typeParameters = GetAllTypeParameters();
        var methodParams = new List<IParameter>(paramsCount);
        foreach (var paramsGroup in mfvCurriedParams)
        foreach (var param in paramsGroup)
          methodParams.Add(new FSharpMethodParameter(param, this, methodParams.Count,
            param.Type.MapType(typeParameters, Module, true)));

        return methodParams;
      }
    }

    private static int GetElementsCount<T>([NotNull] IList<IList<T>> lists)
    {
      var count = 0;
      foreach (var list in lists)
        count += list.Count;
      return count;
    }

    public virtual IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public override IType ReturnType =>
      Mfv?.ReturnParameter.Type is var returnType && returnType != null
        ? returnType.MapType(GetAllTypeParameters(), Module, true, true) // todo: isFromMethod?
        : TypeFactory.CreateUnknownType(Module);

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (!(obj is FSharpMemberBase<TDeclaration> member) || IsStatic != member.IsStatic) // RIDER-11321, RSRP-467025
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        member.GetSignature(member.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public bool IsPredefined => false;
    public bool IsIterator => false;

    public IAttributesSet ReturnTypeAttributes =>
      new FSharpAttributeSet(Attributes, Module);
  }
}
