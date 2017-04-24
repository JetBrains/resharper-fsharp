using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  public class FSharpLookupItem : TextLookupItemBase
  {
    [NotNull] private readonly string myText;
    private readonly bool myIsEscaped;
    private readonly bool myIsParam;

    public FSharpLookupItem([NotNull] string text, [NotNull] IconId image, bool isEscaped, bool isParam) : base(false)
    {
      myText = text;
      myIsEscaped = isEscaped;
      myIsParam = isParam;
      Image = image;
    }

    public override IconId Image { get; }

    public override string Text
    {
      get
      {
        var nameText = myIsEscaped ? "``" + myText + "``" : myText;
        var insertText = myIsParam ? nameText + " = " : nameText;
        return insertText;
      }
    }

    protected override RichText GetDisplayName()
    {
      return LookupUtil.FormatLookupString(myText, TextColor);
    }
  }
}