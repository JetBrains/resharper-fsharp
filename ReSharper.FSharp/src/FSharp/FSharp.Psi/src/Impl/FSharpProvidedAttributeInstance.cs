using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  //TODO: support typeof args 
  internal class FSharpProvidedAttributeInstance : IAttributeInstance
  {
    private readonly RdCustomAttributeData myData;
    private readonly IPsiModule myModule;
    private readonly ClrTypeName myClrTypeName;
    private IConstructor myConstructor;

    public FSharpProvidedAttributeInstance(RdCustomAttributeData data, IPsiModule module)
    {
      myData = data;
      myModule = module;
      myClrTypeName = new ClrTypeName(myData.FullName);
    }

    public IClrTypeName GetClrName() => myClrTypeName;

    public string GetAttributeShortName() => myClrTypeName.ShortName;

    public IDeclaredType GetAttributeType() => TypeFactory.CreateTypeByCLRName(myData.FullName, myModule, true);

    public AttributeValue PositionParameter(int paramIndex) =>
      paramIndex < PositionParameterCount
        ? ConvertToAttributeValue(myData.ConstructorArguments.GetOrThrow()[paramIndex])
        : AttributeValue.BAD_VALUE;

    public IEnumerable<AttributeValue> PositionParameters() =>
      myData.ConstructorArguments.GetOrThrow().Select(ConvertToAttributeValue);

    public AttributeValue NamedParameter(string name) =>
      NamedParameters().SingleOrDefault(t => t.First == name).Second ?? AttributeValue.BAD_VALUE;

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters() => myData.NamedArguments
      .GetOrThrow()
      .Select(t => Pair.Of(t.MemberName, ConvertToAttributeValue(t.TypedValue)));

    public IConstructor Constructor => myConstructor ??=
      GetAttributeType().GetTypeElement()?.Constructors.FirstOrDefault(t =>
      {
        // TODO: check generic attributes
        var signature = t.GetSignature(EmptySubstitution.INSTANCE);
        if (signature.ParametersCount != PositionParameterCount) return false;

        for (var i = 0; i < PositionParameterCount; i++)
          if (TypeEqualityComparer.Default.Equals(signature.GetParameterType(i), PositionParameter(i).TypeValue))
            return false;

        return true;
      });

    public int PositionParameterCount => myData.ConstructorArguments.GetOrThrow().Length;
    public int NamedParameterCount => myData.NamedArguments.GetOrThrow().Length;

    private AttributeValue ConvertToAttributeValue(RdAttributeArg arg)
    {
      var elementType = TypeFactory.CreateTypeByCLRName(arg.TypeName, myModule, true);

      if (!arg.IsArray) return new AttributeValue(ConstantValue.Create(arg.Unbox(), elementType));

      var arrayType = TypeFactory.CreateArrayType(elementType, 1, NullableAnnotation.Unknown);
      return new AttributeValue(arrayType, arg.Values
        .Select(t =>
          new AttributeValue(ConstantValue.Create(t.Unbox(), TypeFactory.CreateTypeByCLRName(t.TypeName, myModule, true))))
        .ToArray());
    }
  }
}
