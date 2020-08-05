using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo
{
  public class FSharpParameterInfoContext : IParameterInfoContext
  {
    public FSharpParameterInfoContext(int argument, ICandidate[] candidates, TextRange range, string[] namedArgs)
    {
      Argument = argument;
      Candidates = candidates;
      Range = range;

      NamedArguments = namedArgs;
    }

    public int Argument { get; }

    public ICandidate[] Candidates { get; }
    public TextRange Range { get; }

    public ICandidate DefaultCandidate => null;
    public string[] NamedArguments { get; set; }
    public Type ParameterListNodeType => null;
    public ICollection<Type> ParameterNodeTypes => null;
  }
}