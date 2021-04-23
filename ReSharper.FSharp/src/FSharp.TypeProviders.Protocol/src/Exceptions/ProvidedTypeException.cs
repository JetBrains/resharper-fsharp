using System;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions
{
  internal class ProvidedTypeException : Exception
  {
    public ProvidedTypeException(string message) : base(message)
    {
    }
  }
}
