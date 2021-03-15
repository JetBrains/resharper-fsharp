namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public interface IProvidedCache<T, in TKey>
  {
    T Get(TKey key);
    void Add(TKey id, T value);
    void Remove(TKey partialKey);

    //Test api
    string Dump();
  }

  public interface IBiDirectionalProvidedCache<T, TKey> : IProvidedCache<(T model, int typeProviderId), TKey>
  {
    bool TryGetKey(T model, int requestingTypeProviderId, out TKey key);
  }
}
