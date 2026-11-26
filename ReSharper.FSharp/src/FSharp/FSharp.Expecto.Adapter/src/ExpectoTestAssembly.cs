using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Expecto.Tasks;
using JetBrains.ReSharper.TestRunner.Abstractions;
using JetBrains.ReSharper.TestRunner.Abstractions.Objects;

namespace JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter
{
  internal sealed class ExpectoTestAssembly
  {
    private static readonly ILogger ourLogger = Logger.GetLogger<ExpectoTestAssembly>();

    private readonly Assembly myTestAssembly;
    private readonly Assembly myExpectoAssembly;
    private readonly Type myImplType;

    private ExpectoTestAssembly(Assembly testAssembly, Assembly expectoAssembly)
    {
      myTestAssembly = testAssembly;
      myExpectoAssembly = expectoAssembly;
      myImplType = expectoAssembly.GetType("Expecto.Impl", true);
    }

    [CanBeNull]
    public static ExpectoTestAssembly TryLoad(string testAssemblyPath)
    {
      var testAssembly = Assembly.LoadFrom(testAssemblyPath);
      var expectoAssemblyName = testAssembly.GetReferencedAssemblies().FirstOrDefault(name => name.Name == "Expecto");
      if (expectoAssemblyName == null)
        return null;

      var expectoAssembly = Assembly.Load(expectoAssemblyName);
      return new ExpectoTestAssembly(testAssembly, expectoAssembly);
    }

    public List<ExpectoTestTask> GetTestCases()
    {
      var toTestCodeList = myExpectoAssembly.GetType("Expecto.TestModule", true).GetMethod("toTestCodeList");
      var getLocation = myImplType.GetMethod("getLocation");

      var testCases = new List<ExpectoTestTask>();
      foreach (var type in myTestAssembly.GetExportedTypes())
      {
        var tests = TryGetTests(type);
        if (tests == null)
          continue;

        var flatTests = (IEnumerable) toTestCodeList.Invoke(null, new[] { tests });
        foreach (var flatTest in flatTests)
        {
          var path = ((IEnumerable<string>) GetPropertyValue(flatTest, "name")).ToArray();
          var test = GetPropertyValue(flatTest, "test");
          var location = TryGetLocation(getLocation, test);
          var sourceFilePath = location == null ? null : (string) GetPropertyValue(location, "sourcePath");
          var line = location == null ? 0 : (int) GetPropertyValue(location, "lineNumber");

          var hasSourceFile = !string.IsNullOrEmpty(sourceFilePath);
          testCases.Add(ExpectoTestTask.ToServer(path, type.FullName, hasSourceFile ? sourceFilePath : null, line));
        }
      }

      return testCases;
    }

    public void Run(TestRunRequest request, ITestExecutionSink sink, CancellationToken cancellationToken)
    {
      var tests = GetTestsOfLoadableTypes();
      if (tests == null)
        return;

      var reporter = new ExpectoTestReporter(request.Selection.Cast<ExpectoTestTask>(), sink);
      var cliArguments = CreateCliArguments(request, reporter);
      var run = myExpectoAssembly.GetType("Expecto.Tests", true).GetMethod("runTestsWithCLIArgsAndCancel");

      var consoleOut = Console.Out;
      var consoleError = Console.Error;
      Console.SetOut(reporter.CreateOutputWriter(TestOutputType.STDOUT, consoleOut));
      Console.SetError(reporter.CreateOutputWriter(TestOutputType.STDERR, consoleError));
      try
      {
        run.Invoke(null, new object[] { cancellationToken, cliArguments, Array.Empty<string>(), tests });
      }
      finally
      {
        Console.SetOut(consoleOut);
        Console.SetError(consoleError);
      }

      if (!cancellationToken.IsCancellationRequested)
        reporter.ReportRunWithNoResults();
    }

    [CanBeNull]
    private object TryGetLocation(MethodInfo getLocation, object test)
    {
      try
      {
        return getLocation.Invoke(null, new[] { myTestAssembly, test });
      }
      catch (TargetInvocationException exception)
      {
        ourLogger.Warn(exception.InnerException, $"Failed to get the location of an Expecto test in {myTestAssembly.FullName}");
        return null;
      }
    }

    [CanBeNull]
    private object GetTestsOfLoadableTypes()
    {
      try
      {
        return GetTests(_ => true);
      }
      catch (TargetInvocationException)
      {
        var loadableTypes = new HashSet<Type>(myTestAssembly.GetExportedTypes().Where(type => TryGetTests(type) != null));
        return GetTests(loadableTypes.Contains);
      }
    }

    [CanBeNull]
    private object TryGetTests(Type type)
    {
      try
      {
        return GetTests(type.Equals);
      }
      catch (TargetInvocationException exception)
      {
        ourLogger.Warn(exception.InnerException, $"Failed to get the Expecto tests of {type.FullName}");
        return null;
      }
    }

    [CanBeNull]
    private object GetTests(Converter<Type, bool> typeFilter)
    {
      var testFromAssemblyWithFilter = myImplType.GetMethod("testFromAssemblyWithFilter");
      var typeFilterType = testFromAssemblyWithFilter.GetParameters()[0].ParameterType;
      var fsharpTypeFilter = FSharpFunctionFactory.FromConverter(typeFilterType, typeFilter);

      var tests = testFromAssemblyWithFilter.Invoke(null, new[] { fsharpTypeFilter, myTestAssembly });
      return tests == null ? null : GetPropertyValue(tests, "Value");
    }

    private Array CreateCliArguments(TestRunRequest request, ExpectoTestReporter reporter)
    {
      var cliArgumentsType = myExpectoAssembly.GetType("Expecto.Tests+CLIArguments", true);

      var newRun = cliArgumentsType.GetMethod("NewRun");
      var namesType = newRun.GetParameters()[0].ParameterType;
      var listModule = namesType.Assembly.GetType("Microsoft.FSharp.Collections.ListModule", true);
      var ofSeq = listModule.GetMethod("OfSeq").MakeGenericMethod(typeof(string));
      var names = ofSeq.Invoke(null, new object[] { reporter.TestNames });

      var printers = CreatePrinters(reporter);
      var cliArguments = new List<object>
      {
        cliArgumentsType.GetMethod("NewPrinter").Invoke(null, new[] { printers }),
        newRun.Invoke(null, new[] { names }),
        cliArgumentsType.GetMethod("NewColours").Invoke(null, new object[] { 0 }),
        cliArgumentsType.GetProperty("No_Spinner").GetValue(null)
      };
      cliArguments.AddRange(GetParallelizationArguments(cliArgumentsType, request));

      var typedCliArguments = Array.CreateInstance(cliArgumentsType, cliArguments.Count);
      Array.Copy(cliArguments.ToArray(), typedCliArguments, cliArguments.Count);
      return typedCliArguments;
    }

    private static IEnumerable<object> GetParallelizationArguments(Type cliArgumentsType, TestRunRequest request)
    {
      var sequenced = cliArgumentsType.GetProperty("Sequenced").GetValue(null);
      var parallel = cliArgumentsType.GetProperty("Parallel").GetValue(null);

      var expectoSettings = XDocument.Parse(request.RunSettings).Root.Element("Expecto");
      foreach (var setting in expectoSettings?.Elements() ?? Enumerable.Empty<XElement>())
      {
        switch (setting.Name.LocalName.ToLowerInvariant())
        {
          case "sequenced" when bool.TryParse(setting.Value, out var isSequenced):
            yield return isSequenced ? sequenced : parallel;
            break;

          case "parallel" when bool.TryParse(setting.Value, out var isParallel):
            yield return isParallel ? parallel : sequenced;
            break;

          case "parallel-workers" when int.TryParse(setting.Value, out var workers):
            yield return cliArgumentsType.GetMethod("NewParallel_Workers").Invoke(null, new object[] { workers });
            break;
        }
      }

      if (request.DisableParallelization == true)
        yield return sequenced;
    }

    private object CreatePrinters(ExpectoTestReporter reporter)
    {
      var callbacksByPrinterName = new Dictionary<string, Action<object[]>>
      {
        ["beforeEach"] = args => reporter.TestStarting((string) args[0]),
        ["info"] = args => reporter.RunnerMessage((string) args[0]),
        ["passed"] = args => reporter.TestFinished((string) args[0], TestOutcome.Success, string.Empty, (TimeSpan) args[1]),
        ["ignored"] = args => reporter.TestFinished((string) args[0], TestOutcome.Ignored, (string) args[1]),
        ["failed"] = args => reporter.TestFailed((string) args[0], [CreateFailure((string) args[1])], (TimeSpan) args[2]),
        ["exn"] = args => reporter.TestFailed((string) args[0], GetExceptionChain((Exception) args[1]), (TimeSpan) args[2])
      };

      var printersType = myExpectoAssembly.GetType("Expecto.Impl+TestPrinters", true);
      var silentPrinters = printersType.GetProperty("silent").GetValue(null);
      var constructor = printersType.GetConstructors().Single();
      var printers = constructor.GetParameters().Select(printer =>
      {
        if (!callbacksByPrinterName.TryGetValue(printer.Name, out var callback))
          return GetPropertyValue(silentPrinters, printer.Name);

        var completedAsync = GetCompletedAsync(printer.ParameterType);
        return FSharpFunctionFactory.Create(printer.ParameterType, callback, completedAsync);
      });

      return constructor.Invoke(printers.ToArray());
    }

    private static ExceptionInfo[] GetExceptionChain(Exception exception)
    {
      var exceptions = new List<ExceptionInfo>();
      for (var current = exception; current != null; current = current.InnerException)
        exceptions.Add(new ExceptionInfo(current));

      return exceptions.ToArray();
    }

    private static ExceptionInfo CreateFailure(string message)
    {
      var stackTraceStart = message.IndexOf("\n   at ", StringComparison.Ordinal);
      if (stackTraceStart < 0)
        return new ExceptionInfo(string.Empty, message.Trim(), string.Empty);

      var stackTrace = message.Substring(stackTraceStart + 1);
      return new ExceptionInfo(string.Empty, message.Substring(0, stackTraceStart).Trim(), stackTrace);
    }

    private static object GetCompletedAsync(Type printerType)
    {
      var fsharpCore = printerType.Assembly;
      var operators = fsharpCore.GetType("Microsoft.FSharp.Core.ExtraTopLevelOperators", true);
      var asyncBuilder = operators.GetProperty("DefaultAsyncBuilder").GetValue(null);
      return asyncBuilder.GetType().GetMethod("Zero").Invoke(asyncBuilder, null);
    }

    private static object GetPropertyValue(object instance, string name) =>
      instance.GetType().GetProperty(name).GetValue(instance);
  }
}
