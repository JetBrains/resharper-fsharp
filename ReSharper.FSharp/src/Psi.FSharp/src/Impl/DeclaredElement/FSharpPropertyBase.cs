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
        mfv != null && mfv.IsMember
          ? mfv.EnclosingEntity.MembersFunctionsAndValues.FirstOrDefault(
            m => m.IsProperty && m.DisplayName == mfv.DisplayName)
          : mfv;

      if (property == null)
      {
        IsReadable = true;
        IsWritable = false;
        ReturnType = TypeFactory.CreateUnknownType(Module);
        ShortName = declaration.DeclaredName;
        return;
      }

      IsReadable = property.IsModuleValueOrMember || property.HasGetterMethod;
      IsWritable = property.IsModuleValueOrMember ? property.IsMutable : property.HasSetterMethod;

      const string compiledNameAttrName = "Microsoft.FSharp.Core.CompiledNameAttribute";
      var compiledNameAttr = property.Attributes.FirstOrDefault(a => a.AttributeType.FullName == compiledNameAttrName);
      var compiledName = compiledNameAttr != null && !compiledNameAttr.ConstructorArguments.IsEmpty()
        ? compiledNameAttr.ConstructorArguments[0].Item2 as string
        : null;
      ShortName = compiledName ?? property.LogicalName;

      ReturnType = FSharpTypesUtil.GetType(property.ReturnParameter.Type, declaration, Module) ??
                   TypeFactory.CreateUnknownType(Module);
    }

    public override string ShortName { get; }
    public override IType ReturnType { get; }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PROPERTY;
    }

    public bool IsExplicitImplementation => false;
    public IList<IExplicitImplementation> ExplicitImplementations => EmptyList<IExplicitImplementation>.Instance;
    public bool CanBeImplicitImplementation => false;
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