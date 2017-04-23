using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  internal class FSharpCacheImplementationProcessor : FSharpCacheDeclarationProcessorBase
  {
    [CanBeNull] private readonly IDictionary<string, FSharpTypeInfo> myPairFileTypesInfo;

    public FSharpCacheImplementationProcessor(ICacheBuilder builder, int cacheVersion,
      [CanBeNull] IDictionary<string, FSharpTypeInfo> pairFileTypesInfo) : base(builder, cacheVersion)
    {
      myPairFileTypesInfo = pairFileTypesInfo;
    }

    internal override void StartTypePart(IFSharpTypeElementDeclaration declaration,
      FSharpPartKind partKind)
    {
      var isHidden = myPairFileTypesInfo != null &&
                     !myPairFileTypesInfo.ContainsKey(FSharpImplUtil.MakeClrName(declaration));
      Builder.StartPart(CreateTypePart(declaration, partKind, isHidden));
    }
  }
}