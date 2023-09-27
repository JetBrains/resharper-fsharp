using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IModulePart : Class.IClassPart, IFSharpTypePart
  {
    bool IsAnonymous { get; }
    ModuleMembersAccessKind AccessKind { get; }

    [CanBeNull] ITypeElement AssociatedTypeElement { get; }
  }
}
