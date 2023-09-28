﻿using System.Runtime.InteropServices;
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
