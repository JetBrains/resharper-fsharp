using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public abstract class FSharpMethodParameterBase([NotNull] IParametersOwner owner, FSharpParameterIndex index)
    : IFSharpGeneratedParameterFromPattern
  {
    public abstract IType Type { get; }

    public readonly IParametersOwner Owner = owner;

    public FSharpParameterIndex FSharpIndex => index;

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public IPsiModule Module => Owner.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public abstract bool IsParams { get; }
    public abstract bool IsParameterArray { get; }
    public abstract bool IsParameterCollection { get; }
    public bool IsValueVariable => false;
    public abstract bool IsOptional { get; }
    public ScopedKind GetScope(IResolveContext context = null) => ScopedKind.None;
    public IParametersOwner ContainingParametersOwner => Owner;

    public IPsiServices GetPsiServices() => Owner.GetPsiServices();
    public IList<IDeclaration> GetDeclarations() => EmptyList<IDeclaration>.InstanceList;
    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) => EmptyList<IDeclaration>.InstanceList;
    public DeclaredElementType GetElementType() => CLRDeclaredElementType.PARAMETER;
    public XmlNode GetXMLDoc(bool inherit) => null;
    public XmlNode GetXMLDescriptionSummary(bool inherit) => null;
    public bool IsValid() => Owner.IsValid();
    public bool IsSynthetic() => false;
    public HybridCollection<IPsiSourceFile> GetSourceFiles() => HybridCollection<IPsiSourceFile>.Empty;
    public bool HasDeclarationsIn(IPsiSourceFile sourceFile) => false;
    public abstract string ShortName { get; }
    public abstract string SourceName { get; }
    public ITypeElement GetContainingType() => Owner.GetContainingType();
    public ITypeMember GetContainingTypeMember() => (ITypeMember) Owner;

    public abstract DefaultValue GetDefaultValue();
    public abstract ParameterKind Kind { get; }
    public virtual bool IsVarArg => false;

    public abstract IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource);

    public abstract IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName,
      AttributesSource attributesSource);

    public abstract bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource);

    public IEnumerable<IFSharpParameterDeclaration> GetParameterOriginDeclarations()
    {
      if (ContainingParametersOwner is not { } owner)
        return EmptyList<IFSharpParameterDeclaration>.Instance;

      var result = new List<IFSharpParameterDeclaration>();
      foreach (var ownerDecl in owner.GetDeclarations())
      {
        if (FSharpParameterOwnerDeclarationNavigator.Unwrap(ownerDecl) is not { } parameterOwnerDecl)
          continue;

        var paramDecl = parameterOwnerDecl.GetParameterDeclaration(index);
        if (paramDecl is IFSharpPattern pat && pat.TryGetNameIdentifierOwner() is IFSharpParameterDeclaration paramPatternDecl)
          result.Add(paramPatternDecl);
        if (paramDecl is IParameterSignatureTypeUsage paramSigTypeUsage)
          result.Add(paramSigTypeUsage);
      }

      return result;
    }

    public IEnumerable<ILocalVariable> GetParameterOriginElements() =>
      GetParameterOriginDeclarations().OfType<ILocalVariable>();
  }
}
