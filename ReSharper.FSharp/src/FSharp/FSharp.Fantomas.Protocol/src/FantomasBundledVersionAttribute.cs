using System;

namespace JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol
{
  public class FantomasBundledVersionAttribute : Attribute
  {
    public string Value { get; }

    public FantomasBundledVersionAttribute(string value) => Value = value;
  }
}
