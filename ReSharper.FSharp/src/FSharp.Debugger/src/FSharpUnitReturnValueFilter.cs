using JetBrains.Debugger.CorApi.ComInterop;
using JetBrains.Debugger.CorApi.SharpGenUtil;
using Mono.Debugging.Autofac;
using Mono.Debugging.Win32;
using Mono.Debugging.Win32.Steppers;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpUnitReturnValueFilter(CorDebuggerSession session) : IReturnValueFilter
{
  public bool IsHidden(ICorDebugValue corDebugValue)
  {
    if (corDebugValue.QI<ICorDebugReferenceValue>() is not { } referenceValue || !referenceValue.IsNull())
      return false;

    var metadataType = referenceValue.GetExactType()?.GetMetadataType(session);
    return metadataType.IsUnit();
  }
}
