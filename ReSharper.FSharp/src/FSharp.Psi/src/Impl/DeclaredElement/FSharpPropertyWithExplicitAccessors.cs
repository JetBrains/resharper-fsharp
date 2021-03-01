using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyWithExplicitAccessors : FSharpPropertyBase<IMemberSignatureOrDeclaration>, IFSharpProperty
  {
    public FSharpPropertyWithExplicitAccessors(IMemberSignatureOrDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsReadable => Getters.Any();
    public override bool IsWritable => Setters.Any();
    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();

    public IEnumerable<IFSharpExplicitAccessor> Getters => GetAccessors(AccessorKind.GETTER);
    public IEnumerable<IFSharpExplicitAccessor> Setters => GetAccessors(AccessorKind.SETTER);

    private IEnumerable<IFSharpExplicitAccessor> GetAccessors(AccessorKind kind)
    {
      foreach (var declaration in GetDeclarations())
      {
        if (declaration is IMemberDeclaration member &&
            member.AccessorDeclarations.TryGet(kind) is { DeclaredElement: IFSharpExplicitAccessor accessor })
          yield return accessor;
      }
    }
  }
}
