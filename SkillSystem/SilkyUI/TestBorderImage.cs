using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace KL.SkillSystem.SilkyUI;

//边框类型的UI，可以在被选择时绘制边框。
public class TestBorderImage : SUIImage
{
    private float updateTime = 0;
    public bool choose = false;

    private Asset<Texture2D> TexEffect;

    public TestBorderImage(Asset<Texture2D> texture)
    {
        Texture2D = texture;
    }
    public TestBorderImage(string SkillName)
    {
        Texture2D= ModContent.Request<Texture2D>(SkillName, AssetRequestMode.ImmediateLoad);
        //TexEffect = ModContent.Request<Texture2D>(SkillName+"冰锥特效", AssetRequestMode.ImmediateLoad);
    }
    protected override void OnInitialize()
    {
        base.OnInitialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        choose = true;
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        choose = false;
        base.OnMouseLeave(evt);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        //EndBeginDrawUI(1);
        Asset<Texture2D> BorderTex = TextureAssets.Extra[98];

        var position = InnerBounds.Position;
        position +=(Vector2)InnerBounds.Size/2;
        
        
        /*spriteBatch.Draw(BorderTex.Value, InnerBounds.Position , new Rectangle(0, 0, BorderTex.Width(), BorderTex.Height()),
            Color.White, 3.14f / 2f, BorderTex.Size() * 0.5f, 1, 0, 0f);*/
        
        

        if (choose)//绘制物品选中提示
        {
            Color c = new(30, 90, 195, 0);
            float baseScale = 0.8f;
            float roateV = Main.GameUpdateCount % 120 * 0.02f;
            for (int i = 0; i < 8;i++)
            {
                spriteBatch.Draw(BorderTex.Value, position + new Vector2(10-i*2.3f-Main.GameUpdateCount%120*0.15f, -22+ roateV), new Rectangle(0, 0, BorderTex.Width(), BorderTex.Height()),
                    c * ((120-Main.GameUpdateCount % 120)/120f), 3.14f / 2f, BorderTex.Size() * 0.5f, baseScale - i*0.07f, 0, 0f);
                    
                spriteBatch.Draw(BorderTex.Value, position + new Vector2(-10 + i * 2.3f + Main.GameUpdateCount % 120 * 0.15f, 22- roateV), new Rectangle(0, 0, BorderTex.Width(), BorderTex.Height()),
                    c * ((120 - Main.GameUpdateCount % 120) / 120f), -3.14f / 2f, BorderTex.Size() * 0.5f, baseScale - i * 0.07f, 0, 0f);
                    
                spriteBatch.Draw(BorderTex.Value, position + new Vector2(-22+ roateV, -11 + i * 2.3f + Main.GameUpdateCount % 120 * 0.15f), new Rectangle(0, 0, BorderTex.Width(), BorderTex.Height()),
                    c * ((120 - Main.GameUpdateCount % 120) / 120f), 0, BorderTex.Size() * 0.5f, baseScale - i * 0.07f, 0, 0f);
                    
                spriteBatch.Draw(BorderTex.Value, position + new Vector2(22- roateV, 11 - i * 2.3f - Main.GameUpdateCount % 120 * 0.15f), new Rectangle(0, 0, BorderTex.Width(), BorderTex.Height()),
                    c * ((120 - Main.GameUpdateCount % 120) / 120f), 0, BorderTex.Size() * 0.5f, baseScale - i * 0.07f, 0, 0f);
            }
        }
    }
}