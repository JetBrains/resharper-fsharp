using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Env;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;

[ZoneMarker(typeof(IReSharperHostNetLightweightFeatureZone))]
[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class LightweightFcsCapturedInfoCache : IFcsCapturedInfoCache
{
  IFcsFileCapturedInfo IFcsCapturedInfoCache.GetOrCreateFileCapturedInfo(IPsiSourceFile sourceFile) =>
    EmptyFcsFileCapturedInfo.Instance;
}
