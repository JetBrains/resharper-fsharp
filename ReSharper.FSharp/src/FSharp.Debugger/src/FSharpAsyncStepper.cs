using System.Linq;
using Debugger.Common;
using Debugger.Common.Utils;
using JetBrains.Annotations;
using JetBrains.Debugger.CorApi.ComInterop;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Reader.API;
using JetBrains.UI.Interop;
using JetBrains.Util;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.Events;
using Mono.Debugging.Client.Steppers;
using Mono.Debugging.Win32;
using Mono.Debugging.Win32.Steppers;
using Mono.Debugging.Win32.Steppers.Impl;
using Mono.Debugging.Win32.Threads;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpAsyncStepper(CorDebuggerSession session) :
  IAdditionalStepper<ICorThread, CorStepContext>,
  IBreakpointHandler
{
  private ICorDebugFunctionBreakpoint myBreakpoint;

  public int Priority => int.MaxValue;

  private static bool IsFSharpAsync([NotNull] IMetadataTypeInfo typeInfo) =>
    typeInfo.GetFields().Any(field =>
      field is { Name: "builder@", Type.FullName: "Microsoft.FSharp.Control.FSharpAsyncBuilder" });

  private static bool IsFSharpTask([NotNull] IMetadataTypeInfo typeInfo) =>
    typeInfo.InterfaceImplementations.Any(impl =>
      impl.Interface.Type is { FullyQualifiedName: "Microsoft.FSharp.Core.CompilerServices.IResumableStateMachine`1" });

  private bool IsApplicable(CorStepContext stepContext)
  {
    if (stepContext.Language != Languages.FSharp)
      return false;

    var methodInfo = stepContext.Function?.GetMethodInfo(session);
    if (methodInfo == null)
      return false;

    var typeInfo = methodInfo.OwnerType?.Type;
    if (typeInfo == null)
      return false;

    if (!typeInfo.Name.Contains("@"))
      return false;

    return IsFSharpAsync(typeInfo) || IsFSharpTask(typeInfo);
  }

  public bool TryStepOver(CorStepContext stepContext)
  {
    if (!IsApplicable(stepContext))
      return false;

    var document = stepContext.Method?.SequencePoints.FirstOrDefault()?.Document;
    if (document == null)
      return false;

    if (stepContext.Function is not { } function)
      return false;

    var module = function.GetModule();
    var moduleSymbols = session.GetModuleSymbols(module);
    var symbolsType = SymbolsSourceType.Internal | SymbolsSourceType.ExternalCache;
    var symbolMethod = moduleSymbols?.GetSymbolMethod(function, symbolsType);

    var currentSequencePoint = symbolMethod?.GetCurrentSequencePoint(stepContext.CurrentIlOffset);
    if (currentSequencePoint == null)
      return false;

    var sequencePoints =
      document.Methods
        .SelectMany(m =>
        {
          var sequencePoints = m.SequencePoints.Where(sp => !sp.IsHidden).AsIReadOnlyList();

          // Workaround dotnet/fsharp#19255
          return sequencePoints
            .Where((sp, i) => i >= sequencePoints.Count - 1 || sp.StartLine <= sequencePoints[i + 1].EndLine)
            .Select(sp => (m, sp));
        });

    var sortedSequencePoints =
      sequencePoints
        .OrderBy(msp => msp.sp.StartLine)
        .ThenBy(msp => msp.sp.StartColumn);

    var nextMethodSequencePoint =
      sortedSequencePoints.FirstOrDefault(msp =>
        msp.sp.StartLine > currentSequencePoint.StartLine ||
        msp.sp.StartLine == currentSequencePoint.StartLine && msp.sp.StartColumn > currentSequencePoint.StartColumn);

    if (nextMethodSequencePoint.sp == null)
      return false;

    var (resumeMethod, resumeSequencePoint) = nextMethodSequencePoint;
    var resumeFunction = stepContext.Module?.GetFunctionFromToken(resumeMethod.Token);
    var corDebugCode = resumeFunction?.GetILCode();
    if (corDebugCode == null)
      return false;

    corDebugCode.CreateBreakpoint((uint)resumeSequencePoint.Offset, out myBreakpoint).AssertSucceeded();
    myBreakpoint.Activate(true);
    session.ThreadsManager.ContinueWithTimeStamp();

    return true;
  }

  public HandleBreakpointResult TryHandleBreakpoint(ICorThread thread, ICorDebugBreakpoint breakpoint)
  {
    if (breakpoint != myBreakpoint)
      return HandleBreakpointResult.NotApplicable;

    myBreakpoint.Activate(false);
    myBreakpoint = null;
    session.ThreadsManager.SetActiveThread(thread);
    session.OnTargetEvent(new TargetEventArgs(TargetEventType.TargetStepCompleted));

    return HandleBreakpointResult.Handled;
  }

  public bool TryStepInto(CorStepContext stepContext) => false;
  public bool TryStepOut(CorStepContext stepContext) => false;
}
