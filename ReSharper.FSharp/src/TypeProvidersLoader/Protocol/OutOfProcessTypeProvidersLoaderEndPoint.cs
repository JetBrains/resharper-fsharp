using System;
using System.IO;
using System.Linq;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Core.CompilerServices;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    OutOfProcessTypeProvidersLoaderEndPoint : ProtocolEndPoint<RdFSharpTypeProvidersLoaderModel, TypeProviderExternal>
  {
    private readonly ITypeProvidersLoader myLoader;
    private readonly IOutOfProcessProtocolHost<ITypeProvider, RdTypeProvider> myTypeProvidersHost;
    private TypeProviderExternal myDispatcher;

    protected override string ProtocolName { get; } = "Out-of-Process Type Provider";

    public OutOfProcessTypeProvidersLoaderEndPoint(string parentProcessPidEnvVariable,
      ITypeProvidersLoader loader) :
      base(parentProcessPidEnvVariable)
    {
      myLoader = loader;
    }

    protected override TypeProviderExternal InitDispatcher(Lifetime lifetime, ILogger logger)
    {
      myDispatcher = new TypeProviderExternal(lifetime);
      return myDispatcher;
    }

    protected override void InitLogger(Lifetime lifetime, string path)
    {
      LogManager.Instance.SetConfig(new XmlLogConfigModel());
      var logPath = FileSystemPath.TryParse(path);
      if (logPath.IsNullOrEmpty())
      {
        return;
      }

      var logEventListener = new FileLogEventListener(logPath);
      LogManager.Instance.AddOmnipresentLogger(lifetime, logEventListener, LoggingLevel.TRACE);
    }

    protected override RdFSharpTypeProvidersLoaderModel InitModel(Lifetime lifetime, Rd.Impl.Protocol protocol)
    {
      var model = new RdFSharpTypeProvidersLoaderModel(lifetime, protocol);
      model.InstantiateTypeProvidersOfAssembly.Set(InstantiateTypeProvidersOfAssembly);
      return model;
    }

    private RdTask<RdTypeProvider[]> InstantiateTypeProvidersOfAssembly(Lifetime lifetime,
      InstantiateTypeProvidersOfAssemblyParameters @params)
    {
      var instantiateResults = myLoader.InstantiateTypeProvidersOfAssembly(@params)
        .Select(t => myTypeProvidersHost.GetRdModel(t, t))
        .ToArray();
      return RdTask<RdTypeProvider[]>.Successful(instantiateResults);
    }

    protected override void Run(Lifetime lifetime, TypeProviderExternal dispatcher)
    {
      while (true)
      {
      }
    }

    //on shutdown requested
  }
}
