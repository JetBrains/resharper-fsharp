using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpGeneratedPropertyAccessor : FSharpGeneratedMethodBase, IFSharpGeneratedAccessor
  {
    private readonly bool myIsGetter;
    private readonly IAccessor myAccessor;

    public FSharpGeneratedPropertyAccessor(IProperty property, bool isGetter)
    {
      myIsGetter = isGetter;
      myAccessor = isGetter ? property.Getter : property.Setter;
    }

    public FSharpGeneratedPropertyAccessor(IAccessor accessor)
    {
      myIsGetter = accessor.Kind == AccessorKind.GETTER;
      myAccessor = accessor;
    }

    public override string ShortName => myAccessor.ShortName;
    protected override ITypeElement ContainingType => myAccessor.GetContainingType();
    public override AccessRights GetAccessRights() => myAccessor.GetAccessRights();
    public override IList<IParameter> Parameters => myAccessor.Parameters;
    public override IType ReturnType => myAccessor.ReturnType;
    public IClrDeclaredElement OriginElement => myAccessor.OwnerMember;
    public override bool IsStatic => myAccessor.IsStatic;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpPropertyAccessorPointer(this, myIsGetter);

    public override bool Equals(object obj) =>
      obj is FSharpGeneratedPropertyAccessor prop &&
      prop.ShortName == ShortName && base.Equals(obj);

    public override int GetHashCode() => OriginElement.GetHashCode();

    public override bool IsValid() => myAccessor.IsValid();
  }
}
