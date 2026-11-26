using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using JetBrains.ReSharper.Plugins.FSharp.Expecto.Tasks;
using JetBrains.ReSharper.TestRunner.Abstractions;
using JetBrains.ReSharper.TestRunner.Abstractions.Objects;

namespace JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter
{
  internal sealed class ExpectoTestReporter
  {
    private readonly ITestExecutionSink mySink;
    private readonly Dictionary<string, RemoteTask> myTestsByFullName = new();
    private readonly List<RemoteTask> myTests = new();
    private readonly AsyncLocal<RemoteTask> myCurrentTest = new();
    private readonly StringBuilder myOutputBeforeFirstResult = new();
    private volatile bool myHasResults;

    public ExpectoTestReporter(IEnumerable<ExpectoTestTask> selection, ITestExecutionSink sink)
    {
      mySink = sink;

      foreach (var task in selection)
      {
        if (task.IsGroup)
          continue;

        myTestsByFullName[ExpectoTestTask.GetFullName(task.Path)] = task;
        myTests.Add(task);
      }
    }

    public IEnumerable<string> TestNames => myTestsByFullName.Keys;

    public void TestStarting(string name)
    {
      if (!myTestsByFullName.TryGetValue(name, out var test))
        return;

      myCurrentTest.Value = test;
      mySink.TestStarting(test);
    }

    public void TestFinished(string name, TestOutcome outcome, string message, TimeSpan? duration = null)
    {
      myCurrentTest.Value = null;

      if (myTestsByFullName.TryGetValue(name, out var test))
        TestFinished(test, outcome, message, duration);
    }

    public void TestFailed(string name, ExceptionInfo[] exceptions, TimeSpan duration)
    {
      myCurrentTest.Value = null;

      if (!myTestsByFullName.TryGetValue(name, out var test))
        return;

      mySink.TestException(test, exceptions);
      TestFinished(test, TestOutcome.Failed, exceptions[0].Message, duration);
    }

    public void RunnerMessage(string message) => AppendOutputBeforeFirstResult(message + Environment.NewLine);

    public TextWriter CreateOutputWriter(TestOutputType outputType, TextWriter consoleWriter) =>
      new OutputWriter(this, outputType, consoleWriter);

    public void ReportRunWithNoResults()
    {
      if (myHasResults)
        return;

      string message;
      lock (myOutputBeforeFirstResult)
        message = myOutputBeforeFirstResult.ToString().Trim();

      if (message.Length == 0)
        return;

      foreach (var test in myTests)
        mySink.TestFinished(test, TestOutcome.Failed, message);
    }

    private void TestFinished(RemoteTask test, TestOutcome outcome, string message, TimeSpan? duration)
    {
      myHasResults = true;
      mySink.TestFinished(test, outcome, message, duration);
    }

    private void ConsoleOutput(string text, TestOutputType outputType, TextWriter consoleWriter)
    {
      var test = myCurrentTest.Value;
      if (test != null)
      {
        mySink.TestOutput(test, text, outputType);
        return;
      }

      consoleWriter.Write(text);
      AppendOutputBeforeFirstResult(text);
    }

    private void AppendOutputBeforeFirstResult(string text)
    {
      if (myHasResults)
        return;

      lock (myOutputBeforeFirstResult)
        myOutputBeforeFirstResult.Append(text);
    }

    private sealed class OutputWriter : TextWriter
    {
      private readonly ExpectoTestReporter myReporter;
      private readonly TestOutputType myOutputType;
      private readonly TextWriter myConsoleWriter;

      public OutputWriter(ExpectoTestReporter reporter, TestOutputType outputType, TextWriter consoleWriter)
      {
        myReporter = reporter;
        myOutputType = outputType;
        myConsoleWriter = consoleWriter;
      }

      public override Encoding Encoding => myConsoleWriter.Encoding;

      public override void Write(char value) => Write(value.ToString());
      public override void Write(string value) => myReporter.ConsoleOutput(value, myOutputType, myConsoleWriter);
      public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));
    }
  }
}
