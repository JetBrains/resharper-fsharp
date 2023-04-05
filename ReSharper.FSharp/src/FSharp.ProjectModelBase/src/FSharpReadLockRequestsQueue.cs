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
    private static readonly Queue<Action> myQueue = new();

    public void Enqueue(Action action)
    {
      lock (mySyncObject)
      {
        myQueue.Enqueue(action);
        Monitor.Pulse(mySyncObject);
      }
    }

    public bool TryDequeue(out Action result)
    {
      lock (mySyncObject)
      {
        var hasRequests = myQueue.Count > 0;
        result = hasRequests ? myQueue.Dequeue() : null;

        return hasRequests;
      }
    }

    [CanBeNull]
    public Action ExtractOrBlock(int timeout, Task fcsTask = null)
    {
      lock (mySyncObject)
      {
        if (myQueue.Count > 0)
          return myQueue.Dequeue();

        Monitor.Wait(mySyncObject, timeout);

        return myQueue.Count > 0 && fcsTask is { IsCompleted: false }
          ? myQueue.Dequeue()
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
