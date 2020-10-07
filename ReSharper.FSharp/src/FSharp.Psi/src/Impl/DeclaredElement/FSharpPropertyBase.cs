using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  
  internal abstract class FSharpPropertyMemberBase<TDeclaration> : FSharpPropertyBase<TDeclaration>
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpPropertyMemberBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration)
    {
      var prop = GetProperty(mfv);

      IsReadable = prop.HasGetterMethod || prop.IsPropertyGetterMethod ||
                   prop.IsModuleValueOrMember && !prop.IsMember;

      IsWritable = prop.IsMutable || prop.HasSetterMethod || prop.IsPropertySetterMethod;
    }

    public override bool IsReadable { get; }
    public override bool IsWritable { get; }

    [NotNull]
    private FSharpMemberOrFunctionOrValue GetProperty([NotNull] FSharpMemberOrFunctionOrValue prop)
    {
      // Property returned in AccessorProperty currently returns HasSetterMethod = false.
      // Workaround it by getting mfv property from declaring entity.
      var entity = prop.DeclaringEntity?.Value;
      var propertyName = prop.LogicalName;
      return entity?.MembersFunctionsAndValues.FirstOrDefault(m => m.LogicalName == propertyName) ?? prop;
    }
  }

  internal abstract class FSharpPropertyBase<TDeclaration> : FSharpMemberBase<TDeclaration>, IProperty
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpPropertyBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }


    protected override FSharpSymbol GetActualSymbol(FSharpSymbol symbol)
    {
      if (!(symbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      if (mfv.IsProperty || !mfv.IsModuleValueOrMember)
        return mfv;

      if (mfv.AccessorProperty?.Value is { } prop)
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

    public InvocableSignature GetSignature(ISubstitution substitution) =>
      new InvocableSignature(this, substitution);

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.PROPERTY;

    public string GetDefaultPropertyMetadataName() => ShortName;

    public IAccessor Getter => IsReadable ? new ImplicitAccessor(this, AccessorKind.GETTER) : null;
    public IAccessor Setter => IsWritable ? new ImplicitAccessor(this, AccessorKind.SETTER) : null;

    public abstract bool IsReadable { get; }
    public abstract bool IsWritable { get; }

    public bool IsAuto => false;
    public bool IsDefault => false;
  }
}
