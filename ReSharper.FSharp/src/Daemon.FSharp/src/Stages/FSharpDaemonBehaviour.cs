using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpDaemonBehaviour : LanguageSpecificDaemonBehavior
  {
    public override ErrorStripeRequest InitialErrorStripe(IPsiSourceFile sourceFile)
    {
      return sourceFile.Properties.ShouldBuildPsi && sourceFile.Properties.ProvidesCodeModel &&
             sourceFile.IsLanguageSupported<FSharpLanguage>()
        ? ErrorStripeRequest.STRIPE_AND_ERRORS
        : ErrorStripeRequest.NONE;
    }
  }
}