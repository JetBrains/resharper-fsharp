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

    /// Same-source-named type element defined in the same namespace group, forcing the module to have `Module` suffix.
    [CanBeNull] ITypeElement AssociatedTypeElement { get; }

    string QualifiedSourceName { get; }
  }
}
