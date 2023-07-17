using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IBindingSignature 
  {
    void SetAccessModifier(AccessRights accessModifier);
  }
}
