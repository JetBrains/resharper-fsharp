using System;
using FSharp.Compiler.CodeAnalysis;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalBinding
  {
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new NotImplementedException();

      var headPat = HeadPattern;
      if (headPat != null)
        headPat.AddTokenBefore(FSharpTokenType.MUTABLE);
    }

    public bool IsInline => InlineKeyword != null;

    public void SetIsInline(bool value)
    {
      if (value)
        throw new NotImplementedException();

      using var _ = WriteLockCookie.Create(IsPhysical());
      var inlineKeyword = InlineKeyword;
      if (inlineKeyword.PrevSibling is Whitespace whitespace)
        ModificationUtil.DeleteChildRange(whitespace, inlineKeyword);
      else
        ModificationUtil.DeleteChild(inlineKeyword);
    }


    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
    public bool IsLiteral => false;
    public bool IsComputed => LetOrUseExprNavigator.GetByBinding(this)?.IsComputed == true;

    IDeclaredElement IParameterOwnerMemberDeclaration.DeclaredElement =>
      HeadPattern is IReferencePat rp ? rp.DeclaredElement : null;

    FSharpSymbolUse IParameterOwnerMemberDeclaration.GetFcsSymbolUse() =>
      HeadPattern is IReferencePat rp ? rp.GetFcsSymbolUse() : null;
  }
}
