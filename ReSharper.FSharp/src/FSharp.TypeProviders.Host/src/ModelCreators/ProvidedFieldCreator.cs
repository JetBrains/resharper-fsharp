using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class
    ProvidedFieldCreator : ProvidedRdModelsCreatorBase<ProvidedFieldInfo, RdProvidedFieldInfo>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedFieldCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedFieldInfo CreateRdModelInternal(ProvidedFieldInfo providedModel, int typeProviderId)
    {
      var flags = RdProvidedFieldFlags.None;
      if (providedModel.IsInitOnly) flags |= RdProvidedFieldFlags.IsInitOnly;
      if (providedModel.IsStatic) flags |= RdProvidedFieldFlags.IsStatic;
      if (providedModel.IsSpecialName) flags |= RdProvidedFieldFlags.IsSpecialName;
      if (providedModel.IsLiteral) flags |= RdProvidedFieldFlags.IsLiteral;
      if (providedModel.IsPublic) flags |= RdProvidedFieldFlags.IsPublic;
      if (providedModel.IsFamily) flags |= RdProvidedFieldFlags.IsFamily;
      if (providedModel.IsFamilyAndAssembly) flags |= RdProvidedFieldFlags.IsFamilyAndAssembly;
      if (providedModel.IsFamilyOrAssembly) flags |= RdProvidedFieldFlags.IsFamilyOrAssembly;
      if (providedModel.IsPrivate) flags |= RdProvidedFieldFlags.IsPrivate;

      var fieldTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.FieldType, typeProviderId);

      var declaringTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.DeclaringType, typeProviderId);

      var typeProvider = myTypeProvidersContext.TypeProvidersCache.Get(typeProviderId);
      var customAttributes = providedModel
        .GetCustomAttributes(typeProvider)
        .CreateRdModels(myTypeProvidersContext.ProvidedCustomAttributeRdModelsCreator, typeProviderId);

      var rawValue = myTypeProvidersContext.Logger.Catch(providedModel.GetRawConstantValue);

      return new RdProvidedFieldInfo(providedModel.Name, fieldTypeId, declaringTypeId,
        PrimitiveTypesBoxer.BoxToClientStaticArg(rawValue),
        flags, customAttributes);
    }
  }
}
