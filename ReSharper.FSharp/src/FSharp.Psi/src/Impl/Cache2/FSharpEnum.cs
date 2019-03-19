using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpEnum : Enum, IFSharpDeclaredElement
  {
    public FSharpEnum(IEnumPart part) : base(part)
    {
    }

    public string SourceName => this.GetSourceName();
  }
}
