using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpMethodParameter : IParameter
  {
    public readonly IParametersOwner Owner;
    public readonly int Index;

    public FSharpMethodParameter(FSharpParameter fsParam, [NotNull] IParametersOwner owner,
      int index, IType type)
    {
      FSharpSymbol = fsParam;
      Type = type;
      Owner = owner;
      Index = index;
    }

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

    public string ShortName =>
      FSharpSymbol.DisplayName is var name && name.IsEmpty()
        ? SharedImplUtil.MISSING_DECLARATION_NAME
        : name;

    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;
    public ITypeElement GetContainingType() => Owner.GetContainingType();
    public ITypeMember GetContainingTypeMember() => (ITypeMember) Owner;

    public IPsiModule Module => Owner.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public FSharpParameter FSharpSymbol { get; }
    public IType Type { get; }

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit) =>
      FSharpSymbol.Attributes.HasAttributeInstance(clrName.FullName);

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit) =>
      FSharpSymbol.Attributes.ToAttributeInstances(Module);

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit) =>
      FSharpSymbol.Attributes.GetAttributes(clrName.FullName).ToAttributeInstances(Module);

    public DefaultValue GetDefaultValue()
    {
      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueAttr = FSharpSymbol.Attributes
          .FirstOrDefault(a => a.GetClrName() == PredefinedType.DEFAULTPARAMETERVALUE_ATTRIBUTE_CLASS.FullName)
          ?.ConstructorArguments.FirstOrDefault();
        return defaultValueAttr == null
          ? new DefaultValue(Type)
          : new DefaultValue(new ConstantValue(defaultValueAttr.Item2, type: null));
      }
      // todo: change exception in FCS
      catch (Exception)
      {
        return DefaultValue.BAD_VALUE;
      }
    }

    public ParameterKind Kind => FSharpSymbol.GetParameterKind();
    public bool IsParameterArray => FSharpSymbol.IsParamArrayArg;
    public bool IsValueVariable => false;

    // todo: implement IsCliOptional in FCS
    public bool IsOptional =>
      FSharpSymbol.Attributes.HasAttributeInstance(PredefinedType.OPTIONAL_ATTRIBUTE_CLASS);

    public bool IsVarArg => false;
    public IParametersOwner ContainingParametersOwner => Owner;

    public override bool Equals(object obj)
    {
      if (!(obj is FSharpMethodParameter parameter)) return false;

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
  }
}
