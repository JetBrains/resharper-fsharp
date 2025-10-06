using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferencePat : IMutableModifierOwner, IFSharpDeclaration, IAccessRightsOwner
  {
    bool IsLocal { get; }
    [CanBeNull] IBindingLikeDeclaration Binding { get; }
  }
}
