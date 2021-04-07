using System.Collections.Generic;
using NuGet;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.Cache
{
  public class SimpleProvidedCache<T> : IProvidedCache<(T model, int typeProviderId), int>
  {
    private readonly Dictionary<int, (T model, int typeProviderId)> myEntities = new Dictionary<int, (T, int)>();

    public void Add(int id, (T, int) value) => myEntities.Add(id, value);

    public void Remove(int typeProviderId) =>
      myEntities.RemoveAll(t => t.Value.typeProviderId == typeProviderId);

    public (T model, int typeProviderId) Get(int key) => myEntities[key];

    public string Dump() => $"{typeof(T).Name}:\nCount: {myEntities.Count}";
  }
}
