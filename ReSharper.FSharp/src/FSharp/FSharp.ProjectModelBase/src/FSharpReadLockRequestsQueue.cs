using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class FSharpReadLockRequestsQueue
  {
    private readonly object mySyncObject = new();
    private static readonly Queue<Action> Queue = new();

    public void Enqueue(Action action)
    {
      lock (mySyncObject)
      {
        Queue.Enqueue(action);
        Monitor.Pulse(mySyncObject);
      }
    }

    public bool TryDequeue(out Action result)
    {
      lock (mySyncObject)
      {
        var hasRequests = Queue.Count > 0;
        result = hasRequests ? Queue.Dequeue() : null;

        return hasRequests;
      }
    }

    [CanBeNull]
    public Action ExtractOrBlock(int timeout, Task fcsTask = null)
    {
      lock (mySyncObject)
      {
        if (Queue.Count > 0)
          return Queue.Dequeue();

        Monitor.Wait(mySyncObject, timeout);

        return Queue.Count > 0 && fcsTask is { IsCompleted: false }
          ? Queue.Dequeue()
          : null;
      }
    }

    public void WakeUp()
    {
      lock (mySyncObject)
        Monitor.PulseAll(mySyncObject);
    }
  }
}
