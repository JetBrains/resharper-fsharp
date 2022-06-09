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
  //TODO: support array and typeof args 
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

    public IDeclaredType GetAttributeType() => TypeFactory.CreateTypeByCLRName(myData.FullName, myModule);

    public AttributeValue PositionParameter(int paramIndex) =>
      paramIndex < PositionParameterCount
        ? ConvertToAttributeValue(myData.ConstructorArguments[paramIndex])
        : AttributeValue.BAD_VALUE;

    public IEnumerable<AttributeValue> PositionParameters() =>
      myData.ConstructorArguments.Select(ConvertToAttributeValue);

    public AttributeValue NamedParameter(string name) =>
      NamedParameters().SingleOrDefault(t => t.First == name).Second ?? AttributeValue.BAD_VALUE;

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters() => myData.NamedArguments
      .Select(t => new Pair<string, AttributeValue>(t.MemberName, ConvertToAttributeValue(t.TypedValue)));

    public IConstructor Constructor => myConstructor ??= 
      GetAttributeType().GetTypeElement()?.Constructors.FirstOrDefault(t =>
      {
        var signature = t.GetSignature(EmptySubstitution.INSTANCE);
        if (signature.ParametersCount != PositionParameterCount) return false;

        for (var i = 0; i < PositionParameterCount; i++)
          if (TypeEqualityComparer.Default.Equals(signature.GetParameterType(i), PositionParameter(i).TypeValue))
            return false;

        return true;
      });

    public int PositionParameterCount => myData.ConstructorArguments.Length;
    public int NamedParameterCount => myData.NamedArguments.Length;

    private AttributeValue ConvertToAttributeValue(RdCustomAttributeTypedArgument arg)
    {
      var value = arg.Value.Unbox(safeMode: true);
      var type = value is null ? null : TypeFactory.CreateTypeByCLRName(value.GetType().FullName!, myModule);
      return new AttributeValue(new ConstantValue(value, type));
    }
  }
}
