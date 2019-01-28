using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpStruct : Struct, IFSharpDeclaredElement
  {
    public FSharpStruct([NotNull] IStructPart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => Parts.GetModifiers();
    public string SourceName => this.GetSourceName();
  }
}
