using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AccessorDeclaration: IParameterOwnerMemberDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    // CompiledName is ignored for accessors.
    protected override string DeclaredElementName => NameIdentifier.GetSourceName() + "_" + OwnerMember.SourceName;

    public override string CompiledName => DeclaredElementName;
    public override string SourceName => DeclaredElementName;

    public override FSharpSymbol GetFcsSymbol()
    {
      var mfv = OwnerMember?.GetFcsSymbol() as FSharpMemberOrFunctionOrValue;
      var members = mfv?.DeclaringEntity?.Value.MembersFunctionsAndValues;

      var range = mfv?.DeclarationLocation;
      return members?.FirstOrDefault(m => m.LogicalName == CompiledName && m.DeclarationLocation.Equals(range));
    }

    protected override IDeclaredElement CreateDeclaredElement() => CreateDeclaredElement(GetFcsSymbol());

    protected override IDeclaredElement CreateDeclaredElement([CanBeNull] FSharpSymbol fcsSymbol) =>
      fcsSymbol is FSharpMemberOrFunctionOrValue mfv && (mfv.IsPropertyGetterMethod || mfv.IsPropertySetterMethod)
        ? new FSharpPropertyAccessor(this)
        : null;

    public IMemberSignatureOrDeclaration OwnerMember =>
      MemberSignatureOrDeclarationNavigator.GetByAccessorDeclaration(this);

    public override AccessRights GetAccessRights() => ModifiersUtil.GetAccessRights(AccessModifier);

    public AccessorKind Kind =>
      NameIdentifier?.Name switch
      {
        "get" => AccessorKind.GETTER,
        "set" => AccessorKind.SETTER,
        _ => AccessorKind.UNKNOWN
      };

    public bool IsExplicit =>
      Kind == AccessorKind.GETTER && !(ParameterPatternsEnumerable.SingleItem.IgnoreInnerParens() is IUnitPat) ||
      Kind == AccessorKind.SETTER && ParameterPatternsEnumerable.Count() > 1;
  }
}
