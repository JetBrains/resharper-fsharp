using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  // ReSharper disable once PossibleInterfaceMemberAmbiguity
  public interface IFSharpTypeElementDeclaration : IFSharpDeclaration, ITypeDeclaration, ITypeMemberDeclaration
  {
    [CanBeNull]
    IDeclaredType BaseClassType { get; }
  }
}