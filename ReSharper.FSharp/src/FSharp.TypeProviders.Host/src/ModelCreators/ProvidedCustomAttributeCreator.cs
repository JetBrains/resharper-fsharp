using System;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedCustomAttributeCreator : ProvidedRdModelsCreatorBase<CustomAttributeData, RdCustomAttributeData>
  {
    protected override RdCustomAttributeData CreateRdModelInternal(CustomAttributeData providedModel,
      int typeProviderId) =>
      new(providedModel.Constructor.DeclaringType?.FullName ?? "",
        providedModel.NamedArguments?.Select(Convert).ToArray() ?? Array.Empty<RdCustomAttributeNamedArgument>(),
        providedModel.ConstructorArguments.Select(Convert).ToArray());

    private static RdCustomAttributeNamedArgument Convert(CustomAttributeNamedArgument argument) =>
      new(argument.MemberName, Convert(argument.TypedValue));

    private static RdCustomAttributeTypedArgument Convert(CustomAttributeTypedArgument argument) =>
      new(argument.ArgumentType.IsArray
        ? null
        : PrimitiveTypesBoxer.BoxToClientStaticArg(argument.Value, safeMode: true));
  }
}
