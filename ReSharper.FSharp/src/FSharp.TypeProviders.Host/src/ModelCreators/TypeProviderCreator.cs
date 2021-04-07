using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class TypeProviderCreator
  {
    private readonly TypeProvidersContext myTypeProvidersContext;
    private int myCurrentId;

    public TypeProviderCreator(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public RdTypeProvider CreateRdModel([NotNull] ITypeProvider providedModel, string envKey)
    {
      var id = CreateEntityKey(providedModel);
      var model = CreateRdModelInternal(providedModel, id);

      myTypeProvidersContext.TypeProvidersCache.Add(id, (providedModel, envKey));
      return model;
    }

    private static RdTypeProvider CreateRdModelInternal(ITypeProvider providedModel, int typeProviderId)
    {
      var typeProviderType = providedModel.GetType();

      var model = new RdTypeProvider(typeProviderId, typeProviderType.Name,
        typeProviderType.FullName ?? typeProviderType.Name);

      return model;
    }

    private int CreateEntityKey(ITypeProvider _) => ++myCurrentId;
  }
}
