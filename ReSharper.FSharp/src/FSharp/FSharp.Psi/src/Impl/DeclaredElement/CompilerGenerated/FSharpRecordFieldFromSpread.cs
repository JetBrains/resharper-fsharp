using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;

public class FSharpRecordFieldFromSpread([NotNull] ITypeElement containingType, [NotNull] IProperty origin,
  [NotNull] ISubstitution substitution)
  : FSharpGeneratedPropertyFromTypeBase(containingType), IFSharpRecordField, IFSharpGeneratedFromOtherElement
{
  public override string ShortName => origin.ShortName;
  public override IType Type => substitution.Apply(origin.Type);
  public override bool IsValid() => base.IsValid() && origin.IsValid();

  public override AccessRights GetAccessRights() => GetContainingType().GetRepresentationAccessRights();
  public AccessRights RepresentationAccessRights => GetContainingType().GetFSharpRepresentationAccessRights();

  public int Index => GetContainingType().GetRecordFields().ToList().IndexOf(this);

  public bool IsMutable => origin is IFSharpMutableModifierOwner { IsMutable: true };
  public void SetIsMutable(bool value) => throw new InvalidOperationException();
  public bool CanBeMutable => false;
  public IClrDeclaredElement OriginElement { get; } =
    origin is FSharpRecordFieldFromSpread fieldFromSpread ? fieldFromSpread.OriginElement : origin;
  public bool IsOwnField => false;

  public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
    new FSharpRecordFieldFromSpreadPointer(this);

  public override bool Equals(object obj) =>
    obj is FSharpRecordFieldFromSpread other &&
    Equals(OriginElement, other.OriginElement) &&
    Equals(GetContainingType(), other.GetContainingType());

  public override int GetHashCode() => OriginElement.GetHashCode();
}
