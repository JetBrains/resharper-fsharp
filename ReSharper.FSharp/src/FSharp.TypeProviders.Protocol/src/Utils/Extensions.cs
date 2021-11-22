using System;
using System.Collections.Generic;
using FSharp.Compiler;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils
{
  public static class Extensions
  {
    public static string GetLogName(this ExtensionTyping.ProvidedAssembly assembly) =>
      assembly.GetName().Version == null ? "generated assembly" : assembly.FullName;

    public static void RemoveAll<T, TU>(this Dictionary<T, TU> dict, Func<KeyValuePair<T, TU>, bool> match)
    {
      var itemsToRemove = new FrugalLocalList<KeyValuePair<T, TU>>();
      foreach (var item in dict)
        if (match(item))
          itemsToRemove.Add(item);

      foreach (var item in itemsToRemove) dict.Remove(item.Key);
    }
  }
}
