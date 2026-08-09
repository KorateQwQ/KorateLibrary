using SilkyUIFramework.Elements;
using SilkyUIFramework.Helper;

namespace KL.Drawing.Snippets;

public class KLTextView : UITextView
{
    protected override void RecalculateString(float maxWidth)
    {
        IntermediateSnippets.Parse(Text, Color.White).ConvertPlainSnippet();

        float defaultLineSpacing = FontAssets.MouseText.Value.LineSpacing;
        float fontScale = defaultLineSpacing > 0f ? Font.LineSpacing / defaultLineSpacing : 1f;
        float fontVerticalOffset =  0f;
        if (Font == FontManager.HarmonyOS_Sans_SC.Value) fontVerticalOffset = 10.5f;
        if (Font == FontManager.LoliFont.Value) fontVerticalOffset = 3.5f;

        foreach (var snippet in IntermediateSnippets)
        {
            if (snippet is KLTextureSnippet textureSnippet)
                textureSnippet.SetFontScale(fontScale, fontVerticalOffset);
        }

        SnippetModule.UpdateProperties(Font, TextScale > 0f ? maxWidth / TextScale : float.MaxValue, MaxLines);
        if (WordWrap)
            SnippetModule.WordWrapSnippets(IntermediateSnippets);
        else
            SnippetModule.FromSnippets(IntermediateSnippets);

        TextSize = SnippetModule.GetStringSize(Font, Vector2.One);
    }
}
