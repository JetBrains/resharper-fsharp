using System;
using System.Linq;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils
{
  public interface IProvidedCustomAttributeProvider
  {
    RdCustomAttributeData[] GetCustomAttributes(IRdProvidedEntity entity);
    FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(RdCustomAttributeData[] data);
    string[] GetXmlDocAttributes(RdCustomAttributeData[] data);
    bool GetHasTypeProviderEditorHideMethodsAttribute(RdCustomAttributeData[] data);

    FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(RdCustomAttributeData[] data, string attribName);
  }

  public class ProvidedCustomAttributeProvider : IProvidedCustomAttributeProvider
  {
    private const string TypeProviderEditorHideMethodsAttributeFullName =
      "Microsoft.FSharp.Core.CompilerServices.TypeProviderEditorHideMethodsAttribute";

    private const string TypeProviderDefinitionLocationAttribute =
      "Microsoft.FSharp.Core.CompilerServices.TypeProviderDefinitionLocationAttribute";

    private const string TypeProviderXmlDocAttribute =
      "Microsoft.FSharp.Core.CompilerServices.TypeProviderXmlDocAttribute";

    private readonly TypeProvidersConnection myConnection;

    private RdTypeProviderProcessModel RdTypeProviderProcessModel =>
      myConnection.ProtocolModel.RdTypeProviderProcessModel;

    public ProvidedCustomAttributeProvider(TypeProvidersConnection connection)
    {
      myConnection = connection;
    }

    public RdCustomAttributeData[] GetCustomAttributes(IRdProvidedEntity entity) =>
      myConnection.ExecuteWithCatch(() =>
        RdTypeProviderProcessModel.GetCustomAttributes.Sync(
          new GetCustomAttributesArgs(entity.EntityId, entity.EntityType), RpcTimeouts.Maximal));

    public FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(RdCustomAttributeData[] data)
    {
      var attribute = data.FirstOrDefault(t => t.FullName == TypeProviderDefinitionLocationAttribute);
      if (attribute == null) return null;

      var filePath = TryGetNamedArgumentValue(attribute, "FilePath") switch
      {
        string s => s,
        _ => null,
      };

      var line = TryGetNamedArgumentValue(attribute, "Line") switch
      {
        int i => i,
        _ => 0,
      };

      var column = TryGetNamedArgumentValue(attribute, "Column") switch
      {
        int i => i,
        _ => 0,
      };

      return Tuple.Create(filePath, line, column);
    }

    public string[] GetXmlDocAttributes(RdCustomAttributeData[] data) =>
      data.Where(t => t.FullName == TypeProviderXmlDocAttribute && t.ConstructorArguments.Length == 1)
        .Select(t => t.ConstructorArguments[0] is { TypeName: "System.String" } x ? x.Values[0].Value : null)
        .Where(t => t != null)
        .ToArray();

    public bool GetHasTypeProviderEditorHideMethodsAttribute(RdCustomAttributeData[] data) =>
      data.Any(t => t.FullName == TypeProviderEditorHideMethodsAttributeFullName);

    //TODO: arrays
    public FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(RdCustomAttributeData[] data, string attribName)
    {
      var attribute = data.FirstOrDefault(t => t.FullName == attribName);
      if (attribute == null) return null;

      var constructorArgs =
        ListModule.OfSeq(
          attribute.ConstructorArguments.Select(t => Option(t.Values[0].Unbox())));

      var namedArgs =
        ListModule.OfSeq(attribute.NamedArguments.Select(t =>
          Tuple.Create(t.MemberName, Option(t.TypedValue.Values[0].Unbox()))));

      return Tuple.Create(constructorArgs, namedArgs);
    }

    private static FSharpOption<T> Option<T>(T value) where T : class =>
      value switch
      {
        null => null,
        { } obj => obj
      };

    private static object TryGetNamedArgumentValue(RdCustomAttributeData attribute, string namedAttributeName) =>
      attribute.NamedArguments
        .FirstOrDefault(t => t.MemberName == namedAttributeName)?.TypedValue.Values[0].Unbox();
  }
}
