namespace KL.Extensions;

public static class TextureExtentions
{
    public static Vector2 Origin(this Texture2D tex, int totalX = 1,int totalY = 1)
    {
        return tex.Size() / new Vector2(totalX, totalY) * 0.5f;
    }
    
    public static Rectangle GetRec(this Texture2D tex, int frame = 0, int TotalX = 1, int TotalY = 1, bool Toright = true)
    {
        int frameWidth = tex.Width / TotalX;//图片总高度除以长度，得到每张图的长度
        int frameHeight = tex.Height / TotalY;//图片总高度除以高度，得到每张图的高度
        int startX = frameWidth * (frame % TotalX);//每一帧的起始坐标X
        int startY = frameHeight * (frame / TotalX);//每一帧的起始坐标Y
        if (!Toright)
        {
            startX = frameWidth * (frame / TotalY);
            startY = frameHeight * (frame % TotalY);
        }
        Rectangle result = new Rectangle(startX, startY, frameWidth, frameHeight);
        return result;
    }
}