using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public class FSharpClass : Class
  {
    public FSharpClass([NotNull] IClassPart part) : base(part)
    {
    }

    protected override MemberDecoration Modifiers => myParts.GetModifiers();
  }
}