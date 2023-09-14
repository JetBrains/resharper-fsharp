namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.UsageChecking

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell

type FSharpDummyUsageAnalyzer(lifetime, suppressors) =
    inherit UsageAnalyzer(lifetime, suppressors)

    override x.InteriorShouldBeProcessed(_, _) = false
    override x.ProcessElement(_, _) = ()

[<Language(typeof<FSharpLanguage>, Instantiation.DemandAnyThreadSafe)>]
[<ZoneMarker(typeof<DaemonZone>, typeof<ILanguageFSharpZone>, typeof<PsiFeaturesImplZone>)>]
type FSharpUsageCheckingServices(lifetime, suppressors) =
    inherit UsageCheckingServices(FSharpDummyUsageAnalyzer(lifetime, suppressors), null, null)

    let _ = FSharpLanguage.Instance // workaround to create assembly reference (dotnet/fsharp#3522)

    override x.CreateUnusedLocalDeclarationAnalyzer(_, _, _) = null


type FSharpCollectUsagesPsiFileProcessor(collectUsagesStageProcess, daemonProcess, settingsStore,
        collectUsagesStagePersistentData, fibers, scopeProcessorFactory, usageCheckingServiceManager,
        globalFileStructureBuilder, swaExtensionProviders) =
    inherit CommonCollectUsagesPsiFileProcessor(collectUsagesStageProcess, daemonProcess, settingsStore,
        collectUsagesStagePersistentData, fibers, scopeProcessorFactory, usageCheckingServiceManager,
        globalFileStructureBuilder, swaExtensionProviders)

    override this.InteriorShouldBeProcessed(_, _) = false
    override this.ProcessBeforeInterior(_, _) = ()


[<Language(typeof<FSharpLanguage>)>]
[<ZoneMarker(typeof<DaemonEngineZone>, typeof<DaemonZone>, typeof<IProjectModelZone>, typeof<PsiFeaturesImplZone>)>]
type FSharpCollectUsagesPsiFileProcessorFactory() =
    inherit CollectUsagesPsiFileProcessorFactory()

    override this.CreatePsiFileProcessor(collectUsagesStageProcess, daemonProcess, settingsStore,
            collectUsagesStagePersistentData, fibers, scopeProcessorFactory, usageCheckingServiceManager,
            globalFileStructureBuilder, swaExtensionProviders) =
        FSharpCollectUsagesPsiFileProcessor(collectUsagesStageProcess, daemonProcess, settingsStore,
            collectUsagesStagePersistentData, fibers, scopeProcessorFactory, usageCheckingServiceManager,
            globalFileStructureBuilder, swaExtensionProviders)
