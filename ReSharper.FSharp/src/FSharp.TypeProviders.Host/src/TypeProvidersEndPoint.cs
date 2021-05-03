using System.Runtime.InteropServices;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Platform.RdFramework.ExternalProcess.Util;
using JetBrains.Rd.Impl;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Hosts;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host
{
  public class
    TypeProvidersEndPoint : ProtocolEndPoint<RdFSharpTypeProvidersModel, RdSimpleDispatcher>
  {
    private RdSimpleDispatcher myDispatcher;

    protected override string ProtocolName => "Out-of-Process Type Providers Host";

    public TypeProvidersEndPoint() : base(TypeProvidersProtocolConstants.TypeProvidersHostPid)
    {
    }

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger)
    {
      myDispatcher = new RdSimpleDispatcher(lifetime, logger);
      return myDispatcher;
    }

    protected override void InitLogger(Lifetime lifetime, string path)
    {
      ProtocolEndPointUtil.InitLogger(path, lifetime, LoggingLevel.TRACE);
      Logger.Log(LoggingLevel.INFO, $"Process Runtime: {RuntimeInformation.FrameworkDescription}");
    }

    protected override RdFSharpTypeProvidersModel InitModel(Lifetime lifetime, Rd.Impl.Protocol protocol)
    {
      var model = new RdFSharpTypeProvidersModel(lifetime, protocol);
      var typeProvidersContext = new TypeProvidersContext(Logger);

      new TypeProvidersHost(typeProvidersContext).Initialize(model.RdTypeProviderProcessModel);
      new ProvidedTypesHost(typeProvidersContext).Initialize(model.RdProvidedTypeProcessModel);
      new ProvidedMethodInfosHost(typeProvidersContext).Initialize(model.RdProvidedMethodInfoProcessModel);
      new ProvidedAssemblyHost(typeProvidersContext).Initialize(model.RdProvidedAssemblyProcessModel);
      new ProvidedConstructorInfosHost(typeProvidersContext).Initialize(model.RdProvidedConstructorInfoProcessModel);
      new TypeProvidersTestHost(typeProvidersContext).Initialize(model.RdTestHost);

      return model;
    }

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher) => dispatcher.Run();
  }
}
