using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpIndexerLikeProperty(IMemberSignatureOrDeclaration declaration)
    : FSharpPropertyBase<IMemberSignatureOrDeclaration>(declaration), IFSharpProperty
  {
    public override bool IsReadable => GetIndexedAccessors(AccessorKind.GETTER).Any();
    public override bool IsWritable => GetIndexedAccessors(AccessorKind.SETTER).Any();

    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();

    public bool IsIndexerLike => true;
    public IEnumerable<IMethod> Accessors => IndexedAccessors;

    private IEnumerable<IFSharpIndexedAccessor> IndexedAccessors
    {
      get
      {
        foreach (var declaration in GetDeclarations())
          if (declaration is IMemberDeclaration member)
            foreach (var accessorDeclaration in member.AccessorDeclarationsEnumerable)
              if (accessorDeclaration 
                  is { IsIndexerLike: true } 
                  and ITypeMemberDeclaration { DeclaredElement: IFSharpIndexedAccessor accessor })
                yield return accessor;
      }
    }

    private IEnumerable<IFSharpIndexedAccessor> GetIndexedAccessors(AccessorKind kind) =>
      IndexedAccessors.Where(accessor => accessor.Kind == kind);
  }
}
