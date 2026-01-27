using JetBrains.Metadata.Reader.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.CallStacks;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Steppers;
using Mono.Debugging.Utils;
using Mono.Debugging.Win32;
using Mono.Debugging.Win32.Frames;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpTypeFuncFrameFilter(CorDebuggerSession session) : IFrameFilter, IAdditionalDebuggerHiddenProvider
{
  public bool SkipFrame(IStackFrame frame)
  {
    if (frame is not IStackFrame<ICorFrame> corStackFrame) return false;

    var method = session.GetMetadataMethod(corStackFrame.Frame);
    return method.IsFSharpTypeFuncSpecialize();
  }

  public bool IsHidden(MethodSpecification methodSpec) =>
    methodSpec.Method.IsFSharpTypeFuncSpecialize();
}
