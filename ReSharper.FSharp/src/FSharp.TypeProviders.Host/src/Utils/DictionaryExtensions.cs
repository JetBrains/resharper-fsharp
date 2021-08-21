using System;
using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Utils
{
  internal static class DictionaryExtensions
  {
    public static void RemoveAll<T, TU>(this Dictionary<T, TU> dictionary, Func<KeyValuePair<T, TU>, bool> selector)
    {
      var ids = new List<T>();

      foreach (var kvp in dictionary)
        if (selector(kvp))
          ids.Add(kvp.Key);

      foreach (var id in ids)
        dictionary.Remove(id);
    }
  }
}
