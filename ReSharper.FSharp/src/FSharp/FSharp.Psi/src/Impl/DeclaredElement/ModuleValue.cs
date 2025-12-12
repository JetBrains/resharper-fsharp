using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ModuleValue(TopPatternDeclarationBase declaration)
    : FSharpMemberBase<TopPatternDeclarationBase>(declaration),
      IFSharpMutableModifierOwner,
      ITopLevelPatternDeclaredElement,
      IFSharpParameterOwnerMember, IProperty
  {
    public bool IsMutable =>
      GetDeclaration() is IFSharpMutableModifierOwner { IsMutable: true };

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is IFSharpMutableModifierOwner mutableModifierOwner)
          mutableModifierOwner.SetIsMutable(true);
    }

    public bool CanBeMutable =>
      GetDeclaration() is IFSharpMutableModifierOwner { CanBeMutable: true };

    public override IType ReturnType =>
      Mfv is { } mfv ? GetType(mfv.ReturnParameter.Type) : TypeFactory.CreateUnknownType(Module);

    public IType Type => ReturnType;

    public bool IsReadable => true;
    public bool IsWritable => IsMutable;
    public IAccessor Getter => new ImplicitAccessor(this, AccessorKind.GETTER);
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;

    public bool IsAuto => false;
    public bool IsDefault => false;
    public bool IsRequired => false;
    public override bool IsStatic => true;

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PROPERTY;
    public string GetDefaultPropertyMetadataName() => ShortName;

    public IList<IList<IFSharpParameter>> FSharpParameterGroups => this.GetFSharpParameterGroups();
    public IFSharpParameter GetParameter(FSharpParameterIndex index) => this.GetFSharpParameter(index);
    public InvocableSignature GetSignature(ISubstitution substitution) => new(this, substitution);

    public override bool Equals(object obj) =>
      obj is ModuleValue && base.Equals(obj);

    public override int GetHashCode() =>
      ShortName.GetHashCode();
  }
}
