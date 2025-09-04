using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpTypeOrExtensionDeclaration
  {
    bool IsPrimary { get; }
    ITypeParameterDeclarationList TypeParameterDeclarationList { get; }
  }

  public partial interface IFSharpTypeDeclaration
  {
    [CanBeNull] ITypeInherit TypeInheritMember { get; }
    IEnumerable<IInheritMember> TypeOrInterfaceInheritMembers { get; }
  }
}
