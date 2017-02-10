using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
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