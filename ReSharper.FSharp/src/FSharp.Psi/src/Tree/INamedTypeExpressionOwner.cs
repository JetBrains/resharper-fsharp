using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface INameIdentifierOwner : IFSharpTypeMemberDeclaration
  {
    [CanBeNull] IFSharpIdentifier NameIdentifier { get; }
  }
}
