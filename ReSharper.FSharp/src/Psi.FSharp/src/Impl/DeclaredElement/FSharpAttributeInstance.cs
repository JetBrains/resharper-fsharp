using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  public class FSharpAttributeInstance : IAttributeInstance
  {
    private readonly FSharpAttribute myAttr;
    private readonly IPsiModule myModule;

    public FSharpAttributeInstance(FSharpAttribute attr, IPsiModule module)
    {
      myAttr = attr;
      myModule = module;
    }

    public IClrTypeName GetClrName()
    {
      return new ClrTypeName(myAttr.AttributeType.FullName);
    }

    public string GetAttributeShortName()
    {
      return myAttr.AttributeType.DisplayName;
    }

    public IDeclaredType GetAttributeType()
    {
      return TypeFactory.CreateTypeByCLRName(myAttr.AttributeType.FullName, myModule);
    }

    private AttributeValue GetAttributeValue(Tuple<FSharpType, object> param)
    {
      var type = FSharpTypesUtil.GetType(param.Item1, EmptyList<ITypeParameter>.Instance, myModule);
      return new AttributeValue(new ConstantValue(param.Item2, type));
    }

    public AttributeValue PositionParameter(int paramIndex)
    {
      return GetAttributeValue(myAttr.ConstructorArguments[paramIndex]);
    }

    public IEnumerable<AttributeValue> PositionParameters()
    {
      foreach (var attr in myAttr.ConstructorArguments)
        yield return GetAttributeValue(attr);
    }

    public AttributeValue NamedParameter(string name)
    {
      var param = myAttr.NamedArguments.FirstOrDefault(p => p.Item2.Equals(name, StringComparison.Ordinal));
      return param != null
        ? new AttributeValue(new ConstantValue(param.Item4, type: null))
        : AttributeValue.BAD_VALUE;
    }

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters()
    {
      return myAttr.NamedArguments.Select(a => new Pair<string, AttributeValue>(a.Item2,
        GetAttributeValue(new Tuple<FSharpType, object>(a.Item1, a.Item4))));
    }

    public IConstructor Constructor
    {
      get
      {
        // todo: add property to FCS to get constructor symbol
        var ctor = myAttr.AttributeType.MembersFunctionsAndValues.FirstOrDefault(m => m.IsConstructor);
        return FSharpElementsUtil.GetDeclaredElement(ctor, myModule) as IConstructor;
      }
    }

    public int PositionParameterCount => myAttr.ConstructorArguments.Count;
    public int NamedParameterCount => myAttr.NamedArguments.Count;
  }
}