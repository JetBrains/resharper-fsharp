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
    private readonly IOutOfProcessProtocolManager<ITypeProvider, RdTypeProvider> myTypeProvidersManager;
    private TypeProviderExternal myDispatcher;

    protected override TypeProviderExternal InitDispatcher(Lifetime lifetime, ILogger logger)
    {
      myDispatcher = new TypeProviderExternal(lifetime);
      return myDispatcher;
    }

    protected override void InitLogger(Lifetime lifetime, string path)
    {
      LogManager.Instance.SetConfig(new XmlLogConfigModel());
      var path1 = FileSystemPath.TryParse(path);
      if (path1.IsNullOrEmpty())
      {
        return;
      }

      var logEventListener = new FileLogEventListener(path1);
      LogManager.Instance.AddOmnipresentLogger(lifetime, logEventListener, LoggingLevel.TRACE);
      File.WriteAllText("tplog.txt", "initlogger lol");
    }

    protected override RdFSharpTypeProvidersLoaderModel InitModel(Lifetime lifetime, Rd.Impl.Protocol protocol)
    {
      File.WriteAllText("tplog.txt", "createmodel");
      var model = new RdFSharpTypeProvidersLoaderModel(lifetime, protocol);
      model.InstantiateTypeProvidersOfAssembly.Set(InstantiateTypeProvidersOfAssembly);
      return model;
    }

    private RdTask<RdTypeProvider[]> InstantiateTypeProvidersOfAssembly(Lifetime lifetime,
      InstantiateTypeProvidersOfAssemblyParameters @params)
    {
      var instantiateResults = myLoader.InstantiateTypeProvidersOfAssembly(@params)
        .Select(myTypeProvidersManager.Register)
        .ToArray();
      return RdTask<RdTypeProvider[]>.Successful(instantiateResults);
    }

    private RdTask<byte[]> GetGeneratedAssemblyContents(Lifetime arg1, GetGeneratedAssemblyContentsParameters arg2)
    {
      throw new NotImplementedException();
    }

    private RdTask<string[]> GetStaticParameters(Lifetime arg1, GetStaticArgumentsParameters arg2)
    {
      throw new NotImplementedException();
    }

    private RdTask<ParameterInfo[]> ApplyStaticArguments(Lifetime arg1, ApplyStaticArgumentsParameters arg2)
    {
      throw new NotImplementedException();
    }
    protected override void Run(Lifetime lifetime, TypeProviderExternal dispatcher)
    {
      while (true)
      {
      }
    }

    protected override string ProtocolName { get; } = "Out-of-Process Type Provider";

    public OutOfProcessTypeProvidersLoaderEndPoint(string parentProcessPidEnvVariable, 
      ITypeProvidersLoader loader,
      IOutOfProcessProtocolManager<ITypeProvider, RdTypeProvider> typeProvidersManager) :
      base(parentProcessPidEnvVariable)
    {
      myLoader = loader;
      myTypeProvidersManager = typeProvidersManager;
      Console.WriteLine("Endpoint created.");
    }

    //on shutdown requested
  }
}
