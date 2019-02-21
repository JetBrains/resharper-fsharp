using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FSharpAttributeInstance : IAttributeInstance
  {
    public readonly FSharpAttribute Attr;
    public readonly IPsiModule Module;

    public FSharpAttributeInstance(FSharpAttribute attr, IPsiModule module)
    {
      Attr = attr;
      Module = module;
    }

    public IClrTypeName GetClrName() => new ClrTypeName(Attr.GetClrName());

    public string GetAttributeShortName() => Attr.AttributeType.DisplayName;

    // todo: containing type type parameters?
    public IDeclaredType GetAttributeType() => TypeFactory.CreateTypeByCLRName(Attr.GetClrName(), Module);

    private AttributeValue GetAttributeValue(Tuple<FSharpType, object> param) =>
      new AttributeValue(new ConstantValue(param.Item2,
        FSharpTypesUtil.GetType(param.Item1, EmptyList<ITypeParameter>.Instance, Module)));

    public AttributeValue PositionParameter(int paramIndex) =>
      paramIndex >= 0 && paramIndex < Attr.ConstructorArguments.Count
        ? GetAttributeValue(Attr.ConstructorArguments[paramIndex])
        : AttributeValue.BAD_VALUE;

    public IEnumerable<AttributeValue> PositionParameters() => Attr.ConstructorArguments.Select(GetAttributeValue);

    public AttributeValue NamedParameter(string name)
    {
      var param = Attr.NamedArguments.FirstOrDefault(p => p.Item2 == name);
      return param != null
        ? new AttributeValue(new ConstantValue(param.Item4, type: null))
        : AttributeValue.BAD_VALUE;
    }

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters() =>
      Attr.NamedArguments.Select(a => new Pair<string, AttributeValue>(a.Item2,
        GetAttributeValue(new Tuple<FSharpType, object>(a.Item1, a.Item4))));

    // todo: add property to FCS to get constructor symbol
    public IConstructor Constructor =>
      FSharpElementsUtil.GetDeclaredElement(
        Attr.AttributeType.MembersFunctionsAndValues.FirstOrDefault(m => m.IsConstructor), Module) as IConstructor;

    public int PositionParameterCount
    {
      get
      {
        try
        {
          return Attr.ConstructorArguments.Count;
        }
        catch (Exception)
        {
          // Possible FCS exception: This custom attribute has an argument that can not yet be converted using this API.
          return 0;
        }
      }
    }

    public int NamedParameterCount => Attr.NamedArguments.Count;
  }

  public static class FSharpAttributeEx
  {
    public static IList<IAttributeInstance> ToAttributeInstances([NotNull] this IList<FSharpAttribute> fsAttributes,
      [NotNull] IPsiModule psiModule) =>
      fsAttributes.ConvertAll(a => (IAttributeInstance) new FSharpAttributeInstance(a, psiModule));
  }
}
