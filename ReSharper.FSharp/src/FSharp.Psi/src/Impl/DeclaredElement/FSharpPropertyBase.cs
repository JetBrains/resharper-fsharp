using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpPropertyBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IProperty
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpPropertyBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv)
      : base(declaration, mfv)
    {
      var property =
        mfv.IsModuleValueOrMember
          ? mfv.EnclosingEntity?.Value.MembersFunctionsAndValues.FirstOrDefault(
              m => m.IsProperty && m.DisplayName == mfv.DisplayName) ?? mfv
          : mfv;
      
      IsReadable = property.HasGetterMethod || property.IsPropertyGetterMethod ||
                   property.IsModuleValueOrMember && !property.IsMember;
      IsWritable = property.IsMutable || property.HasSetterMethod || property.IsPropertySetterMethod;
      ShortName = property.GetMemberCompiledName();
      var returnType = property.IsPropertySetterMethod
        ? property.CurriedParameterGroups[0][0].Type
        : property.ReturnParameter.Type;
      ReturnType = FSharpTypesUtil.GetType(returnType, declaration, Module) ??
                   TypeFactory.CreateUnknownType(Module);
    }

    public override string ShortName { get; }
    public override IType ReturnType { get; }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PROPERTY;
    }

    public IType Type => ReturnType;

    public string GetDefaultPropertyMetadataName()
    {
      return ShortName;
    }

    public IAccessor Getter => IsReadable ? new ImplicitAccessor(this, AccessorKind.GETTER) : null;
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;
    public bool IsReadable { get; }
    public bool IsWritable { get; }
    public bool IsAuto => false;
    public bool IsDefault => false;
  }
}