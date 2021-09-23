using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache
{
  public class DependentProvidedTypesCache<TKey, TArg> : ProvidedEntitiesCacheBase<ProvidedType, (int, TKey), TArg>
  {
    private readonly IRdCall<TArg, int> myGetIdCall;

    public DependentProvidedTypesCache(TypeProvidersContext typeProvidersContext, IRdCall<TArg, int> getIdCall) : base(
      typeProvidersContext) => myGetIdCall = getIdCall;

    protected override bool KeyHasValue((int, TKey) key) => true;

    protected override ProvidedType Create((int, TKey) key, int typeProviderId, TArg rdArg)
    {
      var typeId = TypeProvidersContext.Connection.ExecuteWithCatch(() => myGetIdCall.Sync(rdArg, RpcTimeouts.Maximal));
      return TypeProvidersContext.ProvidedTypesCache.GetOrCreate(typeId, typeProviderId);
    }

    protected override ProvidedType[] CreateBatch((int, TKey)[] _, int __, TArg arg) =>
      throw new System.NotImplementedException();

    public override string Dump() => throw new System.NotImplementedException();
  }
}
