using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.ModelCreators
{
  public class ProvidedMethodCreator : ProvidedRdModelsCreatorWithCacheBase<ProvidedMethodInfo, RdProvidedMethodInfo>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedMethodCreator(TypeProvidersContext typeProvidersContext) : base(typeProvidersContext
      .ProvidedMethodsCache) => myTypeProvidersContext = typeProvidersContext;

    protected override RdProvidedMethodInfo CreateRdModelInternal(ProvidedMethodInfo providedModel, int entityId,
      int typeProviderId)
    {
      var flags = RdProvidedMethodFlags.None;
      if (providedModel.IsGenericMethod) flags |= RdProvidedMethodFlags.IsGenericMethod;
      if (providedModel.IsStatic) flags |= RdProvidedMethodFlags.IsStatic;
      if (providedModel.IsFamily) flags |= RdProvidedMethodFlags.IsFamily;
      if (providedModel.IsFamilyAndAssembly) flags |= RdProvidedMethodFlags.IsFamilyAndAssembly;
      if (providedModel.IsFamilyOrAssembly) flags |= RdProvidedMethodFlags.IsFamilyOrAssembly;
      if (providedModel.IsVirtual) flags |= RdProvidedMethodFlags.IsVirtual;
      if (providedModel.IsFinal) flags |= RdProvidedMethodFlags.IsFinal;
      if (providedModel.IsPublic) flags |= RdProvidedMethodFlags.IsPublic;
      if (providedModel.IsAbstract) flags |= RdProvidedMethodFlags.IsAbstract;
      if (providedModel.IsHideBySig) flags |= RdProvidedMethodFlags.IsHideBySig;
      if (providedModel.IsConstructor) flags |= RdProvidedMethodFlags.IsConstructor;

      var returnTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.ReturnType, typeProviderId);

      var declaringTypeId =
        myTypeProvidersContext.ProvidedTypeRdModelsCreator.GetOrCreateId(providedModel.DeclaringType, typeProviderId);

      var genericArgs = providedModel.IsGenericMethod
        ? providedModel
          .GetGenericArguments()
          .CreateIds(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId)
        : null;

      return new RdProvidedMethodInfo(returnTypeId, providedModel.MetadataToken, declaringTypeId, flags, genericArgs,
        providedModel.Name, entityId);
    }
  }
}
