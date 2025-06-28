using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpIndexerProperty(IMemberSignatureOrDeclaration declaration)
    : FSharpPropertyWithExplicitAccessors(declaration)
  {
    public override bool IsReadable => HasPublicAccessor(AccessorKind.GETTER);
    public override bool IsWritable => HasPublicAccessor(AccessorKind.SETTER);
    public override bool IsDefault => true;

    public override AccessRights GetAccessRights()
    {
      if (GetDeclaration() is not { } decl)
        return AccessRights.NONE;
      
      var accessRights = decl.GetAccessRights();
      if (accessRights == AccessRights.PUBLIC)
        return IsReadable || IsWritable ? AccessRights.PUBLIC : AccessRights.INTERNAL;

      return accessRights;
    }

    public override IList<IParameter> Parameters => this.GetFunctionParameters(Mfv);

    private bool HasPublicAccessor(AccessorKind kind) =>
      GetDeclaration()?.AccessorDeclarationsEnumerable.TryGet(kind) is { } declaration &&
      declaration.GetAccessRights() == AccessRights.PUBLIC;

    public override bool Equals(object obj)
    {
      if (!base.Equals(obj))
        return false;

      if (obj is not FSharpIndexerProperty indexer)
        return false;

      return SignatureComparers.Strict.CompareWithoutName(GetSignature(IdSubstitution),
        indexer.GetSignature(indexer.IdSubstitution));
    }

    public override int GetHashCode() => ShortName.GetHashCode();
  }
}
