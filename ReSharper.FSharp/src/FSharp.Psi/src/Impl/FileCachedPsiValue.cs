using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FileCachedPsiValue<T> : CachedPsiValue<T>
  {
    protected override int GetTimestamp(ITreeNode element) =>
      element.GetContainingFile()?.ModificationCounter ??
      element.GetPsiServices().Files.PsiCacheTimestamp;
  }
}
