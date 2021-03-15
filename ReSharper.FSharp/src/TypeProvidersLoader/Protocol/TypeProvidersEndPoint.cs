using System.Runtime.InteropServices;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd.Impl;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Hosts;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    TypeProvidersEndPoint : ProtocolEndPoint<RdFSharpTypeProvidersLoaderModel, RdSimpleDispatcher>
  {
    private RdSimpleDispatcher myDispatcher;

    protected override string ProtocolName => "Out-of-Process Type Providers Loader";

    public TypeProvidersEndPoint() : base(TypeProvidersProtocol.TypeProvidersProtocol
      .TypeProvidersLoaderPid)
    {
    }

    protected override RdSimpleDispatcher InitDispatcher(Lifetime lifetime, ILogger logger)
    {
      myDispatcher = new RdSimpleDispatcher(lifetime, logger);
      return myDispatcher;
    }

    protected override void InitLogger(Lifetime lifetime, string path)
    {
      LogManager.Instance.SetConfig(new XmlLogConfigModel());
      var logPath = FileSystemPath.TryParse(path);
      if (logPath.IsNullOrEmpty()) return;

      var logEventListener = new FileLogEventListener(logPath);
      LogManager.Instance.AddOmnipresentLogger(lifetime, logEventListener, LoggingLevel.TRACE);
      Logger.Log(LoggingLevel.INFO, $"Process Runtime: {RuntimeInformation.FrameworkDescription}");
    }

    protected override RdFSharpTypeProvidersLoaderModel InitModel(Lifetime lifetime, Rd.Impl.Protocol protocol)
    {
      var model = new RdFSharpTypeProvidersLoaderModel(lifetime, protocol);
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
