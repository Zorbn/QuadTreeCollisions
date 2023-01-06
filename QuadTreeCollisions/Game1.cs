using System;
using System.Collections.Generic;
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
    private readonly List<Collider> enemies = new();
    private Collider player;
    private Input input;
    private const float Speed = 60f;
    private readonly Random random = new();
    private readonly QuadTree quadTree = new(0, 0, VirtualViewWidth, VirtualViewHeight);
    private readonly List<Collider> potentialPlayerCollisions = new();

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

        player = new Collider(0, 0, textureAtlas.TileSize * 2, textureAtlas.TileSize * 2);

        for (var i = 0; i < 100000; i++)
        {
            enemies.Add(new Collider(random.NextSingle() * VirtualViewWidth, random.NextSingle() * VirtualViewHeight, textureAtlas.TileSize, textureAtlas.TileSize));
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

        player.Pos += move * deltaTime * Speed;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        camera.ScaleToScreen(Window.ClientBounds.Width, Window.ClientBounds.Height);
        
        GraphicsDevice.Clear(Color.CornflowerBlue);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        
        foreach (Collider enemy in enemies)
        {
            textureAtlas.Draw(spriteBatch, camera, enemy.Pos, 4, 0, 1, 1, Color.Black);
        }
        
        quadTree.Clear();
        
        foreach (Collider enemy in enemies)
        {
            quadTree.Add(enemy);
        }

        potentialPlayerCollisions.Clear();
        quadTree.GetPotentialCollisions(potentialPlayerCollisions, player, false);

        foreach (Collider collider in potentialPlayerCollisions)
        {
            if (collider.Intersects(player))
            {
                textureAtlas.Draw(spriteBatch, camera, collider.Pos, 4, 0, 1, 1, Color.Red);
            }
        }
        
        textureAtlas.Draw(spriteBatch, camera, player.Pos, 4, 0, 2, 2, Color.White);

        spriteBatch.End();

        base.Draw(gameTime);
    }
}