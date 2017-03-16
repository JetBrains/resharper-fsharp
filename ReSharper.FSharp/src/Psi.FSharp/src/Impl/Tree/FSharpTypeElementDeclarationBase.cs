using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpTypeElementDeclarationBase : FSharpCachedDeclarationBase, IFSharpTypeElementDeclaration
  {
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) DeclaredElement;

    /// <summary>
    /// May take long time due to waiting for FCS
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IEnumerable<IDeclaredType> SuperTypes
    {
      get
      {
        if (Symbol == null)
          TryFindAndSetFSharpSymbol();

        var entity = Symbol as FSharpEntity;
        return entity != null
          ? FSharpElementsUtil.GetSuperTypes(entity, GetPsiModule())
          : EmptyList<IDeclaredType>.Instance;
      }
    }

    /// <summary>
    /// May take long time due to waiting for FCS
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IDeclaredType BaseClassType
    {
      get
      {
        if (Symbol == null)
          TryFindAndSetFSharpSymbol();

        var entity = Symbol as FSharpEntity;
        return entity != null
          ? FSharpElementsUtil.GetBaseType(entity, GetPsiModule())
          : null;
      }
    }

    private void TryFindAndSetFSharpSymbol()
    {
      var fsFile = this.GetContainingFile() as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");

      var symbol = FSharpSymbolsUtil.TryFindFSharpSymbol(fsFile, GetText(), GetNameRange().EndOffset.Offset);
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      Symbol = mfv != null && mfv.IsImplicitConstructor
        ? mfv.EnclosingEntity
        : symbol;
    }

    public virtual TreeNodeCollection<ITypeMemberDeclaration> MemberDeclarations =>
      this.Children<ITypeMemberDeclaration>().Where(IsCompiledElementDeclaration).ToTreeNodeCollection();

    private static bool IsCompiledElementDeclaration(ITypeMemberDeclaration decl)
    {
      if (decl is ITypeDeclaration)
        return !(decl is IFSharpNotCompiledTypeDeclaration);

      return true; // todo: update when type members added
    }

    public string CLRName => FSharpImplUtil.MakeClrName(this);

    // todo
    public TreeNodeCollection<ITypeDeclaration> NestedTypeDeclarations =>
      MemberDeclarations.OfType<ITypeDeclaration>().ToTreeNodeCollection();

    public IList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;
  }
}