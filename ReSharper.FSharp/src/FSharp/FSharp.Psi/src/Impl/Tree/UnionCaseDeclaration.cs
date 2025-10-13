using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionCaseDeclaration : ICachedTypeMemberDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => Identifier;

    private bool? myHasFields;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myHasFields = null;
    }

    private static readonly Func<UnionCaseDeclaration, IDeclaredElement>
      DeclaredElementFactory = declaration => new FSharpUnionCaseProperty(declaration);

    public override IDeclaredElement DeclaredElement =>
      this.GetOrCreateDeclaredElement(DeclaredElementFactory);

    public bool HasFields
    {
      get
      {
        if (myHasFields != null)
          return myHasFields.Value;

        lock (this)
          return myHasFields ??= !FieldsEnumerable.IsEmpty();
      }
    }

    public FSharpUnionCaseClass NestedType =>
      (FSharpUnionCaseClass) CacheDeclaredElement;

    public IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index) => null;

    public IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations() =>
      EmptyList<IList<IFSharpParameterDeclaration>>.Instance;

    public void SetParameterFcsType(FSharpParameterIndex index, FSharpType fcsType) =>
      this.SetFieldDeclFcsType(index, fcsType);
  }

  internal static class UnionCaseLikeDeclarationUtil
  {

    public static ICaseFieldDeclaration GetFieldDecl(this IUnionCaseLikeDeclaration decl, FSharpParameterIndex index)
    {
      if (index.GroupIndex != 0)
        return null;

      if (index.NamedArg is { } namedArg)
        return decl.FieldsEnumerable.FirstOrDefault(f => f.SourceName == namedArg);

      return decl.FieldsEnumerable.ElementAtOrDefault(index.ParameterIndex ?? 0);
    }

    public static void SetFieldDeclFcsType(this IUnionCaseLikeDeclaration decl, FSharpParameterIndex index,
      FSharpType fcsType)
    {
      if (decl.GetFieldDecl(index) is { } fieldDecl)
        decl.GetFSharpTypeAnnotationUtil().SetTypeOwnerFcsType(fieldDecl, fcsType);
    }
  }
}
