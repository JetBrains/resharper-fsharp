using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public abstract class FSharpMethodParameterBase : IParameter
  {
    public readonly IParametersOwner Owner;
    public readonly int Index;

    protected FSharpMethodParameterBase([NotNull] IParametersOwner owner, int index, [NotNull] IType type)
    {
      Type = type;
      Owner = owner;
      Index = index;
    }

    public IType Type { get; }

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public IPsiModule Module => Owner.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public abstract bool IsParameterArray { get; }
    public bool IsValueVariable => false;
    public abstract bool IsOptional { get; }
    public DeclarationScope Scope => DeclarationScope.Unscoped;
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
    public ITypeElement GetContainingType() => Owner.GetContainingType();
    public ITypeMember GetContainingTypeMember() => (ITypeMember) Owner;

    public abstract DefaultValue GetDefaultValue();
    public abstract ParameterKind Kind { get; }
    public virtual bool IsVarArg => false;

    public override bool Equals(object obj)
    {
      if (!(obj is FSharpMethodParameterBase parameter)) return false;

      return Owner.Equals(parameter.Owner) &&
             Index == parameter.Index;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return 197 * Owner.GetHashCode() + 47 * Index;
      }
    }

    public abstract IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource);

    public abstract IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName,
      AttributesSource attributesSource);

    public abstract bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource);
  }
}
