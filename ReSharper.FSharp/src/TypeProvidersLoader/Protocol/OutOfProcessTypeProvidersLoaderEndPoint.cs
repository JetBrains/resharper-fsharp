using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.ExternalProcess;
using JetBrains.Rd.Impl;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Client;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class
    OutOfProcessTypeProvidersLoaderEndPoint : ProtocolEndPoint<RdFSharpTypeProvidersLoaderModel, RdSimpleDispatcher>
  {
    private readonly IUnitOfWork myUnitOfWork;
    private RdSimpleDispatcher myDispatcher;

    protected override string ProtocolName { get; } = "Out-of-Process Type Provider";

    public OutOfProcessTypeProvidersLoaderEndPoint(string parentProcessPidEnvVariable, IUnitOfWork unitOfWork) :
      base(parentProcessPidEnvVariable)
    {
      myUnitOfWork = unitOfWork;
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
      myUnitOfWork.TypeProvidersLoaderHostFactory.Initialize(model);
      myUnitOfWork.TypeProvidersHostFactory.Initialize(model.RdTypeProviderProcessModel);
      myUnitOfWork.ProvidedNamespacesHostFactory.Initialize(model.RdProvidedNamespaceProcessModel);
      myUnitOfWork.ProvidedTypesHostFactory.Initialize(model.RdProvidedTypeProcessModel);
      myUnitOfWork.ProvidedPropertyInfosHostFactory.Initialize(model.RdProvidedPropertyInfoProcessModel);
      myUnitOfWork.ProvidedMethodInfosHostFactory.Initialize(model.RdProvidedMethodInfoProcessModel);
      myUnitOfWork.ProvidedParameterInfosHostFactory.Initialize(model.RdProvidedParameterInfoProcessModel);
      myUnitOfWork.ProvidedAssemblyHostFactory.Initialize(model.RdProvidedAssemblyProcessModel);
      myUnitOfWork.ProvidedFieldInfosHostFactory.Initialize(model.RdProvidedFieldInfoProcessModel);
      myUnitOfWork.ProvidedEventInfosHostFactory.Initialize(model.RdProvidedEventInfoProcessModel);

      return model;
    }

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher)
    {
      dispatcher.Run();
    }

    //TODO: on shutdown requested
  }
}
