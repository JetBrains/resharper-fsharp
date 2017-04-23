using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal abstract class FSharpPropertyBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IProperty
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpPropertyBase([NotNull] ITypeMemberDeclaration declaration, FSharpMemberOrFunctionOrValue mfv)
      : base(declaration, mfv)
    {
      var property =
        mfv != null && mfv.IsModuleValueOrMember
          ? mfv.EnclosingEntity.MembersFunctionsAndValues.FirstOrDefault(
            m => m.IsProperty && m.DisplayName == mfv.DisplayName) ?? mfv
          : mfv;

      if (property != null)
      {
        IsReadable = property.HasGetterMethod || property.IsPropertyGetterMethod;
        IsWritable = property.IsMutable || property.HasSetterMethod || property.IsPropertySetterMethod;
        ShortName = property.GetMemberCompiledName();
        var returnType = property.IsPropertySetterMethod
          ? property.CurriedParameterGroups[0][0].Type
          : property.ReturnParameter.Type;
        ReturnType = FSharpTypesUtil.GetType(returnType, declaration, Module) ??
                     TypeFactory.CreateUnknownType(Module);
      }
      else
      {
        IsReadable = true;
        IsWritable = false;
        ReturnType = TypeFactory.CreateUnknownType(Module);
        ShortName = declaration.DeclaredName;
      }
    }

    public override string ShortName { get; }
    public override IType ReturnType { get; }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PROPERTY;
    }

    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
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