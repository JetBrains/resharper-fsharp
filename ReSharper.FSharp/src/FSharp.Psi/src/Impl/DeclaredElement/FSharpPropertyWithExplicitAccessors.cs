using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpPropertyWithExplicitAccessors : FSharpPropertyBase<MemberDeclaration>, IFSharpProperty
  {
    private readonly IAccessorDeclaration myGetter;
    private readonly IAccessorDeclaration mySetter;

    public FSharpPropertyWithExplicitAccessors(IMemberDeclaration declaration) : base(declaration)
    {
      var accessors = declaration.AccessorDeclarations;
      myGetter = accessors.TryGet(AccessorKind.GETTER);
      mySetter = accessors.TryGet(AccessorKind.SETTER);
    }

    public override bool IsReadable => myGetter != null;
    public override bool IsWritable => mySetter != null;
    public override IAccessor Getter => IsReadable ? myGetter.DeclaredElement as IAccessor : null;
    public override IAccessor Setter => IsWritable ? mySetter.DeclaredElement as IAccessor : null;
    public override AccessRights GetAccessRights() => AccessRights.PRIVATE;
    public AccessRights RepresentationAccessRights => base.GetAccessRights();
    public override IList<IParameter> Parameters => this.GetParameters(Mfv);

    public IEnumerable<IAccessor> Getters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration.DeclaredElement is IFSharpProperty {IsReadable: true} prop)
            yield return prop.Getter;
        }
      }
    }

    public IEnumerable<IAccessor> Setters
    {
      get
      {
        foreach (var declaration in GetDeclarations())
        {
          if (declaration.DeclaredElement is IFSharpProperty {IsWritable: true} prop)
            yield return prop.Setter;
        }
      }
    }
  }
}
