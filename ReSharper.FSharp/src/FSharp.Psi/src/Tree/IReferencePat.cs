using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferencePat : IMutableModifierOwner, IFSharpDeclaration
  {
    bool IsLocal { get; }
    [CanBeNull] IBindingLikeDeclaration Binding { get; }
  }
}
