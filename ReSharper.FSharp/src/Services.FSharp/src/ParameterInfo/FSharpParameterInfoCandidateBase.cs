using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.ParameterInfo
{
  public abstract class FSharpParameterInfoCandidateBase : ICandidate
  {
    public abstract RichText GetSignature(string[] namedArguments, AnnotationsDisplayKind showAnnotations,
      out TextRange[] parameterRanges, out int[] mapToOriginalOrder, out ExtensionMethodInfo extensionMethodInfo);

    public void GetParametersInfo(out ParamPresentationInfo[] paramInfos, out bool isParamsArray)
    {
      throw new System.NotImplementedException();
    }

    public RichTextBlock GetDescription()
    {
      throw new System.NotImplementedException();
    }

    public bool Matches(IDeclaredElement signature)
    {
      return true;
    }

    public bool IsFilteredOut { get; set; }
    public abstract int PositionalParameterCount { get; }
    public bool IsObsolete => false; // todo: use FCS API with symbols to get element attributes
    public RichTextBlock ObsoleteDescription => null;
  }
}