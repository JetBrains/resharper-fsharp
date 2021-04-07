using System;
using JetBrains.Rd.Tasks;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
{
  public static class ExecutionWrapper
  {
    public static T ExecuteWithCatch<T>(this TypeProvidersConnection connection, Func<T> func)
    {
      try
      {
        return connection.Execute(func);
      }
      catch (RdFault ex)
      {
        if (ex.ReasonTypeFqn == TypeProvidersInstantiationException.ReasonTypeFqn)
          throw new TypeProvidersInstantiationException(ex.ReasonMessage, ex.ReasonText);

        throw new ProvidedTypeException(ex.ReasonMessage);
      }
    }
  }
}
