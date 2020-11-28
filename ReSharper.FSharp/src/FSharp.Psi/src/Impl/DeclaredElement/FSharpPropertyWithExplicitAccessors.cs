using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyWithExplicitAccessors : FSharpPropertyBase<MemberDeclaration>, IFSharpProperty
  {
    public FSharpPropertyWithExplicitAccessors(IMemberDeclaration declaration) : base(declaration)
    {
      var accessors = declaration.AccessorDeclarations;
      IsReadable = accessors.TryGet(AccessorKind.GETTER) != null;
      IsWritable = accessors.TryGet(AccessorKind.SETTER) != null;
    }

    public override bool IsReadable { get; }
    public override bool IsWritable { get; }
    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();
    public override IList<IParameter> Parameters => this.GetParameters(Mfv);

    public IEnumerable<IFSharpExplicitAccessor> Getters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration is IMemberDeclaration member &&
              member.AccessorDeclarations.TryGet(AccessorKind.GETTER) is
                {DeclaredElement: IFSharpExplicitAccessor accessor} _)
            yield return accessor;
        }
      }
    }

    public IEnumerable<IFSharpExplicitAccessor> Setters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration is IMemberDeclaration member &&
              member.AccessorDeclarations.TryGet(AccessorKind.SETTER) is
                {DeclaredElement: IFSharpExplicitAccessor accessor} _)
            yield return accessor;
        }
      }
    }
  }
}
