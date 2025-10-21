using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpMethodParameter([NotNull] IFSharpParameterOwner owner, FSharpParameterIndex index)
    : FSharpMethodParameterBase(owner, index)
  {
    [CanBeNull]
    public FSharpParameter FcsParameter
    {
      get
      {
        if (Owner is not IFSharpMember { Mfv.CurriedParameterGroups: var fcsParamGroups })
          return null;

        var paramGroup = fcsParamGroups.ElementAtOrDefault(FSharpIndex.GroupIndex);
        if (paramGroup == null)
          return null;

        if (FSharpIndex.ParameterIndex is not { } paramIndex)
          return paramGroup.Count == 1 ? paramGroup[0] : null;

        return paramGroup.ElementAtOrDefault(paramIndex);
      }
    }

    public override IType Type =>
      Owner is IFSharpTypeParametersOwner fsTypeParamOwner && FcsParameter is { } fcsParam
        ? fcsParam.Type.MapType(fsTypeParamOwner.AllTypeParameters, Module, true)
        : TypeFactory.CreateUnknownType(Module);

    public override string ShortName =>
      FcsParameter?.DisplayName is { } name && !name.RemoveBackticks().IsEmpty()
        ? name
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public override string SourceName => ShortName; // todo: calc from decl

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      FcsParameter?.Attributes.HasAttributeInstance(clrName.FullName) ?? false;

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      FcsParameter?.Attributes.ToAttributeInstances(Module) ?? EmptyList<IAttributeInstance>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName,
      AttributesSource attributesSource) =>
      FcsParameter?.Attributes.GetAttributes(clrName.FullName).ToAttributeInstances(Module) ??
      EmptyList<IAttributeInstance>.Instance;

    public override DefaultValue GetDefaultValue()
    {
      bool IsDefaultParamValueAttr(FSharpAttribute a) =>
        a.GetClrNameFullName() == PredefinedType.DEFAULTPARAMETERVALUE_ATTRIBUTE_CLASS.FullName;

      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueFcsAttr = FcsParameter?.Attributes.FirstOrDefault(IsDefaultParamValueAttr);
        var defaultValue = defaultValueFcsAttr?.ConstructorArguments.FirstOrDefault();
        return defaultValue != null
          ? new DefaultValue(Type, ConstantValue.Create(defaultValue.Item2, Type))
          : new DefaultValue(Type, Type);
      }
      // todo: change exception in FCS
      catch (Exception)
      {
        return DefaultValue.BAD_VALUE;
      }
    }

    public override ParameterKind Kind => FcsParameter?.MapParameterKind() ?? ParameterKind.VALUE;
    public override bool IsParams => IsParameterArray;

    public override bool IsParameterArray =>
      Owner is IFSharpFunction && FcsParameter is { IsParamArrayArg: true };

    public override bool IsParameterCollection => false;

    // todo: implement IsCliOptional in FCS
    public override bool IsOptional =>
      FcsParameter?.Attributes.HasAttributeInstance(PredefinedType.OPTIONAL_ATTRIBUTE_CLASS) ?? false;
    
    public override bool Equals(object obj) =>
      obj is FSharpMethodParameter fsParam && 
      FSharpIndex == fsParam.FSharpIndex && Owner.Equals(fsParam.Owner);

    public override int GetHashCode() =>
      197 * Owner.GetHashCode() + 47 * FSharpIndex.GetHashCode();
  }
}
