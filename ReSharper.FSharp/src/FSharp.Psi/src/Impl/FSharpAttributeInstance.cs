using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

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

    private AttributeValue GetArgValue(Tuple<FSharpType, object> arg) =>
      new AttributeValue(new ConstantValue(arg.Item2, arg.Item1.MapType(EmptyList<ITypeParameter>.Instance, Module)));

    public AttributeValue PositionParameter(int paramIndex) =>
      paramIndex >= 0 && AttrConstructorArgs is var args && paramIndex < args.Count
        ? GetArgValue(args[paramIndex])
        : AttributeValue.BAD_VALUE;

    public IEnumerable<AttributeValue> PositionParameters() => AttrConstructorArgs.Select(GetArgValue);

    public AttributeValue NamedParameter(string name) =>
      AttrNamedArgs.FirstOrDefault(p => p.Item2 == name) is { } param
        ? new AttributeValue(new ConstantValue(param.Item4, type: null))
        : AttributeValue.BAD_VALUE;

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters() =>
      AttrNamedArgs.Select(a => Pair.Of(a.Item2, GetArgValue(Tuple.Create(a.Item1, a.Item4))));

    // todo: add property to FCS to get constructor symbol
    public IConstructor Constructor =>
      Attr.AttributeType.MembersFunctionsAndValues.FirstOrDefault(m => m.IsConstructor)
        .GetDeclaredElement(Module) as IConstructor;

    public IList<Tuple<FSharpType, object>> AttrConstructorArgs
    {
      get
      {
        try
        {
          return Attr.ConstructorArguments;
        }
        catch (Exception)
        {
          // Possible FCS exception: This custom attribute has an argument that can not yet be converted using this API.
          return EmptyList<Tuple<FSharpType, object>>.Instance;
        }
      }
    }

    public IList<Tuple<FSharpType, string, bool, object>> AttrNamedArgs
    {
      get
      {
        try
        {
          return Attr.NamedArguments;
        }
        catch (Exception)
        {
          // Possible FCS exception: This custom attribute has an argument that can not yet be converted using this API.
          return EmptyList<Tuple<FSharpType, string, bool, object>>.Instance;
        }
      }
    }

    public int PositionParameterCount => AttrConstructorArgs.Count;
    public int NamedParameterCount => AttrNamedArgs.Count;
  }

  public static class FSharpAttributeEx
  {
    public static IList<IAttributeInstance> ToAttributeInstances([NotNull] this IList<FSharpAttribute> fsAttributes,
      [NotNull] IPsiModule psiModule) =>
      fsAttributes.ConvertAll(a => (IAttributeInstance) new FSharpAttributeInstance(a, psiModule));
  }
}
