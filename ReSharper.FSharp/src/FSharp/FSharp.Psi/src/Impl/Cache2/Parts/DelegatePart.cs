using System;
using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class DelegatePart : FSharpTypeParametersOwnerPart<IFSharpTypeDeclaration>, IFSharpDelegatePart
  {
    public DelegatePart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifier, declaration.Attributes),
        declaration.TypeParameterDeclarations, cacheBuilder)
    {
    }

    public DelegatePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpDelegate(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Delegate;

    public FSharpDelegateSignature FcsDelegateSignature =>
      GetDeclaration() is {TypeRepresentation: IDelegateRepresentation { DelegateSignature: { } signature }}
        ? signature
        : null;

    public IParameter[] Parameters =>
      FcsDelegateSignature is { } signature
        ? GetParameters(signature.DelegateArguments)
        : EmptyArray<IParameter>.Instance;

    internal IPsiModule Module => GetPsiModule();

    public IType ReturnType =>
      FcsDelegateSignature is { } signature
        ? GetType(signature.DelegateReturnType, true)
        : TypeFactory.CreateUnknownType(Module);

    protected IParameter[] GetParameters(IList<Tuple<FSharpOption<string>, FSharpType>> types)
    {
      var invokeMethod = (TypeElement as IDelegate)?.InvokeMethod;
      if (invokeMethod == null)
        return EmptyArray<IParameter>.Instance;

      if (types.Count == 1 && types[0].Item2.IsUnit())
        return EmptyArray<IParameter>.Instance;

      const string name = SharedImplUtil.MISSING_DECLARATION_NAME;
      var result = new IParameter[types.Count];
      for (var i = 0; i < types.Count; i++)
        result[i] = new Parameter(invokeMethod, i, ParameterKind.VALUE, GetType(types[i].Item2, false), name);

      return result;
    }

    protected IType GetType([CanBeNull] FSharpType fcsType, bool isReturn) =>
      fcsType != null && TypeElement is { } typeElement
        ? fcsType.MapType(typeElement.TypeParameters, Module, true, isReturn)
        : TypeFactory.CreateUnknownType(Module);

    public ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
  }
}
