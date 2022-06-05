using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.ParameterInfo
{
  public class FSharpParameterInfoContext : IParameterInfoContext
  {
    private readonly int myArgument;

    public FSharpParameterInfoContext(int argument, ICandidate[] candidates, TextRange range, string[] namedArgs)
    {
      myArgument = argument;
      Candidates = candidates;
      Range = range;

      NamedArguments = namedArgs;
    }

    public int GetArgument(ICandidate candidate) => myArgument;
    public ICandidate[] Candidates { get; }
    public TextRange Range { get; }

    public ICandidate DefaultCandidate => null;
    public string[] NamedArguments { get; set; }
    public Type ParameterListNodeType => null;
    public ICollection<Type> ParameterNodeTypes => null;
  }
}