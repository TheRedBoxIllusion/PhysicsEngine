using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace PhysicsTesting
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        WorldContext worldContext;

        Texture2D blackBlock;
        Texture2D whiteBlock;
        Texture2D playerSprite;

        
        PhysicsEngine physicsEngine;
        List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

        Player player;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            worldContext = new WorldContext();
            worldContext.initialiseWorld();

            physicsEngine = new PhysicsEngine(worldContext);

            player = new Player(worldContext);
            physicsObjects.Add(player);


        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.ApplyChanges();


            blackBlock = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            whiteBlock = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            playerSprite = new Texture2D(_graphics.GraphicsDevice, 1, 1);


            blackBlock.SetData<Color>(new Color[]{ Color.Black });
            whiteBlock.SetData<Color>(new Color[] { Color.White });
            playerSprite.SetData<Color>(new Color[] { Color.Red });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            double horizontalAcceleration = 70; //Really large acceleration so that the kX can be high and the inputs are still responsive

            if (Keyboard.GetState().IsKeyDown(Keys.D)) {
                
                player.accelerationX += horizontalAcceleration;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A)) {
                player.accelerationX -= horizontalAcceleration;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W)) {
                if (player.isOnGround) {
                    player.accelerationY += 150;
                }
            }
            

            for (int i = 0; i < physicsObjects.Count; i++) {
                //General Physics simulations
                //Order: Acceleration, velocity then location
                physicsObjects[i].isOnGround = false;
                physicsEngine.addGravity(physicsObjects[i]);
                physicsEngine.computeAccelerationWithAirResistance(physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);
                physicsEngine.detectBlockCollisions(physicsObjects[i]);
                physicsEngine.computeAccelerationToVelocity(physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);
                physicsEngine.applyVelocityToPosition(physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                //Reset acceleration to be calculated next frame
                physicsObjects[i].accelerationX = 0;
                physicsObjects[i].accelerationY = 0;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            int[,] tempWorldArray = worldContext.worldArray;

            _spriteBatch.Begin();
            for (int x = 0; x < tempWorldArray.GetLength(0); x++)
            {
                for (int y = 0; y < tempWorldArray.GetLength(1); y++)
                {
                    if (tempWorldArray[x, y] == 0)
                    {
                        _spriteBatch.Draw(whiteBlock, new Rectangle(x * worldContext.pixelsPerBlock, y * worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), Color.White);
                    }
                    else {
                        _spriteBatch.Draw(blackBlock, new Rectangle(x * worldContext.pixelsPerBlock, y * worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), Color.White);
                    }
                }
            }

            //System.Diagnostics.Debug.WriteLine(player.x + ", " + player.y);
            _spriteBatch.Draw(playerSprite, new Rectangle((int)player.x, (int)player.y, (int)(0.9 * worldContext.pixelsPerBlock), 2 * worldContext.pixelsPerBlock), Color.White);

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }

    public class WorldContext
    {

        public int pixelsPerBlock { get; set; } = 32;
        public int[,] worldArray { get; set; }
        public void initialiseWorld()
        {
            worldArray = new int[32, 32];
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    worldArray[x, y] = 0; //Initialise the array to all equal 0. prevents any null values
                }
            }

            for (int i = 0; i < 16; i++)
            {
                worldArray[i, 10] = 1;
            }
            for (int i = 11; i < 21; i++)
            {
                worldArray[15, i] = 1;
            }
            for (int i = 15; i < 32; i++)
            {
                worldArray[i, 21] = 1;
            }
            //initialise the array with a z shape of 1s, to represent flat ground then a drop to another flat piece
        }
    }

    public class PhysicsEngine
    {
        public double blockSizeInMeters { get; } = 0.6; //The pixel size in meters can be found by taking this value and dividing it by pixelsPerBlock
        WorldContext wc;

        public PhysicsEngine(WorldContext worldContext)
        {
            wc = worldContext;
        }
        public void computeAccelerationWithAirResistance(PhysicsObject entity, double timeElapsed) {
            int directionalityX;
            int directionalityY;
            //If cases to determine the direction of the current velocity. It can be done purely mathematically but it yeilded /0 errors. The directionality is unimportant when velocity = 0
            if (entity.velocityX > 0)
            {
                directionalityX = 1;
            }
            else
            {
                directionalityX = -1;
            }
            if (entity.velocityY > 0)
            {
                directionalityY = 1;
            }
            else
            {
                directionalityY = -1;
            }
            entity.accelerationX += -(directionalityX * (entity.kX * Math.Pow(entity.velocityX, 2)));
            entity.accelerationY += -(directionalityY * (entity.kY * Math.Pow(entity.velocityY, 2)));
        }
        public void computeAccelerationToVelocity(PhysicsObject entity, double timeElapsed)
        {
            entity.velocityX += (entity.accelerationX) * timeElapsed;
            entity.velocityY += (entity.accelerationY) * timeElapsed;

            //Sets the velocity to 0 if it is below a threshold. Reduces excessive sliding and causes the drag function to actually reach a halt
            if ((entity.velocityX > 0 && entity.velocityX < entity.minVelocityX) || (entity.velocityX < 0 && entity.velocityX > -entity.minVelocityX))
            {
                entity.velocityX = 0;
            }
            if ((entity.velocityY > 0 && entity.velocityY < entity.minVelocityY) || (entity.velocityY < 0 && entity.velocityY > -entity.minVelocityY))
            {
                entity.velocityY = 0;
            }

        }

        public void addGravity(PhysicsObject entity)
        {
            entity.accelerationY -= 9.8;
        }

        public void applyVelocityToPosition(PhysicsObject entity, double timeElapsed) {
            //Adds the velocity * time passed to the x and y variables of the entity. Y is -velocity as the y-axis is flipped from in real life (Up is negative in screen space)
            //Converts the velocity into pixel space. This allows for realistic m/s calculations in the actual physics function and then converted to pixel space for the location
            entity.updateLocation(entity.velocityX * timeElapsed * (wc.pixelsPerBlock/blockSizeInMeters), -entity.velocityY * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters));
        }


        public void detectBlockCollisions(PhysicsObject entity) {
            //Gets the blocks within a single block radius around the entity. Detects if they are colliding, then if they are, calls another method
            int entityLocationInGridX = (int)Math.Floor(entity.x / wc.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(entity.y / wc.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)entity.collider.Width/wc.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)entity.collider.Height / wc.pixelsPerBlock);

            Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);
            int[,] worldArray = wc.worldArray; //A temporary storage of an array to reduce external function calls

            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++) { //A range of x values on either side of the outer bounds of the entity
                for(int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1)) {
                        if (worldArray[x, y] != 0) //In game implementation, air can either be null or have a special 'colliderless' block type 
                        {
                            Rectangle blockRect = new Rectangle(x * wc.pixelsPerBlock, y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
                            if (blockRect.Intersects(entityCollider))
                            {
                                if (entity.accelerationY < 0)
                                {
                                    entity.velocityY = 0;
                                    entity.accelerationY = 0;
                                    entity.y = blockRect.Y - entityCollider.Height + 1; //Plus one prevents the block from re-falling
                                }
                                entity.isOnGround = true;
                                System.Diagnostics.Debug.WriteLine("Has collided with a block! " + entity.accelerationY + ", ");
                            }
                        }
                    }
                }
            }

        }

    }

    public class PhysicsObject
    {
        public double accelerationX { get; set; }
        public double accelerationY { get; set; }

        public double velocityX { get; set; }
        public double velocityY { get; set; }

        public double x { get; set; }
        public double y { get; set; }

        public double kX { get; set; }
        public double kY { get; set; }

        public double bounceCoefficient { get; set; }

        public double minVelocityX { get; set; }
        public double minVelocityY { get; set; }

        public Rectangle collider { get; set; }

        public WorldContext worldContext;

        public bool isOnGround { get; set; }

        public PhysicsObject(WorldContext wc) {
            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 1.0;
            velocityY = 1.0;
            x = 0.0;
            y = 0.0;
            kX = 0.0;
            kY = 0.0;
            bounceCoefficient = 0.0;
            minVelocityX = 0.5;
            minVelocityY = 0.1;
            isOnGround = false;

            collider = new Rectangle(0, 0, wc.pixelsPerBlock, wc.pixelsPerBlock);

            worldContext = wc;
        }

        public void updateLocation(double xChange, double yChange) {
            x += xChange;
            y += yChange;
        }

        public void onBlockCollision(int blockX, int blockY) {
        
        }
    }

    public class Player : PhysicsObject
    {
        public Player(WorldContext wc) : base(wc)
        {
            x = 10.0;
            y = 10.0;
            //It's weird because k is unitless, however the fact that I'm in the world of pixels/second means that realistic drag coefficients don't work very well. 
            //I think it's because v^2 has a massive change with the pixel to block ratio. My math's is probably just wrong, so I'll merely account for it by using unrealistic numbers
            kX = 3;
            kY = 0.01;

            collider = new Rectangle(0, 0, (int)(0.9 * wc.pixelsPerBlock), 2 * wc.pixelsPerBlock);
        }
    }
}


