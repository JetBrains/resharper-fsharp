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
    OutOfProcessTypeProvidersLoaderEndPoint : ProtocolEndPoint<RdFSharpTypeProvidersLoaderModel, RdSimpleDispatcher>
  {
    private RdSimpleDispatcher myDispatcher;

    protected override string ProtocolName { get; } = "Out-of-Process Type Provider";

    public OutOfProcessTypeProvidersLoaderEndPoint(string parentProcessPidEnvVariable) :
      base(parentProcessPidEnvVariable)
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
      var unitOfWork = new UnitOfWork();
      new TypeProvidersLoaderHostFactory(unitOfWork).Initialize(model);
      new TypeProvidersHostFactory(unitOfWork).Initialize(model.RdTypeProviderProcessModel);
      new ProvidedNamespacesHostFactory(unitOfWork).Initialize(model.RdProvidedNamespaceProcessModel);
      new ProvidedTypesHostFactory(unitOfWork).Initialize(model.RdProvidedTypeProcessModel);
      new ProvidedPropertyInfoHostFactory(unitOfWork).Initialize(model.RdProvidedPropertyInfoProcessModel);
      new ProvidedMethodInfosHostFactory(unitOfWork).Initialize(model.RdProvidedMethodInfoProcessModel);
      new ProvidedParameterInfosHostFactory(unitOfWork).Initialize(model.RdProvidedParameterInfoProcessModel);
      new ProvidedAssemblyHostFactory(unitOfWork).Initialize(model.RdProvidedAssemblyProcessModel);
      new ProvidedFieldInfoHostFactory(unitOfWork).Initialize(model.RdProvidedFieldInfoProcessModel);
      new ProvidedEventInfoHostFactory(unitOfWork).Initialize(model.RdProvidedEventInfoProcessModel);
      new ProvidedConstructorInfosHostFactory(unitOfWork).Initialize(model.RdProvidedConstructorInfoProcessModel);
      new ProvidedExprHostFactory(unitOfWork).Initialize(model.RdProvidedExprProcessModel);
      new ProvidedVarsHostFactory(unitOfWork).Initialize(model.RdProvidedVarProcessModel);

      return model;
    }

    protected override void Run(Lifetime lifetime, RdSimpleDispatcher dispatcher)
    {
      dispatcher.Run();
    }

    //TODO: on shutdown requested
  }
}
