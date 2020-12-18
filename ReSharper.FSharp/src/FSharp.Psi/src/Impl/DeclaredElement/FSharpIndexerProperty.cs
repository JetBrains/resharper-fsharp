using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpIndexerProperty : FSharpPropertyWithExplicitAccessors
  {
    public FSharpIndexerProperty(IMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsReadable => HasPublicAccessor(AccessorKind.GETTER);
    public override bool IsWritable => HasPublicAccessor(AccessorKind.SETTER);
    public override bool IsDefault => true;
    public override AccessRights GetAccessRights() => RepresentationAccessRights;
    public override IList<IParameter> Parameters => this.GetParameters(Mfv);

    private bool HasPublicAccessor(AccessorKind kind) =>
      GetDeclaration()?.AccessorDeclarations.TryGet(kind) is { } declaration &&
      declaration.GetAccessRights() == AccessRights.PUBLIC;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is FSharpIndexerProperty indexer))
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        indexer.GetSignature(indexer.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
