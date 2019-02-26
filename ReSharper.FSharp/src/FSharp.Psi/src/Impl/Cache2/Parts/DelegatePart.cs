using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Impl.Special;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;
using Delegate = JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.Delegate;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class DelegatePart : FSharpTypeParametersOwnerPart<IDelegateDeclaration>, Delegate.IDelegatePart
  {
    public DelegatePart([NotNull] IDelegateDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable),
        declaration.TypeParameters, cacheBuilder)
    {
    }

    public DelegatePart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpDelegate(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Delegate;

    public IParameter[] Parameters =>
      GetDeclaration() is IDelegateDeclaration declaration
        ? GetParameters(declaration.DelegateSignature.DelegateArguments)
        : EmptyArray<IParameter>.Instance;

    internal IPsiModule Module => GetPsiModule();

    public IType ReturnType =>
      GetDeclaration() is IDelegateDeclaration declaration
        ? GetType(declaration.DelegateSignature.DelegateReturnType, true)
        : TypeFactory.CreateUnknownType(Module);

    protected IParameter[] GetParameters(IList<Tuple<FSharpOption<string>, FSharpType>> types)
    {
      var invokeMethod = (TypeElement as IDelegate)?.InvokeMethod;
      if (invokeMethod == null)
        return EmptyArray<IParameter>.Instance;

      if (types.Count == 1 && types[0].Item2.IsUnit)
        return EmptyArray<IParameter>.Instance;

      const string name = SharedImplUtil.MISSING_DECLARATION_NAME;
      var result = new IParameter[types.Count];
      for (var i = 0; i < types.Count; i++)
        result[i] = new Parameter(invokeMethod, i, ParameterKind.VALUE, GetType(types[i].Item2, false), name);

      return result;
    }

    protected IType GetType([CanBeNull] FSharpType fsType, bool isReturn) =>
      fsType != null && TypeElement is TypeElement typeElement
        ? fsType.MapType(typeElement.TypeParameters, Module, true, isReturn)
        : TypeFactory.CreateUnknownType(Module);

    public ReferenceKind ReturnKind => ReferenceKind.VALUE;
    public IAttributesSet ReturnTypeAttributes => EmptyAttributesSet.Instance;
  }
}
