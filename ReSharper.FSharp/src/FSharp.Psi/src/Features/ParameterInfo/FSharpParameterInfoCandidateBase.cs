﻿using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo
{
  public abstract class FSharpParameterInfoCandidateBase : ICandidate
  {
    protected FSharpParameterInfoCandidateBase(bool isFilteredOut = false)
    {
      IsFilteredOut = isFilteredOut;
    }

    public abstract RichText GetSignature(string[] namedArguments, AnnotationsDisplayKind showAnnotations,
      out TextRange[] parameterRanges, out int[] mapToOriginalOrder, out ExtensionMethodInfo extensionMethodInfo);

    public void GetParametersInfo(out ParamPresentationInfo[] paramInfos, out int paramArrayIndex)
    {
      paramInfos = EmptyArray<ParamPresentationInfo>.Instance;
      paramArrayIndex = -1;
    }

    public RichTextBlock GetDescription() => null;

    public virtual bool Matches(IDeclaredElement signature) => true;

    public bool IsFilteredOut { get; set; }
    public abstract int PositionalParameterCount { get; }
    public bool IsObsolete => false; // todo: use FCS API with symbols to get element attributes
    public RichTextBlock ObsoleteDescription => null;
  }
}
