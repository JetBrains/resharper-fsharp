using System;
using JetBrains.Diagnostics;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils
{
  public static class LoggerExtensions
  {
    public static (T result, Exception exception) CatchWithException<T>(this ILogger logger, Func<T> f)
    {
      try
      {
        return (f(), null);
      }
      catch (Exception ex)
      {
        logger.LogException(LoggingLevel.ERROR, ex, ExceptionOrigin.Algorithmic);
        return (default, ex);
      }
    }
  }
}
