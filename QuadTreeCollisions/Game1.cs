using System;
using System.Collections.Generic;
using GameClient;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace QuadTreeCollisions;

public class Game1 : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private Camera camera;
    private TextureAtlas textureAtlas;
    private const int VirtualViewWidth = 320;
    private const int VirtualViewHeight = 180;
    private const int WindowDefaultSizeMultiplier = 2;
    private readonly List<Vector2> enemies = new();
    private Vector2 playerPos;
    private Input input;
    private const float Speed = 60f;
    private readonly Random random = new();
    private readonly QuadTree quadTree = new(0, 0, VirtualViewWidth, VirtualViewHeight);
    private readonly HashSet<Rectangle> potentialPlayerCollisions = new();

    public Game1()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        InactiveSleepTime = TimeSpan.Zero; // Don't throttle FPS when the window is inactive.
        IsFixedTimeStep = false;
        graphics.SynchronizeWithVerticalRetrace = false;
        graphics.PreferredBackBufferWidth = VirtualViewWidth * WindowDefaultSizeMultiplier;
        graphics.PreferredBackBufferHeight = VirtualViewHeight * WindowDefaultSizeMultiplier;
        graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        camera = new Camera(VirtualViewWidth, VirtualViewHeight);
        textureAtlas = new TextureAtlas(GraphicsDevice, "Content/atlas.png", 16);
        input = new Input();

        for (var i = 0; i < 10000; i++)
        {
            enemies.Add(new Vector2(random.NextSingle() * VirtualViewWidth, random.NextSingle() * VirtualViewHeight));
        }
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        input.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            input.IsKeyDown(Keys.Escape))
            Exit();
        
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Console.WriteLine($"{1f / deltaTime}");
        
        if (deltaTime > 0.1f) return;

        Vector2 move = Vector2.Zero;
        if (input.IsKeyDown(Keys.Left)) move.X -= 1f;
        if (input.IsKeyDown(Keys.Right)) move.X += 1f;
        if (input.IsKeyDown(Keys.Up)) move.Y -= 1f;
        if (input.IsKeyDown(Keys.Down)) move.Y += 1f;

        playerPos += move * deltaTime * Speed;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        camera.ScaleToScreen(Window.ClientBounds.Width, Window.ClientBounds.Height);
        
        GraphicsDevice.Clear(Color.CornflowerBlue);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        foreach (Vector2 enemy in enemies)
        {
            textureAtlas.Draw(spriteBatch, camera, enemy, 4, 0, 1, 1, Color.Black);
        }
        
        quadTree.Clear();
        
        foreach (Vector2 enemy in enemies)
        {
            quadTree.Add(new Rectangle((int)enemy.X, (int)enemy.Y, textureAtlas.TileSize, textureAtlas.TileSize));
        }

        var playerRect = new Rectangle((int)playerPos.X, (int)playerPos.Y, textureAtlas.TileSize * 2, textureAtlas.TileSize * 2);
        potentialPlayerCollisions.Clear();
        quadTree.GetPotentialCollisions(potentialPlayerCollisions, playerRect);

        foreach (Rectangle rectangle in potentialPlayerCollisions)
        {
            if (rectangle.Intersects(playerRect))
            {
                textureAtlas.Draw(spriteBatch, camera, new Vector2(rectangle.X, rectangle.Y), 4, 0, 1, 1, Color.Red);
            }
        }
        
        textureAtlas.Draw(spriteBatch, camera, playerPos, 4, 0, 2, 2, Color.White);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}