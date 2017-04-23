using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  internal class FSharpCacheSignatureProcessor : FSharpCacheDeclarationProcessorBase
  {
    [CanBeNull] private readonly IDictionary<string, FSharpTypeInfo> myPairFileTypesInfo;

    public FSharpCacheSignatureProcessor(ICacheBuilder builder, int cacheVersion,
      [CanBeNull] IDictionary<string, FSharpTypeInfo> pairFileTypesInfo) : base(builder, cacheVersion)
    {
      myPairFileTypesInfo = pairFileTypesInfo;
    }

    internal override void StartTypePart(IFSharpTypeElementDeclaration declaration, FSharpPartKind partKind)
    {
      var clrName = FSharpImplUtil.MakeClrName(declaration);
      var partKindToCreate =
        myPairFileTypesInfo != null && myPairFileTypesInfo.ContainsKey(clrName)
          ? myPairFileTypesInfo[clrName].TypeKind
          : partKind;
      Builder.StartPart(CreateTypePart(declaration, partKindToCreate));
    }
  }
}