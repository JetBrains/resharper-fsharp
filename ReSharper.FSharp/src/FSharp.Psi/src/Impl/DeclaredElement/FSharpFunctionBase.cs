using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpFunctionBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IFSharpFunction
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpFunctionBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override IList<IParameter> Parameters
    {
      get
      {
        var mfv = Mfv;
        if (mfv == null)
          return EmptyList<IParameter>.Instance;

        var paramGroups = mfv.CurriedParameterGroups;
        var isFsExtension = mfv.IsExtensionMember;
        var isVoidReturn = paramGroups.Count == 1 && paramGroups[0].Count == 1 && paramGroups[0][0].Type.IsUnit;

        if (!isFsExtension && isVoidReturn)
          return EmptyArray<IParameter>.Instance;

        var paramsCount = GetElementsCount(paramGroups);
        if (paramsCount == 0)
          return EmptyList<IParameter>.Instance;

        var typeParameters = AllTypeParameters;
        var methodParams = new List<IParameter>(paramsCount);
        if (isFsExtension && mfv.IsInstanceMember)
        {
          var typeElement = mfv.ApparentEnclosingEntity.GetTypeElement(Module);

          var type =
            typeElement != null
              ? TypeFactory.CreateType(typeElement)
              : TypeFactory.CreateUnknownType(Module);

          methodParams.Add(new FSharpExtensionMemberParameter(this, type));
        }

        if (isVoidReturn)
          return methodParams;
        
        foreach (var paramsGroup in paramGroups)
        foreach (var param in paramsGroup)
          methodParams.Add(new FSharpMethodParameter(param, this, methodParams.Count,
            param.Type.MapType(typeParameters, Module, true)));

        return methodParams;
      }
    }

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    private static int GetElementsCount<T>([NotNull] IList<IList<T>> lists)
    {
      var count = 0;
      foreach (var list in lists)
        count += list.Count;
      return count;
    }

    public virtual IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public override IType ReturnType =>
      Mfv?.ReturnParameter.Type is { } returnType
        ? returnType.MapType(AllTypeParameters, Module, true, true) // todo: isFromMethod?
        : TypeFactory.CreateUnknownType(Module);

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!base.Equals(obj))
        return false;

      if (!(obj is IFSharpFunction fsFunction) || IsStatic != fsFunction.IsStatic) // RIDER-11321, RSRP-467025
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        fsFunction.GetSignature(fsFunction.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public bool IsPredefined => false;
    public bool IsIterator => false;

    public IAttributesSet ReturnTypeAttributes =>
      new FSharpAttributeSet(Mfv?.ReturnParameter.Attributes ?? EmptyList<FSharpAttribute>.Instance, Module);
  }
}
