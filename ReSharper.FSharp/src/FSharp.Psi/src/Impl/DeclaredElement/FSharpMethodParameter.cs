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
  public class FSharpMethodParameter : FSharpMethodParameterBase
  {
    public FSharpMethodParameter(FSharpParameter fsParam, [NotNull] IParametersOwner owner,
      int index, [NotNull] IType type) : base(owner, index, type) =>
      FSharpSymbol = fsParam;

    public FSharpParameter FSharpSymbol { get; }

    public override string ShortName =>
      FSharpSymbol.DisplayName is var name && name.IsEmpty()
        ? SharedImplUtil.MISSING_DECLARATION_NAME
        : name;

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      FSharpSymbol.Attributes.HasAttributeInstance(clrName.FullName);

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      FSharpSymbol.Attributes.ToAttributeInstances(Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName,
      AttributesSource attributesSource) =>
      FSharpSymbol.Attributes.GetAttributes(clrName.FullName).ToAttributeInstances(Module);

    public override DefaultValue GetDefaultValue()
    {
      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueAttr = FSharpSymbol.Attributes.FirstOrDefault(a =>
            a.GetClrName() == PredefinedType.DEFAULTPARAMETERVALUE_ATTRIBUTE_CLASS.FullName)
          ?.ConstructorArguments.FirstOrDefault();
        return defaultValueAttr == null
          ? new DefaultValue(Type, Type)
          : new DefaultValue(Type, new ConstantValue(defaultValueAttr.Item2, type: null));
      }
      // todo: change exception in FCS
      catch (Exception)
      {
        return DefaultValue.BAD_VALUE;
      }
    }

    public override ParameterKind Kind => FSharpSymbol.MapParameterKind();
    public override bool IsParameterArray => FSharpSymbol.IsParamArrayArg;

    // todo: implement IsCliOptional in FCS
    public override bool IsOptional =>
      FSharpSymbol.Attributes.HasAttributeInstance(PredefinedType.OPTIONAL_ATTRIBUTE_CLASS);
  }
}
