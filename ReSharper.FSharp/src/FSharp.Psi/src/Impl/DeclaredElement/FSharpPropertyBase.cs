using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpPropertyBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IProperty
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpPropertyBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
      IsReadable = mfv.HasGetterMethod || mfv.IsPropertyGetterMethod ||
                   mfv.IsModuleValueOrMember && !mfv.IsMember;

      IsWritable = mfv.IsMutable || mfv.HasSetterMethod || mfv.IsPropertySetterMethod;
    }

    protected override FSharpSymbol GetActualSymbol(FSharpSymbol symbol)
    {
      if (!(symbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      if (mfv.IsProperty || !mfv.IsModuleValueOrMember)
        return mfv;

      if (mfv.AccessorProperty?.Value is var prop && prop != null)
        return prop;

      var members = mfv.DeclaringEntity?.Value.MembersFunctionsAndValues;
      return members?.FirstOrDefault(m => m.IsProperty && m.LogicalName == mfv.LogicalName) ?? mfv;
    }

    public IType Type => ReturnType;

    public override IType ReturnType
    {
      get
      {
        var mfv = Mfv;
        if (mfv == null)
          return TypeFactory.CreateUnknownType(Module);

        var returnType = mfv.IsPropertySetterMethod
          ? mfv.CurriedParameterGroups[0][0].Type
          : mfv.ReturnParameter.Type;

        return GetType(returnType);
      }
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PROPERTY;

    public string GetDefaultPropertyMetadataName() => ShortName;

    public IAccessor Getter => IsReadable ? new ImplicitAccessor(this, AccessorKind.GETTER) : null;
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;
    public bool IsReadable { get; }
    public bool IsWritable { get; }
    public bool IsAuto => false;
    public bool IsDefault => false;
  }
}
