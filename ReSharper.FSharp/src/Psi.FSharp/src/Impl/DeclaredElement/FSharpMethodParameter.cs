using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement
{
  public class FSharpMethodParameter : IParameter
  {
    private const string OptionalTypeName = "System.Runtime.InteropServices.OptionalAttribute";

    private const string DefaultParameterValueTypeName =
      "System.Runtime.InteropServices.DefaultParameterValueAttribute";

    private readonly IParametersOwner myParametersOwner;
    private readonly int myParameterIndex;

    public FSharpMethodParameter(FSharpParameter fsParam, [NotNull] IParametersOwner parametersOwner,
      int parameterIndex, ParameterKind kind, IType type, string name)
    {
      FSharpSymbol = fsParam;
      Type = type;
      Kind = kind;
      ShortName = name;
      myParametersOwner = parametersOwner;
      myParameterIndex = parameterIndex;
    }

    public IPsiServices GetPsiServices()
    {
      return myParametersOwner.GetPsiServices();
    }

    public IList<IDeclaration> GetDeclarations()
    {
      return EmptyList<IDeclaration>.InstanceList;
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return EmptyList<IDeclaration>.InstanceList;
    }

    public DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.PARAMETER;
    }

    public XmlNode GetXMLDoc(bool inherit)
    {
      return null;
    }

    public XmlNode GetXMLDescriptionSummary(bool inherit)
    {
      return null;
    }

    public bool IsValid()
    {
      return myParametersOwner.IsValid();
    }

    public bool IsSynthetic()
    {
      return false;
    }

    public HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
      return HybridCollection<IPsiSourceFile>.Empty;
    }

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
      return false;
    }

    public string ShortName { get; }
    public bool CaseSensitiveName => true;
    public PsiLanguageType PresentationLanguage => FSharpLanguage.Instance;

    public ITypeElement GetContainingType()
    {
      return myParametersOwner.GetContainingType();
    }

    public ITypeMember GetContainingTypeMember()
    {
      return (ITypeMember) myParametersOwner;
    }

    public IPsiModule Module => myParametersOwner.Module;
    public ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public FSharpParameter FSharpSymbol { get; }
    public IType Type { get; }

    public IList<IAttributeInstance> GetAttributeInstances(bool inherit)
    {
      return EmptyList<IAttributeInstance>.InstanceList;
    }

    public IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, bool inherit)
    {
      return EmptyList<IAttributeInstance>.InstanceList;
    }

    public bool HasAttributeInstance(IClrTypeName clrName, bool inherit)
    {
      return false;
    }

    public DefaultValue GetDefaultValue()
    {
      try
      {
        // todo: implement DefaultValue in FCS
        var defaultValueAttr = FSharpSymbol.Attributes
          .FirstOrDefault(a => a.AttributeType.QualifiedName.SubstringBefore(",", StringComparison.Ordinal)
            .Equals(DefaultParameterValueTypeName, StringComparison.Ordinal))?.ConstructorArguments.FirstOrDefault();
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

    public ParameterKind Kind { get; }
    public bool IsParameterArray => FSharpSymbol.IsParamArrayArg;
    public bool IsValueVariable => false;

    // todo: implement IsCliOptional in FCS
    public bool IsOptional =>
      FSharpSymbol.Attributes.Any(a =>
        a.AttributeType.QualifiedName.SubstringBefore(",", StringComparison.Ordinal).Equals(OptionalTypeName, StringComparison.Ordinal));

    public bool IsVarArg => false;
    public IParametersOwner ContainingParametersOwner => myParametersOwner;

    public override bool Equals(object obj)
    {
      var parameter = obj as FSharpMethodParameter;
      if (parameter == null) return false;

      return myParametersOwner.Equals(parameter.myParametersOwner) &&
             myParameterIndex == parameter.myParameterIndex;
    }

    public override int GetHashCode()
    {
      unchecked
      {
        return 197 * myParametersOwner.GetHashCode() + 47 * myParameterIndex;
      }
    }
  }
}