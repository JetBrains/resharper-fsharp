using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyWithExplicitAccessors : FSharpPropertyBase<IMemberSignatureOrDeclaration>,
    IFSharpProperty
  {
    public FSharpPropertyWithExplicitAccessors(IMemberSignatureOrDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsReadable => FSharpExplicitGetters.Any();
    public override bool IsWritable => FSharpExplicitSetters.Any();

    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();

    public bool HasExplicitAccessors =>
      GetDeclarations().Any(decl =>
        decl is IMemberDeclaration memberDecl && memberDecl.AccessorDeclarationsEnumerable.Any());

    public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitGetters => GetExplicitAccessors(AccessorKind.GETTER);
    public IEnumerable<IFSharpExplicitAccessor> FSharpExplicitSetters => GetExplicitAccessors(AccessorKind.SETTER);

    public IEnumerable<IFSharpExplicitAccessor> GetExplicitAccessors()
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is IMemberDeclaration member)
          foreach (var accessorDeclaration in member.AccessorDeclarationsEnumerable)
            if ((ITypeMemberDeclaration)accessorDeclaration is { DeclaredElement: IFSharpExplicitAccessor accessor })
              yield return accessor;
    }

    private IEnumerable<IFSharpExplicitAccessor> GetExplicitAccessors(AccessorKind kind) =>
      GetExplicitAccessors().Where(accessor => accessor.Kind == kind);
  }
}
