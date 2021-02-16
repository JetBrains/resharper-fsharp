using System.Runtime.InteropServices;
using JetBrains.Core;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts
{
  internal class TypeProvidersTestHost : IOutOfProcessHost<RdTestHost>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public TypeProvidersTestHost(TypeProvidersContext typeProvidersContext) =>
      myTypeProvidersContext = typeProvidersContext;

    public void Initialize(RdTestHost processModel)
    {
      processModel.RuntimeVersion.Set(RuntimeVersion);
      processModel.Dump.Set(Dump);
    }

    private static string RuntimeVersion(Unit _) => RuntimeInformation.FrameworkDescription;

    private string Dump(Unit _) =>
      string.Join("\n\n",
        myTypeProvidersContext.TypeProvidersCache.Dump(),
        myTypeProvidersContext.ProvidedTypesCache.Dump(),
        myTypeProvidersContext.ProvidedAssembliesCache.Dump(),
        myTypeProvidersContext.ProvidedConstructorsCache.Dump(),
        myTypeProvidersContext.ProvidedMethodsCache.Dump(),
        myTypeProvidersContext.ProvidedPropertyCache.Dump());
  }
}
