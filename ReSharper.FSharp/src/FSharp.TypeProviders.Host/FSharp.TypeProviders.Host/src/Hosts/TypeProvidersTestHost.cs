using System.Runtime.InteropServices;
using JetBrains.Core;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts
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

    private static string RuntimeVersion(Unit _) => PlatformUtil.CurrentFrameworkDescription;

    private string Dump(Unit _)
    {
      var tpCache = myTypeProvidersContext.TypeProvidersCache;
      return string.Join("\n\n",
        tpCache.Dump(),
        myTypeProvidersContext.ProvidedTypesCache.Dump(tpCache),
        myTypeProvidersContext.ProvidedAssembliesCache.Dump(tpCache),
        myTypeProvidersContext.ProvidedConstructorsCache.Dump(tpCache),
        myTypeProvidersContext.ProvidedMethodsCache.Dump(tpCache),
        myTypeProvidersContext.ProvidedPropertyCache.Dump(tpCache));
    }
  }
}
