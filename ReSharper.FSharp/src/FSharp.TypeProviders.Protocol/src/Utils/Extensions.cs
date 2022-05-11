using System;
using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Util.dataStructures;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils
{
  public static class Extensions
  {
    public static string GetLogName(this ProvidedAssembly assembly) =>
      assembly.GetName().Version == null ? "generated assembly" : assembly.FullName;

    public static void RemoveAll<T, TU>(this IDictionary<T, TU> dict, Func<KeyValuePair<T, TU>, bool> match)
    {
      var itemsToRemove = new FrugalLocalList<KeyValuePair<T, TU>>();
      foreach (var item in dict)
        if (match(item))
          itemsToRemove.Add(item);

      foreach (var item in itemsToRemove) dict.Remove(item.Key);
    }

    public static void RemoveAll<T, TU>(this BidirectionalMapOnDictionary<T, TU> dict,
      Func<KeyValuePair<T, TU>, bool> match)
    {
      var itemsToRemove = new FrugalLocalList<KeyValuePair<T, TU>>();
      foreach (var item in dict)
        if (match(item))
          itemsToRemove.Add(item);

      foreach (var item in itemsToRemove) dict.RemoveLeft(item.Key);
    }
  }
}
