using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  internal class FSharpProperty : FSharpMemberBase<MemberDeclaration>, IProperty
  {
    public FSharpProperty([NotNull] ITypeMemberDeclaration declaration, FSharpMemberOrFunctionOrValue mfv)
      : base(declaration, mfv)
    {
      if (mfv == null)
      {
        ReturnType = TypeFactory.CreateUnknownType(Module);
        ShortName = declaration.DeclaredName;
        return;
      }

      var property =
        mfv.EnclosingEntity.MembersFunctionsAndValues.FirstOrDefault(
          m => m.IsProperty && m.DisplayName == mfv.DisplayName);

      if (property == null)
      {
        IsReadable = false;
        IsWritable = false;
        return;
      }

      IsReadable = property.HasGetterMethod;
      IsWritable = property.HasSetterMethod;
      ShortName = property.CompiledName; // todo: returns LogicalName, fix it in FCS

      ReturnType = FSharpElementsUtil.GetType(mfv.ReturnParameter.Type, declaration, Module) ??
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