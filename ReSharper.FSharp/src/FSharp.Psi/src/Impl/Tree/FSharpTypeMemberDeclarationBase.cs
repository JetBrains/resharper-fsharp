using System.Diagnostics;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpTypeMemberDeclarationBase : FSharpDeclarationBase, ITypeMemberDeclaration,
    IModifiersOwnerDeclaration
  {
    private volatile IDeclaredElement myCachedDeclaredElement;
    [CanBeNull] private volatile string myCachedName;

    protected abstract string DeclaredElementName { get; }
    
    protected override void ClearCachedData() =>
      myCachedName = null;

    public override string DeclaredName
    {
      get
      {
        lock (this)
        {
          return myCachedName ?? (myCachedName = DeclaredElementName);
        }
      }
    }

    protected override void PreInit()
    {
      base.PreInit();
      myCachedDeclaredElement = null;
    }

    public IDeclaredElement CachedDeclaredElement
    {
      get => myCachedDeclaredElement;
      set => myCachedDeclaredElement = value;
    }

    public IFSharpTypeElementDeclaration GetContainingTypeDeclaration() =>
      GetContainingNode<IFSharpTypeElementDeclaration>();

    ITypeDeclaration ITypeMemberDeclaration.GetContainingTypeDeclaration() => GetContainingTypeDeclaration();

    public void SetAbstract(bool value)
    {
    }

    public void SetSealed(bool value)
    {
    }

    public void SetVirtual(bool value)
    {
    }

    public void SetOverride(bool value)
    {
    }

    public void SetStatic(bool value)
    {
    }

    public void SetReadonly(bool value)
    {
    }

    public void SetExtern(bool value)
    {
    }

    public void SetUnsafe(bool value)
    {
    }

    public void SetVolatile(bool value)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    IModifiersOwner IModifiersOwnerDeclaration.ModifiersOwner => (IModifiersOwner) DeclaredElement;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;

    public AccessRights GetAccessRights() => AccessRights.PUBLIC;
    public bool IsAbstract => false;
    public bool IsSealed => false;
    public bool IsVirtual => false;
    public bool IsOverride => false;
    public bool IsStatic => false;
    public bool IsReadonly => false;
    public bool IsExtern => false;
    public bool IsUnsafe => false;
    public bool IsVolatile => false;
    public bool HasAccessRights => false;

    public void SetAccessRights(AccessRights rights)
    {
    }
  }
}