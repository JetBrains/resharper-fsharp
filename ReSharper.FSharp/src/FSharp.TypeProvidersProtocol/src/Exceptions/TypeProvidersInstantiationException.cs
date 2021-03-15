using System;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Exceptions
{
  public class TypeProvidersInstantiationException : Exception
  {
    public TypeProvidersInstantiationException(string message, string number) : base(message)
      => FcsNumber = int.TryParse(number, out var fcsNumber) ? fcsNumber : 0;

    public TypeProvidersInstantiationException(string message, int number) : base(message) => FcsNumber = number;

    public int FcsNumber { get; }
    public override string ToString() => FcsNumber.ToString();
    public static readonly string ReasonTypeFqn = typeof(TypeProvidersInstantiationException).FullName;
  }
}
