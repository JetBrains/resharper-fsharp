using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferencePat : IMutableModifierOwner
  {
    [CanBeNull] IBinding Binding { get; }
  }
}
