using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IModulePart : Class.IClassPart, IFSharpTypePart
  {
    bool IsAnonymous { get; }

    [CanBeNull] ITypeElement AssociatedTypeElement { get; }
    
    string[] ValueNames { get; }
    string[] FunctionNames { get; }
    string[] LiteralNames { get; }
    string[] ActivePatternNames { get; }
    string[] ActivePatternCaseNames { get; }
  }
}
