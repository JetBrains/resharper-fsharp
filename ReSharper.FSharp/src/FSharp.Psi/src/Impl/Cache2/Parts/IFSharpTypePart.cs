using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  public interface IFSharpTypePart
  {
    [NotNull]
    string SourceName { get; }
  }
}
