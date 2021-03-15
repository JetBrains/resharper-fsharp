using System;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions
{
  internal class ProvidedTypeException : Exception
  {
    public ProvidedTypeException(string message) : base(message)
    {
    }
  }
}
