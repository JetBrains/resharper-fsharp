namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader.Protocol.Cache
{
  public interface IReadProvidedCache<out T>
  {
    T Get(int key);
    bool Contains(int key);
  }

  public interface IWriteProvidedCache<in T>
  {
    void Add(int id, T value);
  }

  public interface IProvidedCache<T> : IReadProvidedCache<T>, IWriteProvidedCache<T>
  {
  }
}
