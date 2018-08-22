using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpInterface : Interface, IFSharpDeclaredElement
  {
    public FSharpInterface(IInterfacePart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => myParts.GetModifiers();

    public string SourceName => this.GetFSharpName().SourceName;
  }
}
