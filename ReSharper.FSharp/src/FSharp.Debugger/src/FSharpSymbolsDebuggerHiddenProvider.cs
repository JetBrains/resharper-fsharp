using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Util;
using Microsoft.FSharp.Core;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.CallStacks;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Steppers;
using Mono.Debugging.Utils;
using Mono.Debugging.Win32;
using Mono.Debugging.Win32.Frames;

namespace JetBrains.ReSharper.Plugins.FSharp.Debugger;

[DebuggerSessionComponent(typeof(CorDebuggerType))]
public class FSharpSymbolsDebuggerHiddenProvider(CorDebuggerSession session)
  : IFrameFilter, IAdditionalDebuggerHiddenProvider
{
  private static SourceConstructFlags GetSourceConstructFlag(IMetadataMethod metadataMethod)
  {
    var compilationMappingAttr =
      GetCompilationMappingAttr(metadataMethod) ??
      GetCompilationMappingAttr(metadataMethod.GetPropertyFromAccessor());

    if (compilationMappingAttr == null)
      return SourceConstructFlags.None;

    var attrValue = compilationMappingAttr.ConstructorArguments.FirstOrDefault();
    return attrValue.IsBadValue() ? SourceConstructFlags.None : (SourceConstructFlags)attrValue.Value;
  }

  private static IMetadataCustomAttribute GetCompilationMappingAttr([CanBeNull] IMetadataEntity metadataEntity) =>
    metadataEntity?.GetCustomAttributes("Microsoft.FSharp.Core.CompilationMappingAttribute").SingleItem();

  public bool IsHidden(IMetadataMethod metadataMethod)
  {
    var sourceConstructFlag = GetSourceConstructFlag(metadataMethod);
    return sourceConstructFlag is
      SourceConstructFlags.Field or
      SourceConstructFlags.UnionCase or
      SourceConstructFlags.Value;
  }

  public bool IsHidden(MethodSpecification methodSpec) =>
    IsHidden(methodSpec.Method);

  public bool SkipFrame(IStackFrame frame) =>
    frame is IStackFrame<ICorFrame> corStackFrame && IsHidden(session.GetMetadataMethod(corStackFrame.Frame));
}
