using System;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol
{
  public class TypeProviderExternal : IScheduler
  {
    public TypeProviderExternal(Lifetime lifetime)
    {
      //lifetime.OnTermination(() => Current?.Shutdown());
    }

    public void Queue(Action action)
    {
      action();
    }

    public bool IsActive => true;
    public bool OutOfOrderExecution => false;
  }
}
