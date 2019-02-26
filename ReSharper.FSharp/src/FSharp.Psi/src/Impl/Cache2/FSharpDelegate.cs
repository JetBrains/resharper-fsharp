using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpDelegate : Delegate, IFSharpDeclaredElement
  {
    public FSharpDelegate([NotNull] IDelegatePart part) : base(part)
    {
    }

    public string SourceName => this.GetSourceName();
  }
}
