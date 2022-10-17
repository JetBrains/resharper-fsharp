using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpSignatureParameter : FSharpCachedTypeMemberBase<IParameterSignatureTypeUsage>, IFSharpParameter
  {
    public (int group, int index) Position { get; }

    public bool IsErased =>
      GetDeclaration() is { } decl &&
      FunctionTypeUsageNavigator.GetByArgumentTypeUsage(decl) is { Parent: not ITypeUsage } &&
      Symbol is { } fcsParam && fcsParam.Type.IsUnit();

    public bool IsGenerated => false;

    public FSharpSignatureParameter([NotNull] IDeclaration declaration, (int group, int index) position)
      : base(declaration) =>
      Position = position;

    public FSharpParameter Symbol
    {
      get
      {
        var decl = GetDeclaration();
        if (decl?.OwnerDeclaration is not IFSharpDeclaration fsDecl ||
            fsDecl.GetFcsSymbol() is not FSharpMemberOrFunctionOrValue mfv)
          return null;

        var (group, index) = Position;

        var fcsParamGroups = mfv.CurriedParameterGroups;
        if (fcsParamGroups.Count >= group) return null;

        var fcsParamGroup = fcsParamGroups[group];
        return fcsParamGroup.Count >= index ? null : fcsParamGroup[index];
      }
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;

    public ITypeMember GetContainingTypeMember()
    {
      var paramDecl = GetDeclaration();
      var paramOwnerDecl = paramDecl?.GetContainingNode<IFSharpParameterOwnerDeclaration>() as IDeclaration;
      return paramOwnerDecl?.DeclaredElement as ITypeMember;
    }

    public IType Type
    {
      get
      {
        var typeParameters = GetContainingTypeMember() is IFSharpTypeParametersOwner typeParamsOwner
          ? typeParamsOwner.AllTypeParameters
          : EmptyList<ITypeParameter>.Instance;

        return Symbol is { } fcsParam
          ? fcsParam.Type.MapType(typeParameters, Module)
          : TypeFactory.CreateUnknownType(Module);
      }
    }

    public override IList<IDeclaration> GetDeclarations() => 
      GetPartialDeclarations(null);

    public override IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      GetPartialDeclarations(sourceFile);

    private IList<IDeclaration> GetPartialDeclarations([CanBeNull] IPsiSourceFile sourceFile)
    {
      var owner = ContainingParametersOwner;
      if (owner == null)
        return  EmptyList<IDeclaration>.Instance;

      using var _ = CompilationContextCookie.GetOrCreate(owner.Module.GetContextFromModule());

      var ownerDecls =
        sourceFile != null
          ? owner.GetDeclarationsIn(sourceFile)
          : owner.GetDeclarations();

      var result = new List<IDeclaration>();

      foreach (var ownerDecl in ownerDecls)
        if (ownerDecl is IFSharpParameterOwnerDeclaration fsParamOwnerDecl &&
            fsParamOwnerDecl.GetParameter(Position) is IDeclaration fsParamDecl)
          result.Add(fsParamDecl);

      return result;
    }

    public IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      Symbol.GetAttributeInstances(Module);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      Symbol.GetAttributeInstances(clrName, Module);

    public bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      Symbol.HasAttributeInstance(clrName);

    public bool IsOptional => Symbol.HasAttributeInstance(PredefinedType.OPTIONAL_ATTRIBUTE_CLASS);

    public DefaultValue GetDefaultValue() => this.GetParameterDefaultValue();

    public ParameterKind Kind => Symbol.GetParameterKind();
    public bool IsParameterArray => this.IsParameterArray();

    public bool IsValueVariable => false;
    public bool IsVarArg => false;

    public IParametersOwner ContainingParametersOwner { get; }
  }
}
