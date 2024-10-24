using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.FSharp
{
  public class FSharpReadLockRequestsQueue
  {
    private readonly object mySyncObject = new();
    private static readonly Queue<Action> Queue = new();

    public void Enqueue(Action action)
    {
      var logger = Logger.GetLogger<FSharpReadLockRequestsQueue>();

      logger.Trace("Enqueue: trying to enter lock");
      lock (mySyncObject)
      {
        Queue.Enqueue(action);
        logger.Trace("Enqueue: enqueued FCS request, before Pulse");
        Monitor.Pulse(mySyncObject);
        logger.Trace("Enqueue: after Pulse");
      }
    }

    public bool TryDequeue(out Action result)
    {
      var logger = Logger.GetLogger<FSharpReadLockRequestsQueue>();

      logger.Trace("TryDequeue: trying to enter lock");
      lock (mySyncObject)
      {
        logger.Trace($"TryDequeue: there are {Queue.Count} requests, returning the first");
        var hasRequests = Queue.Count > 0;
        result = hasRequests ? Queue.Dequeue() : null;

        return hasRequests;
      }
    }

    [CanBeNull]
    public Action ExtractOrBlock(int timeout, Task fcsTask = null)
    {
      var logger = Logger.GetLogger<FSharpReadLockRequestsQueue>();

      logger.Trace("ExtractOrBlock: trying to enter lock");
      lock (mySyncObject)
      {
        if (Queue.Count > 0)
        {
          logger.Trace("ExtractOrBlock: returning a request");
          return Queue.Dequeue();
        }

        logger.Trace("ExtractOrBlock: waiting for requests");
        Monitor.Wait(mySyncObject, timeout);

        logger.Trace("ExtractOrBlock: woken up");
        if (Queue.Count > 0 && fcsTask is { IsCompleted: false })
        {
          logger.Trace($"ExtractOrBlock: There are {Queue.Count} requests, returning the first");
          return Queue.Dequeue();
        }

        logger.Trace("ExtractOrBlock: No requests, returning null");
        return null;
      }
    }

    public void WakeUp()
    {
      var logger = Logger.GetLogger<FSharpReadLockRequestsQueue>();

      logger.Trace("WakeUp: trying to enter lock");
      lock (mySyncObject)
      {
        logger.Trace("WakeUp: before PulseAll");
        Monitor.PulseAll(mySyncObject);
        logger.Trace("WakeUp: after PulseAll");
      }
    }
  }
}
