using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
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

    public IClrTypeName GetClrName() => new ClrTypeName(myAttr.GetClrName());

    public string GetAttributeShortName() => myAttr.AttributeType.DisplayName;

    public IDeclaredType GetAttributeType() => TypeFactory.CreateTypeByCLRName(myAttr.GetClrName(), myModule);

    private AttributeValue GetAttributeValue(Tuple<FSharpType, object> param) =>
      new AttributeValue(new ConstantValue(param.Item2,
        FSharpTypesUtil.GetType(param.Item1, EmptyList<ITypeParameter>.Instance, myModule)));

    public AttributeValue PositionParameter(int paramIndex) =>
      paramIndex >= 0 && paramIndex < myAttr.ConstructorArguments.Count
        ? GetAttributeValue(myAttr.ConstructorArguments[paramIndex])
        : AttributeValue.BAD_VALUE;

    public IEnumerable<AttributeValue> PositionParameters() => myAttr.ConstructorArguments.Select(GetAttributeValue);

    public AttributeValue NamedParameter(string name)
    {
      var param = myAttr.NamedArguments.FirstOrDefault(p => p.Item2 == name);
      return param != null
        ? new AttributeValue(new ConstantValue(param.Item4, type: null))
        : AttributeValue.BAD_VALUE;
    }

    public IEnumerable<Pair<string, AttributeValue>> NamedParameters()
    {
      return myAttr.NamedArguments.Select(a => new Pair<string, AttributeValue>(a.Item2,
        GetAttributeValue(new Tuple<FSharpType, object>(a.Item1, a.Item4))));
    }

    // todo: add property to FCS to get constructor symbol
    public IConstructor Constructor =>
      FSharpElementsUtil.GetDeclaredElement(
        myAttr.AttributeType.MembersFunctionsAndValues.FirstOrDefault(m => m.IsConstructor), myModule) as IConstructor;

    public int PositionParameterCount
    {
      get
      {
        try
        {
          return myAttr.ConstructorArguments.Count;
        }
        catch (Exception)
        {
          //This custom attribute has an argument that can not yet be converted using this API
          return 0;
        }
      }
    }

    public int NamedParameterCount => myAttr.NamedArguments.Count;

    public static IList<IAttributeInstance> GetAttributeInstances(IList<FSharpAttribute> attrs, IPsiModule psiModule) =>
      attrs.Select(a => (IAttributeInstance) new FSharpAttributeInstance(a, psiModule)).AsIList();
  }
}