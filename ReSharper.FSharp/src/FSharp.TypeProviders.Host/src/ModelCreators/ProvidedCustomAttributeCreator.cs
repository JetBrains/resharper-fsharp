using System;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedCustomAttributeCreator : ProvidedRdModelsCreatorBase<CustomAttributeData, RdCustomAttributeData>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedCustomAttributeCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdCustomAttributeData CreateRdModelInternal(CustomAttributeData providedModel,
      int typeProviderId)
    {
      var (namedArguments, exn1) =
        myTypeProvidersContext.Logger.CatchWithException(() => providedModel.NamedArguments?.Select(Convert).ToArray());

      var (constructorArguments, exn2) =
        myTypeProvidersContext.Logger.CatchWithException(() =>
          providedModel.ConstructorArguments.Select(Convert).ToArray());

      return new RdCustomAttributeData(
        providedModel.Constructor.DeclaringType?.FullName ?? "",
        new(namedArguments ?? Array.Empty<RdCustomAttributeNamedArgument>(), exn1?.Message),
        new(constructorArguments ?? Array.Empty<RdCustomAttributeTypedArgument>(), exn2?.Message));
    }

    private static RdCustomAttributeNamedArgument Convert(CustomAttributeNamedArgument argument) =>
      new(argument.MemberName, Convert(argument.TypedValue));

    private static RdCustomAttributeTypedArgument Convert(CustomAttributeTypedArgument argument) =>
      new(argument.ArgumentType.IsArray
        ? null
        : PrimitiveTypesBoxer.BoxToClientStaticArg(argument.Value));
  }
}
