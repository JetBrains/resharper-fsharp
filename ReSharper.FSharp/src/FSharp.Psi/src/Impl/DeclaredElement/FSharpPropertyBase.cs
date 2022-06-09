using System.Linq;
using FSharp.Compiler.Symbols;
using FSharp.Compiler.Text;
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
      var (isReadable, isWriteable) = GetReadWriteRights(mfv);
      IsReadable = isReadable;
      IsWritable = isWriteable;
    }

    public override bool IsReadable { get; }
    public override bool IsWritable { get; }

    private static (bool isReadable, bool isWriteable) GetReadWriteRights([NotNull] FSharpMemberOrFunctionOrValue prop)
    {
      bool IsWriteable(FSharpMemberOrFunctionOrValue mfv) =>
        mfv.IsMutable || mfv.HasSetterMethod || mfv.IsPropertySetterMethod;

      bool IsReadable(FSharpMemberOrFunctionOrValue mfv) =>
        mfv.HasGetterMethod || mfv.IsPropertyGetterMethod ||
        mfv.IsModuleValueOrMember && !mfv.IsMember;

      var entity = prop.DeclaringEntity?.Value;
      if (entity == null)
        return (IsReadable(prop), IsWriteable(prop));

      // Property returned in AccessorProperty currently returns HasSetterMethod = false.
      // Workaround it by getting mfv property from declaring entity.
      var name = prop.LogicalName;
      var range = prop.DeclarationLocation;

      var mfvs = entity.MembersFunctionsAndValues;
      var propMfv = mfvs.FirstOrDefault(m => RangeModule.equals(m.DeclarationLocation, range) && m.LogicalName == name);
      if (propMfv != null)
        // Matching property is returned by Fcs
        return (IsReadable(propMfv), IsWriteable(propMfv));

      // Property isn't returned by Fcs for some reason, e.g. in explicit implementation.
      // Trying to calculate info from accessors.

      var isReadable = false;
      var isWriteable = false;

      foreach (var mfv in mfvs)
      {
        if (RangeModule.equals(mfv.DeclarationLocation, range) && mfv.DisplayNameCore == name)
        {
          if ($"get_{name}" == mfv.LogicalName)
          {
            var getterType = mfv.FullType;
            if (getterType.IsFunctionType && getterType.GenericArguments[1].Equals(prop.FullType))
              isReadable = true;
            continue;
          }

          if ($"set_{name}" == mfv.LogicalName)
          {
            var getterType = mfv.FullType;
            if (getterType.IsFunctionType && getterType.GenericArguments[0].Equals(prop.FullType))
              isWriteable = true;
          }
        }
      }
      return (isReadable, isWriteable);
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
    public bool IsRequired => false;
    public bool IsAuto => false;
    public virtual bool IsDefault => false;

    public override bool Equals(object obj) =>
      obj is IProperty && base.Equals(obj);

    public override int GetHashCode() =>
      ShortName.GetHashCode();
  }
}
