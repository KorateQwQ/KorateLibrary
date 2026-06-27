using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KL.Drawing.ThreeD;
using KL.Dusts;
using KL.Dusts.Fire;
using KL.Dusts.Ice;
using KL.Dusts.Lightning;
using KL.Dusts.Smoke;
using KL.Dusts.Water;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using SilkyUIFramework.Graphics2D;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace KL.Projectiles
{
    internal class DrawTestProj : KLProjectile
    {
        public static void testtest(){}
        
        private Texture2D Circle;

        Vector2[] 弧线坐标组 = new Vector2[400];
        
        Vector2[] topPoints = new Vector2[100];
        Vector2[] bottomPoints = new Vector2[100];
        List<Vector2> windPoints;

        private int time = 0;

        private float length = 0;
        private float rotationOffset;
        private float totalLife = 60;
        
        float lightningTime = 0;
        float lightningTime2 = 0;
        
        int testInt1 = 0;
        int testInt2 = 0;
        
        private static ObjModel testModel;
        private static ObjModel Sphere;

        private static Texture2D testModelTex;
        private static Texture2D testModelNormalTex;
        private static Texture2D testModelSpecTex;
        private static string testModelLoadErrorMessage;

        public override void Load()
        {
            testModel = ObjModel.Load("Models.bing");
            /*Circle ??= Mod.Assets.Request<Texture2D>("Projectiles/Circle", AssetRequestMode.ImmediateLoad).Value;
            testModel = ObjModel.Load(Mod, "Models/bing.obj");
            Sphere = ObjModel.Load(Mod, "Models/Sphere.obj");

            testModelNormalTex = TryLoadOptionalTexture("Models/HugeIceCone_nrm");
            testModelSpecTex = TryLoadOptionalTexture("Models/HugeIceCone_Spec");*/

            base.Load();
        }
        
        public override void SetStaticDefaults()
        {

            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {

            弧线坐标组 = new Vector2[1000];
            Projectile.timeLeft = 120;
            totalLife = Projectile.timeLeft;
            
            TrailLength = 30;
            Projectile.extraUpdates = 0;
            
            rotationOffset = Main.rand.NextFloat(0, MathHelper.Pi);
            //rotationOffset = 3.15f;
            
            //HeldInfo.IsHeld = true;
            
            base.SetDefaults();
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.GetGlobalProjectile<TimeStopGlobalProjectile>().ImmuneTimeStop = true;
            //Projectile.Center = Main.MouseWorld;
            //Projectile.rotation = (Projectile.Center - Owner.MountedCenter).ToRotation();
            //TimeStopManager.RequestTimeStop(Projectile,60);


            base.OnSpawn(source);
        }

        public override void AI()
        {
            float angleStep = MathHelper.TwoPi / 10;
            float angle = -1.2f+angleStep * BezierEase(Projectile.timeLeft,60)* 5f*Owner.direction; // 调整0.01f可以改变旋转速度
            float width = 200;
            float height = 80;

            //Projectile.Center =Owner.Center+ new Vector2(width * (float)Math.Cos(angle), height * (float)Math.Sin(angle));
            //Projectile.Center = Main.MouseWorld;

            /*if (Main.mouseLeft)
            {
                Projectile.timeLeft = 5;
                RotateToPosition(Main.MouseWorld,0.04f);
                Projectile.Center = Owner.MountedCenter + new Vector2(1, 0).RotatedBy(Projectile.rotation) * 50;
            }*/

            if (HasAuthority())
            {
                if (!Main.mouseLeft)
                {
                    HeldInfo.IsControlling = false;
                }

                //Projectile.Center = Main.MouseWorld;
            }
            
            
            topPoints = new Vector2[500];
            bottomPoints = new Vector2[500];
            Vector2 startPosition = Projectile.Center;
            Vector2 endPosition = Main.MouseWorld;
            Vector2 toward = startPosition - endPosition;
            Vector2 normalDir = Vector2.Normalize(new Vector2(-toward.Y, toward.X));


            float radius = 200*MathHelper.Lerp(2.2f, 1, Projectile.timeLeft / totalLife);
             
            int pointNum = 100;
            //windPoints = CalculateEllipseVertices(new Vector2(), radius, 40, ((Projectile.timeLeft) % 1800) / 5.0f,pointNum,0, MathHelper.TwoPi*0.6f);

            //Projectile.Center = Main.MouseWorld;
            
            List<FrameInfo> widthInfos = new List<FrameInfo>();
            widthInfos.Add(new FrameInfo(5,1,5));
            widthInfos.Add(new FrameInfo(1,1,10));
            widthInfos.Add(new FrameInfo(1,0,15));
            time++;
            //GetFrameValue(widthInfos,time,clamp:true);
            base.AI();
        }
        
        public override bool ShouldUpdatePosition()
        {
            return false;
        }
        
        
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
            List<int> overWiresUI)
        {
            base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
        }
        
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            Texture2D greenCircle  = Mod.Assets.Request<Texture2D>("Effects/Tex/空间扭曲", AssetRequestMode.ImmediateLoad).Value;
            Texture2D air  = Mod.Assets.Request<Texture2D>("Effects/Tex/Trail/AirFlow2", AssetRequestMode.ImmediateLoad).Value;

            Texture2D burstTex = Mod.Assets.Request<Texture2D>("Projectiles/burst", AssetRequestMode.ImmediateLoad).Value;

            Texture2D 扭曲角度  = Mod.Assets.Request<Texture2D>("Effects/Tex/空间扭曲角度", AssetRequestMode.ImmediateLoad).Value;
            Effect 空间扭曲角度effect = Mod.Assets.Request<Effect>("Effects/Content/AirDistortion", AssetRequestMode.ImmediateLoad).Value;
            
            Texture2D waterNoise  = Mod.Assets.Request<Texture2D>("Effects/Tex/水波", AssetRequestMode.ImmediateLoad).Value;
            Texture2D noise1  = Mod.Assets.Request<Texture2D>("Effects/Tex/Noise/5", AssetRequestMode.ImmediateLoad).Value;
            Texture2D noise2  = Mod.Assets.Request<Texture2D>("Effects/Tex/Noise/Twist", AssetRequestMode.ImmediateLoad).Value;


            Texture2D HeadClip = ModContent.Request<Texture2D>("KL/Effects/Tex/射灯", AssetRequestMode.ImmediateLoad).Value;
            Texture2D background = ModContent.Request<Texture2D>("KL/Effects/Tex/background_Premultiplied", AssetRequestMode.ImmediateLoad).Value;

            Texture2D ThunderTrail = ModContent.Request<Texture2D>("KL/Effects/Tex/Trail/LineTrail", AssetRequestMode.ImmediateLoad).Value;
            Texture2D Trail = ModContent.Request<Texture2D>("KL/Effects/Tex/光柱", AssetRequestMode.ImmediateLoad).Value;
            Texture2D Line = ModContent.Request<Texture2D>("KL/Effects/Tex/Line", AssetRequestMode.ImmediateLoad).Value;
            

            float imageRot = 0.3f;
            
            Texture2D wind = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/SemiCircle2", AssetRequestMode.ImmediateLoad).Value;
            Texture2D wind2 = ModContent.Request<Texture2D>("KL/Effects/Tex/Wind/wind4", AssetRequestMode.ImmediateLoad).Value;
            Texture2D 光圈 = ModContent.Request<Texture2D>("KL/Effects/Tex/光圈Premultiplied", AssetRequestMode.ImmediateLoad).Value;

            Effect radialDissolve = ModContent.Request<Effect>("KL/Effects/Content/RadialDissolve", AssetRequestMode.ImmediateLoad).Value;
            
            float totalTilt = -0.2f;

            /*EndBeginDraw(2,1,ss:SamplerState.LinearWrap);
            SphereEffect(new Vector3(0),new Vector3(rotTime,0,0),enableInner:true,InnerColor:Color.White);
            DrawInWorld(wind,Main.MouseWorld,/*火红色#1# new Color(255,127,0,255));
            
            SphereEffect(new Vector3(0),new Vector3(rotTime,0,0));
            DrawInWorld(wind,Main.MouseWorld,/*火红色#1# new Color(255,127,0,255));*/
            float fade = (float)Main.timeForVisualEffects / 60f;
            //fade在1-0之间循环
            fade = fade % 1;
            Texture2D test = AssetManager.GetTexture("KL/Effects/Tex/background");
            
            Effect IceCone3D = ModContent.Request<Effect>("KL/Effects/Content/ThreeD/IceCone3D", AssetRequestMode.ImmediateLoad).Value;
            testModelTex = Mod.Assets.Request<Texture2D>("Models/HugeIceCone_Color", AssetRequestMode.ImmediateLoad).Value;
            bool useNormalMap = testModelNormalTex != null;
            bool useSpecularMap = testModelSpecTex != null;
            //testModelTex = AssetManager.GetTexture("KL/Effects/Tex/PerlinX");

            Lighting.AddLight(Main.MouseWorld,Color.White.ToVector3());

            float rotTime = ((float)Main.timeForVisualEffects % 12000f)/30f;

            //Projectile.Center = Main.MouseWorld;
            if (testModel != null)
            {
                VertexBuffer vertexBuffer = testModel.GetOrCreateVertexBuffer(gd);
                int vertexCount = vertexBuffer.VertexCount;
                Vector3 position = new(Main.MouseWorld, 1f);
                Vector3 rotation3D = new Vector3(0,rotTime,1);
                Vector3 scale3D = new(10);
                Vector3 cameraPosition = GraphicsUtils.CameraPos(MathF.PI / 3f);
                Vector3 sunPosition = new(new Vector2(Main.screenWidth, Main.screenHeight)/4f+Main.screenPosition, -1000);
                Vector3 lightDirection = Vector3.Normalize(position - sunPosition);
                Vector4 baseColor = new Color(50, 220, 255, 200).ToVector4()*0.50f;
                Vector3 fresnelColor = (new Color(180, 220, 255)).ToVector3();
                Vector4 outlineColor = new Color(0, 0, 0, 255).ToVector4();
                bool enableToonShading = true;
                //底光
                float ambientStrength = 0.8f;
                //光照影响强度
                float diffuseStrength = 1f;
                //描边厚度
                float outlineThickness = 0f;
                //菲涅尔强度
                float fresnelStrength = 2f;
                float normalStrength = 0f;
                float specularStrength = 0.05f;
                float specularPower = 24f;
                Vector3 specularColor = Color.White.ToVector3();

                Matrix modelMatrix = Matrix.CreateScale(scale3D) *
                                     Matrix.CreateRotationX(rotation3D.X) *
                                     Matrix.CreateRotationY(rotation3D.Y) *
                                     Matrix.CreateRotationZ(rotation3D.Z) *
                                     Matrix.CreateTranslation(position);
                Matrix viewProjectionMatrix = GraphicsUtils.GetVPMatrix();

                RasterizerState baseRasterizerState = new RasterizerState
                {
                    CullMode = CullMode.CullClockwiseFace
                };
                
                Main.spriteBatch.End();

                IceCone3D.Parameters["uWorld"].SetValue(modelMatrix);
                IceCone3D.Parameters["uViewProjection"].SetValue(viewProjectionMatrix);
                IceCone3D.Parameters["uLightDirection"].SetValue(lightDirection);
                IceCone3D.Parameters["uLightColor"].SetValue(lightColor.ToVector3());
                IceCone3D.Parameters["uCameraPosition"].SetValue(cameraPosition);
                IceCone3D.Parameters["uBaseColor"].SetValue(baseColor);
                IceCone3D.Parameters["uFresnelColor"].SetValue(fresnelColor);
                IceCone3D.Parameters["uEnableToonShading"].SetValue(enableToonShading);
                IceCone3D.Parameters["uAmbientStrength"].SetValue(ambientStrength);
                IceCone3D.Parameters["uDiffuseStrength"].SetValue(diffuseStrength);
                IceCone3D.Parameters["uOutlineThickness"].SetValue(outlineThickness);
                IceCone3D.Parameters["uOutlineColor"].SetValue(outlineColor);
                IceCone3D.Parameters["uFresnelStrength"].SetValue(fresnelStrength);
                IceCone3D.Parameters["uUseNormalMap"].SetValue(useNormalMap);
                IceCone3D.Parameters["uUseSpecularMap"].SetValue(useSpecularMap);
                IceCone3D.Parameters["uNormalStrength"].SetValue(normalStrength);
                IceCone3D.Parameters["uSpecularStrength"].SetValue(specularStrength);
                IceCone3D.Parameters["uSpecularPower"].SetValue(specularPower);
                IceCone3D.Parameters["uSpecularColor"].SetValue(specularColor);

                gd.BlendState = BlendState.NonPremultiplied;
                gd.DepthStencilState = DepthStencilState.Default;
                gd.SamplerStates[0] = SamplerState.PointWrap;
                gd.SamplerStates[1] = SamplerState.LinearWrap;
                gd.SamplerStates[2] = SamplerState.LinearWrap;

                gd.RasterizerState = baseRasterizerState;
                gd.Textures[0] = testModelTex;
                gd.Textures[1] = testModelNormalTex ?? testModelTex;
                gd.Textures[2] = testModelSpecTex ?? testModelTex;
                gd.SetVertexBuffer(vertexBuffer);
                IceCone3D.CurrentTechnique.Passes[0].Apply();
                gd.DrawPrimitives(PrimitiveType.TriangleList, 0,vertexCount);

                gd.RasterizerState = baseRasterizerState;
                gd.Textures[0] = testModelTex;
                gd.Textures[1] = testModelNormalTex ?? testModelTex;
                gd.Textures[2] = testModelSpecTex ?? testModelTex;
                gd.SetVertexBuffer(vertexBuffer);
                IceCone3D.CurrentTechnique.Passes[1].Apply();
                gd.SetVertexBuffer(vertexBuffer);
                gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexCount);
    
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.Default, RasterizerState.CullNone, null, Main.Transform);
            }
            else if (!string.IsNullOrEmpty(testModelLoadErrorMessage))
            {
                PrintText(testModelLoadErrorMessage);
            }

            return false;
        }
        
        

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitNPC(target, hit, damageDone);
        }

        internal class TestModPlayer: ModPlayer
        {
            private bool canMoveInTimeStop = false;
            public bool CanMoveInTimeStop()
            {
                return canMoveInTimeStop;
            }

            public override void FrameEffects()
            {
                if(Main.GameUpdateCount%4==0)
                {


                }
                

                if(Main.myPlayer==Player.whoAmI&&Player.active&& GamePlayStatic.IsLeftClick()&&Player.HeldItem.IsAir)
                {
                    
                    Vector2 velocity = Main.MouseWorld - Player.Center;
                    velocity = velocity.SafeNormalize(Vector2.One);
                    /*
                    KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<WaterDust>(), 5, velocity*10.1f, 
                        6.28f,20, new Color(71, 176, 255, 155),new Vector2(0.5f),
                        0,50, scaleOffset: new Vector2(0.5f,0.5f),2);
                        */
                    
                    /*KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<LightningDust>(), 15, velocity*5.1f, 
                        2.8f,20, new Color(71, 176, 255, 255),new Vector2(1,0.5f),
                        0,50, scaleOffset: new Vector2(0.8f,0.1f),2);*/
                    
                    //GamePlayStatic.ShakeScreen(Main.MouseWorld,velocity,20,10f,8);
                    //Texture2D 爆裂 = Mod.Assets.Request<Texture2D>("Effects/Tex/air", AssetRequestMode.ImmediateLoad).Value;


                    //CreateRadialBlur(Main.MouseWorld,0.004f,16);
                    //CreateRadialWaveWarp(Main.MouseWorld,20,1600,30,2.5f);
                    
                    //KLProjectile.SpawnWindCircle(null,爆裂,Main.MouseWorld,velocity,frontColor:new Color(255,100,200),backColor:Color.Black,height:200,width:50);

                    
                    /*KLBasicDust.SpawnDust(Player.Center+ velocity*100f,ModContent.DustType<LightningDust>(), velocity*1.1f, 
                        20, new Color(71, 176, 255, 255), new Vector2(1));*/
                    

                    /*
                    KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<FireDust3>(), 1, velocity*10.1f, 
                        1.28f,40,  new Color(255, 255, 255,255),new Vector2(1f)*0.5f,
                        0,50, scaleOffset: new Vector2(0.2f,0.2f),10);*/
                    
                    /*KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<WaterDust>(), 5, velocity*15.1f, 
                        1.3f,20,  new Color(180, 230, 255,255),new Vector2(1),
                        50,100, scaleOffset: new Vector2(0.8f,0.8f),4);*/
                    
                    /*KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<LineSparkle>(), 3, velocity*10.1f, 
                        0.3f,30, new Color(255, 120, 239,255),Vector2.One,30,new Vector2(0.8f,0f),10);
                    
                    KLBasicDust.SpawnDustsCircle(Player.Center+ velocity*50f, ModContent.DustType<LineSparkle>(), 5, velocity*7.1f, 
                        0.6f,30, new Color(255, 120, 239,255),Vector2.One,10,new Vector2(0.8f,0f),10);*/
                    //Projectile.NewProjectile(null,Main.MouseWorld,velocity, ModContent.ProjectileType<DrawTestProj>(), 0, 0, Main.myPlayer);

                    //SpawnWindCircle(null,Main.MouseWorld,velocity,Color.Red,Color.Black,250,50);
                }
                
                /*int x = (int)(Main.MouseWorld.X / 16);
                int y = (int)(Main.MouseWorld.Y / 16);
                if(GamePlayStatic.IsLeftClick())
                {
                    Main.NewText(Main.tile[x,y]+" TileFrameNumber: " +Main.tile[x,y].TileFrameNumber);
                }

                if(GamePlayStatic.IsRightClick())
                {
                    Main.tile[x+1, y].TileType = 5;
                    Main.tile[x+1, y].TileFrameX = 88;
                    Main.tile[x+1, y].TileFrameY = 0;
                    
                    Main.tile[x, y].TileType = 5;
                    Main.tile[x, y].TileFrameX = 44;
                    Main.tile[x, y].TileFrameY = 198;
                    Main.tile[x, y].ForceChangeActice(true);
                }*/
                base.FrameEffects();
            }
        }
    }
}