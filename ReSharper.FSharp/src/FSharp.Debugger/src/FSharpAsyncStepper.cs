using System;
using System.Collections.Generic;
using System.Linq;
using Debugger.Common;
using Debugger.Common.ManagedSymbols;
using Debugger.Common.Utils;
using JetBrains.Annotations;
using JetBrains.Debugger.CorApi.ComInterop;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Reader.API;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.DebuggerWorkerToHost;
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
public class FSharpAsyncStepper(CorDebuggerSession session, DebuggerWorkerToHostModel hostModel) :
  IAdditionalStepper<ICorThread, CorStepContext>,
  IBreakpointHandler
{
  private List<ICorDebugFunctionBreakpoint> myBreakpoints;

  private readonly MethodSequencePointEqualityComparer myMspComparer = new();

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

    return IsApplicableMethod(methodInfo);
  }

  private static bool IsApplicableMethod(MethodSpecification methodInfo)
  {
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
    var metadata = session.AppDomainsManager.GetOrCreateModuleInfo(module).Metadata?.MetadataAssembly?.Internals;
    if (metadata == null)
      return false;

    var moduleSymbols = session.GetModuleSymbols(module);
    var symbolsType = SymbolsSourceType.Internal | SymbolsSourceType.ExternalCache;
    var symbolMethod = moduleSymbols?.GetSymbolMethod(function, symbolsType);

    var currentSequencePoint = symbolMethod?.GetCurrentSequencePoint(stepContext.CurrentIlOffset);
    if (currentSequencePoint == null)
      return false;

    var sequencePoints = GetSortedSequencePoints(document, metadata).ToList();
    var resumeSequencePoints = new HashSet<(IManagedSymbolMethod m, IManagedSequencePoint sp)>(myMspComparer);

    var currentLine = currentSequencePoint.StartLine;
    var currentColumn = currentSequencePoint.StartColumn;
    if (FindSequencePoint(sequencePoints, currentLine, currentColumn + 1) is { } nextMsp)
      resumeSequencePoints.Add(nextMsp);

    var additionalResumeLocations = GetAdditionalResumeLocations(stepContext, currentSequencePoint);
    foreach (var (startLine, startCol) in additionalResumeLocations)
      if (FindSequencePoint(sequencePoints, startLine, startCol) is { } msp)
        resumeSequencePoints.Add(msp);

    var breakpoints = new List<ICorDebugFunctionBreakpoint>();
    foreach (var msp in resumeSequencePoints)
      if (SetBreakpoint(stepContext, msp) is { } breakpoint)
        breakpoints.Add(breakpoint);

    if (breakpoints.Count > 0)
    {
      myBreakpoints = breakpoints;
      session.ThreadsManager.ContinueWithTimeStamp();
      return true;
    }

    return false;
  }

  private static (IManagedSymbolMethod m, IManagedSequencePoint sp)? FindSequencePoint(
    List<(IManagedSymbolMethod m, IManagedSequencePoint sp)> sortedSequencePoints, int startLine, int startCol)
  {
    return sortedSequencePoints.FirstOrNull(msp =>
      msp.sp.StartLine > startLine || msp.sp.StartLine == startLine && msp.sp.StartColumn >= startCol);
  }

  [NotNull]
  private List<ResumeLocationCoords> GetAdditionalResumeLocations(CorStepContext stepContext,
    IManagedSequencePoint currentSequencePoint)
  {
    var location = new GetExpressionListForILRangeArg(
      currentSequencePoint.Document.Url.ResolveUserPathOrConvertUrl().Path,
      currentSequencePoint.StartLine, currentSequencePoint.StartColumn,
      currentSequencePoint.EndLine, currentSequencePoint.EndColumn);

    var lifetime = stepContext.Session.Lifetime;
    var getLocationsTask = hostModel.GetAdditionalResumeLocations.Start(lifetime, location).AsTask();
    if (getLocationsTask.Wait(TimeSpan.FromSeconds(1)))
      return getLocationsTask.Result;

    return [];
  }

  private ICorDebugFunctionBreakpoint SetBreakpoint(CorStepContext stepContext,
    (IManagedSymbolMethod m, IManagedSequencePoint sp) nextMethodSequencePoint)
  {
    var (resumeMethod, resumeSequencePoint) = nextMethodSequencePoint;
    if (resumeSequencePoint == null)
      return null;

    var resumeFunction = stepContext.Module?.GetFunctionFromToken(resumeMethod.Token);
    var corDebugCode = resumeFunction?.GetILCode();
    if (corDebugCode == null)
      return null;

    corDebugCode.CreateBreakpoint((uint)resumeSequencePoint.Offset, out var breakpoint).AssertSucceeded();
    breakpoint.Activate(true);
    return breakpoint;
  }

  private static IOrderedEnumerable<(IManagedSymbolMethod m, IManagedSequencePoint sp)> GetSortedSequencePoints(
    [NotNull] IManagedSymbolDocument document, [NotNull] IMetadataAssemblyInternals metadata)
  {
    var sequencePoints =
      document.Methods
        .Where(m => IsApplicableMethod(metadata.GetMethodFromToken(m.Token)))
        .SelectMany(m =>
        {
          var sequencePoints = m.SequencePoints.Where(sp => !sp.IsHidden).AsIReadOnlyList();

          // Workaround dotnet/fsharp#19255
          return sequencePoints
            .Where((sp, i) => IsInOrder(i, sequencePoints, sp) || !ContainsOtherSequencePoint(sequencePoints, sp))
            .Select(sp => (m, sp));
        });

    return sequencePoints.OrderBy(msp => msp.sp.StartLine).ThenBy(msp => msp.sp.StartColumn);

    bool IsInOrder(int i, IReadOnlyList<IManagedSequencePoint> managedSequencePoints, IManagedSequencePoint sp) =>
      i >= managedSequencePoints.Count - 1 || sp.StartLine <= managedSequencePoints[i + 1].EndLine;

    bool ContainsOtherSequencePoint(IReadOnlyList<IManagedSequencePoint> readOnlyList, IManagedSequencePoint sp) =>
      readOnlyList.Any(other =>
        sp.StartLine == other.StartLine && sp.EndLine == other.EndLine &&
        sp.StartColumn <= other.StartColumn && sp.EndColumn > other.EndColumn);
  }

  public HandleBreakpointResult TryHandleBreakpoint(ICorThread thread, ICorDebugBreakpoint breakpoint)
  {
    if (myBreakpoints == null || !myBreakpoints.Contains(breakpoint))
      return HandleBreakpointResult.NotApplicable;

    myBreakpoints.ForEach(b => b.Activate(false));
    myBreakpoints = null;

    session.ThreadsManager.SetActiveThread(thread);
    session.OnTargetEvent(new TargetEventArgs(TargetEventType.TargetStepCompleted));

    return HandleBreakpointResult.Handled;
  }

  public bool TryStepInto(CorStepContext stepContext) => false;
  public bool TryStepOut(CorStepContext stepContext) => false;

  private class MethodSequencePointEqualityComparer :
    IEqualityComparer<(IManagedSymbolMethod m, IManagedSequencePoint sp)>
  {
    public bool Equals((IManagedSymbolMethod m, IManagedSequencePoint sp) msp1,
      (IManagedSymbolMethod m, IManagedSequencePoint sp) msp2) =>
      msp1.sp.StartLine == msp2.sp.StartLine &&
      msp1.sp.StartColumn == msp2.sp.StartColumn;

    public int GetHashCode((IManagedSymbolMethod m, IManagedSequencePoint sp) msp) => msp.sp.StartLine;
  }
}
