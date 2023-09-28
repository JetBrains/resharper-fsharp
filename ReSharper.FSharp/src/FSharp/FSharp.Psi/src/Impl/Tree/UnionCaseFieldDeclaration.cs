﻿using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionCaseFieldDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override string BaseName => "Item";
    protected override int BaseIndex => 1;

    protected override bool UseBaseNameForSingleField => true;

    protected override IUnionCaseLikeDeclaration FieldOwnerDeclaration =>
      UnionCaseDeclarationNavigator.GetByField(this);

    protected override IList<FSharpField> GetTypeFields(FSharpSymbol type) =>
      type is FSharpUnionCase unionCase ? unionCase.Fields : null;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpUnionCaseField<UnionCaseFieldDeclaration>(this);
  }

  internal partial class ExceptionFieldDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override string BaseName => "Data";
    protected override int BaseIndex => 0;

    protected override bool UseBaseNameForSingleField => false;

    protected override IUnionCaseLikeDeclaration FieldOwnerDeclaration =>
      ExceptionDeclarationNavigator.GetByField(this);

    protected override IList<FSharpField> GetTypeFields(FSharpSymbol type) =>
      type is FSharpEntity { IsFSharpExceptionDeclaration: true } entity ? entity.FSharpFields : null;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpUnionCaseField<ExceptionFieldDeclaration>(this);
  }

  internal abstract class UnionCaseFieldDeclarationBase : FSharpProperTypeMemberDeclarationBase
  {
    protected override string DeclaredElementName
    {
      get
      {
        var id = NameIdentifier;
        if (id != null)
          return id.GetSourceName();

        var fields = GetFields();
        if (UseBaseNameForSingleField && fields.Count == 1)
          return BaseName;

        var fieldIndex = fields.IndexOf((ICaseFieldDeclaration) this);
        return fieldIndex == -1
          ? SharedImplUtil.MISSING_DECLARATION_NAME
          : BaseName + (BaseIndex + fieldIndex);
      }
    }

    public int Index => GetIndex(GetFields());

    protected int GetIndex(TreeNodeCollection<ICaseFieldDeclaration> fields) =>
      fields.IndexOf((ICaseFieldDeclaration) this);

    protected abstract int BaseIndex { get; }
    protected abstract string BaseName { get; }
    protected abstract bool UseBaseNameForSingleField { get; }

    protected abstract IUnionCaseLikeDeclaration FieldOwnerDeclaration { get; }

    protected TreeNodeCollection<ICaseFieldDeclaration> GetFields() =>
      FieldOwnerDeclaration?.Fields ?? TreeNodeCollection<ICaseFieldDeclaration>.Empty;

    public override FSharpSymbol GetFcsSymbol()
    {
      if (base.GetFcsSymbol() is FSharpField namedField) // todo: named params have type FSharpParameters
        return namedField;

      var typeSymbol = FieldOwnerDeclaration?.GetFcsSymbol();
      var index = Index;
      var fields = GetTypeFields(typeSymbol);
      return fields != null && index <= fields.Count
        ? fields[index]
        : null;
    }

    [CanBeNull]
    protected abstract IList<FSharpField> GetTypeFields([CanBeNull] FSharpSymbol type);

    public bool IsNameGenerated => NameIdentifier != null;
  }
}
