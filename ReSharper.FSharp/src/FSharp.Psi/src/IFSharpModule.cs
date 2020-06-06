using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public enum ModuleMembersAccessKind : byte
  {
    Normal = 0,
    RequiresQualifiedAccess = 1,
    AutoOpen = 2,
  }

  public interface IFSharpModule : IFSharpTypeElement
  {
    bool IsAnonymous { get; }
    bool IsAutoOpen { get; }
    bool RequiresQualifiedAccess { get; }

    ModuleMembersAccessKind AccessKind { get; }

    [CanBeNull] ITypeElement AssociatedTypeElement { get; }
  }
}
