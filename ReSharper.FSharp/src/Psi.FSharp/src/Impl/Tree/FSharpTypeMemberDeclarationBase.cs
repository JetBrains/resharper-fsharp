using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public abstract class FSharpTypeMemberDeclarationBase : FSharpDeclarationBase, ITypeMemberDeclaration,
    IModifiersOwnerDeclaration
  {
    private volatile IDeclaredElement myCachedDeclaredElement;

    protected override void PreInit()
    {
      base.PreInit();
      myCachedDeclaredElement = null;
    }

    public IDeclaredElement CachedDeclaredElement
    {
      get { return myCachedDeclaredElement; }
      set { myCachedDeclaredElement = value; }
    }

    public ITypeDeclaration GetContainingTypeDeclaration()
    {
      return GetContainingNode<ITypeDeclaration>();
    }

    public void SetAbstract(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetSealed(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetVirtual(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetOverride(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetStatic(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetReadonly(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetExtern(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetUnsafe(bool value)
    {
      throw new System.NotImplementedException();
    }

    public void SetVolatile(bool value)
    {
      throw new System.NotImplementedException();
    }

    IModifiersOwner IModifiersOwnerDeclaration.DeclaredElement => (IModifiersOwner) DeclaredElement;
    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;

    public AccessRights GetAccessRights()
    {
      return AccessRights.PUBLIC;
    }

    public bool IsAbstract => false;
    public bool IsSealed => false;
    public bool IsVirtual=> false;
    public bool IsOverride => false;
    public bool IsStatic => false;
    public bool IsReadonly => false;

    public bool IsExtern => false;
    public bool IsUnsafe => false;
    public bool IsVolatile => false;


    public void SetAccessRights(AccessRights rights)
    {
    }

    public bool HasAccessRights => false;
  }
}