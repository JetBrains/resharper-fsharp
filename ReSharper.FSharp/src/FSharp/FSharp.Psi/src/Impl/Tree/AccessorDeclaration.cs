using System;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using Range = FSharp.Compiler.Text.Range;

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
      if (members == null) return null;

      var range = TryGetMfvDeclarationLocation(mfv);
      var matchingMembers = members.Where(m => m.LogicalName == CompiledName).ToList();
      if (matchingMembers.IsEmpty())
        return mfv;

      if (matchingMembers.Count == 1)
        return matchingMembers[0];

      return matchingMembers.FirstOrDefault(m => TryGetMfvDeclarationLocation(m).Equals(range)) ??
             matchingMembers.FirstOrDefault();

      Range? TryGetMfvDeclarationLocation(FSharpMemberOrFunctionOrValue m)
      {
        try
        {
          return m.DeclarationLocation;
        }
        catch (Exception)
        {
          return null;
        }
      }
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
