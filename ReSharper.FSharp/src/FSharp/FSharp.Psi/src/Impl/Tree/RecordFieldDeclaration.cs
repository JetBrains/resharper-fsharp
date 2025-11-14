using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordFieldDeclaration
  {
    private bool? myIsMutable;
    
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpRecordField(this);

    protected override void ClearCachedData()
    {
      lock (this)
      {
        base.ClearCachedData();
        myIsMutable = null;  
      }
    }

    public bool IsMutable
    {
      get
      {
        lock (this)
        {
          if (myIsMutable is { } isMutable)
            return isMutable;

          myIsMutable = isMutable = MutableKeyword != null;
          return isMutable;
        }
      }
    }

    public void SetIsMutable(bool value)
    {
      if (value == IsMutable)
        return;

      if (value)
      {
        Identifier.AddTokenBefore(FSharpTokenType.MUTABLE);
        return;
      }

      var mutableKeyword = MutableKeyword.NotNull();
      var firstMeaningfulToken = FirstChild?.GetNextMeaningfulToken(true);
      if (mutableKeyword == firstMeaningfulToken && Identifier is { PrevSibling: { } identifierPrevSibling })
        ModificationUtil.DeleteChildRange(mutableKeyword, identifierPrevSibling);
      else
      {
        if (mutableKeyword.NextSibling is { } mutableNextSibling &&
            mutableNextSibling.GetTokenType() == FSharpTokenType.WHITESPACE)
          ModificationUtil.DeleteChild(mutableNextSibling);

        ModificationUtil.DeleteChild(mutableKeyword);
      }
    }

    public int Index =>
      RecordFieldDeclarationListNavigator.GetByFieldDeclaration(this)?.FieldDeclarationsEnumerable.IndexOf(this) ?? -1;

    public void SetType(IType type)
    {
      throw new System.NotImplementedException();
    }

    public IType Type => throw new System.NotImplementedException();
  }
}
