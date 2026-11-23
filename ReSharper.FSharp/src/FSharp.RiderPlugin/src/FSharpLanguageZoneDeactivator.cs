using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.Rider.Backend.Env;

namespace JetBrains.ReSharper.Plugins.FSharp.RiderPlugin;

[ZoneActivator]
[ZoneMarker]
public class FSharpLanguageZoneDeactivator(RiderPluginManager riderPluginManager)
  : IDeactivateDynamic<ILanguageFSharpZone>,
    IRequire<IRiderBackendSharedFeatureEnvironmentZone>
{
  private const string PluginId = "com.jetbrains.rider.fsharp";

  bool IDeactivateDynamic<ILanguageFSharpZone>.IsDeactivateActive() => !riderPluginManager.IsLoaded(PluginId);
}
