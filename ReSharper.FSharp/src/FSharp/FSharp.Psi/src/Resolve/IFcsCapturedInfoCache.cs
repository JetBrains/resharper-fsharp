using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

public interface IFcsCapturedInfoCache
{
  internal IFcsFileCapturedInfo GetOrCreateFileCapturedInfo(IPsiSourceFile sourceFile);
}
