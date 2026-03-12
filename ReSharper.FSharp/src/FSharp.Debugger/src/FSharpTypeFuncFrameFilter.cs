using JetBrains.Metadata.Reader.API;
using Mono.Debugging.Autofac;
using Mono.Debugging.Utils;
using Mono.Debugging.Win32;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpTypeFuncFrameFilter : IAdditionalDebuggerHiddenProvider
{
  public bool IsHidden(IMetadataMethod metadataMethod) =>
    metadataMethod.IsFSharpTypeFuncSpecialize();
}
