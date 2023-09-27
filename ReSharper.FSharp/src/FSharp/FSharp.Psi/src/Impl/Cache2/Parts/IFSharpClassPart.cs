using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IFSharpClassPart : Class.IClassPart, IFSharpClassLikePart
  {
    [CanBeNull] IClass GetSuperClass();
  }
}
