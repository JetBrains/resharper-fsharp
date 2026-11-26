using System.Linq;
using System.Threading;
using JetBrains.ReSharper.TestRunner.Abstractions;
using JetBrains.ReSharper.TestRunner.Abstractions.Objects;

namespace JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter
{
  public class ExpectoRunner : LongLivedMarshalByRefObject, ITestDiscoverer, ITestExecutor
  {
    private readonly CancellationTokenSource myRunCancellation = new();

    public void DiscoverTests(TestDiscoveryRequest request, ITestDiscoverySink discoverySink)
    {
      var testAssembly = ExpectoTestAssembly.TryLoad(request.Container.Location);
      if (testAssembly == null)
        return;

      var testCases = testAssembly.GetTestCases();
      discoverySink.TestsDiscovered(testCases.ToArray<RemoteTask>());
    }

    public void AbortDiscovery()
    {
    }

    public void RunTests(TestRunRequest request, ITestDiscoverySink discoverySink, ITestExecutionSink executionSink)
    {
      var testAssembly = ExpectoTestAssembly.TryLoad(request.Container.Location);
      testAssembly?.Run(request, executionSink, myRunCancellation.Token);
    }

    public void AbortRun() => myRunCancellation.Cancel();
  }
}
