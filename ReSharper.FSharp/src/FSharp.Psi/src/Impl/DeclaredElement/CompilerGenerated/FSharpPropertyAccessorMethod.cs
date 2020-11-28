using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpPropertyAccessorMethod : FSharpGeneratedMethodBase, IFSharpGeneratedAccessor
  {
    private readonly IAccessor myAccessor;

    public FSharpPropertyAccessorMethod(IProperty property, AccessorKind kind)
    {
      myAccessor = kind == AccessorKind.GETTER ? property.Getter : property.Setter;
    }

    public FSharpPropertyAccessorMethod(IAccessor accessor)
    {
      myAccessor = accessor;
    }

    public override string ShortName => myAccessor.ShortName;
    public override string SourceName => ShortName;
    protected override ITypeElement ContainingType => myAccessor.GetContainingType();
    public override AccessRights GetAccessRights() => myAccessor.GetAccessRights();
    public override IList<IParameter> Parameters => myAccessor.Parameters;
    public override IType ReturnType => myAccessor.ReturnType;
    public override bool IsStatic => myAccessor.IsStatic;
    public IClrDeclaredElement OriginElement => myAccessor.OwnerMember;
    public AccessorKind Kind => myAccessor.Kind;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpPropertyAccessorPointer(this);

    public override bool Equals(object obj) =>
      obj is FSharpPropertyAccessorMethod prop && myAccessor.Equals(prop.myAccessor);

    public override int GetHashCode() => OriginElement.GetHashCode();
    public override bool IsValid() => myAccessor.IsValid();
  }
}
