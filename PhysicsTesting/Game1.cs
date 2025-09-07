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
        Texture2D collisionSprite;

        
        PhysicsEngine physicsEngine;
        List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

        Player player;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            this.TargetElapsedTime = TimeSpan.FromSeconds(1d/120d);

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
            collisionSprite = new Texture2D(_graphics.GraphicsDevice, 1, 1);


            blackBlock.SetData<Color>(new Color[]{ Color.Black });
            whiteBlock.SetData<Color>(new Color[] { Color.White });
            playerSprite.SetData<Color>(new Color[] { Color.Red });
            collisionSprite.SetData<Color>(new Color[] { Color.Green });

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

            //As the game's framerate changes, the small impulses caused by the added acceleration changes.
            //So the added values must change with the elapsed game time


            double horizontalAcceleration = 1; //The acceleration in m/s^-2
            double jumpAcceleration = 5;

            if (Keyboard.GetState().IsKeyDown(Keys.D)) {
                
                player.accelerationX += horizontalAcceleration / gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A)) {
                player.accelerationX -= horizontalAcceleration / gameTime.ElapsedGameTime.TotalSeconds;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W)) {
                if (player.isOnGround) {
                    player.accelerationY += jumpAcceleration / gameTime.ElapsedGameTime.TotalSeconds;
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.R)) {
                player.x = 10;
                player.y = 10;
                player.velocityX = 0;
                player.velocityY = 0;
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

            
            _spriteBatch.Draw(playerSprite, new Rectangle((int)player.x, (int)player.y, (int)(0.9 * worldContext.pixelsPerBlock), 2 * worldContext.pixelsPerBlock), Color.White);
            
            
             /* A segment of code that draws an outline around all the blocks considered for the players collision
            int entityLocationInGridX = (int)Math.Floor(player.x / worldContext.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(player.y / worldContext.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)player.collider.Width / worldContext.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)player.collider.Height / worldContext.pixelsPerBlock);
            int p = worldContext.pixelsPerBlock;
            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    Rectangle entityCollider = new Rectangle((int)player.x, (int)player.y, player.collider.Width, player.collider.Height);

                    Rectangle blockRect = new Rectangle(x * p, y * p, p, p);
                    if (blockRect.Intersects(entityCollider) && worldContext.worldArray[x,y] != 0)
                    {
                        
                        _spriteBatch.Draw(collisionSprite, new Rectangle(x * p, y * p, p, p), Color.White);
                    }
                        _spriteBatch.Draw(playerSprite, new Rectangle(x * p, y * p, p, 2), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle(x * p, y * p, 2, p), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle(x * p, (y + 1) * p, p, 2), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle((x + 1) * p, y * p, 2, p), Color.White);
                }
            }
            // */

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
            for (int i = 24; i < 32; i++) {
                worldArray[i, 17] = 1;
            }
            //for (int i = 28; i < 32; i++)
            //{
               // worldArray[i, 18] = 1;
            //}
            //initialise the array with a z shape of 1s, to represent flat ground then a drop to another flat piece
        }
    }

    public class PhysicsEngine
    {
        //Current bug: When the player collides with a block horizontally, there's a singular frame where they aren't colliding
        //anymore, allowing this annoying rubberbanding visual. Using a basic visual of the collision, it is considered to be
        //colliding with the horizontal block. So it's something else weird.
        bool helpDebug = false;
        public double blockSizeInMeters { get; } = 0.6; //The pixel size in meters can be found by taking this value and dividing it by pixelsPerBlock
        WorldContext wc;

        int horizontalOverlapMin = 5;
        int verticalOverlapMin = 5;


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

            System.Diagnostics.Debug.WriteLine(entity.accelerationY * timeElapsed);
            
            if (helpDebug) { System.Diagnostics.Debug.WriteLine(entity.velocityX); }
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
            System.Diagnostics.Debug.WriteLine(entity.velocityY + ", " + entity.accelerationY);
            entity.updateLocation(entity.velocityX * timeElapsed * (wc.pixelsPerBlock/blockSizeInMeters), -entity.velocityY * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters));
        }


        public void detectBlockCollisions(PhysicsObject entity) {
            helpDebug = false;
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
                                (double x, double y) collisionNormal = computeCollisionNormal(entityCollider, blockRect);


                                //If the signs are unequal on either the velocity or the acceleration then the forces should cancel as the resulting motion would be counteracted by the block
                                if (((Math.Sign(collisionNormal.y) != Math.Sign(entity.velocityY) && entity.velocityY != 0) || (Math.Sign(collisionNormal.y) != Math.Sign(entity.accelerationY) && entity.accelerationY != 0)) && collisionNormal.y != 0)
                                {
                                    entity.velocityY -= (1 + entity.bounceCoefficient) * entity.velocityY;
                                    entity.accelerationY -= entity.accelerationY;

                                    if (Math.Sign(collisionNormal.y) > 0) {
                                        entity.isOnGround = true;
                                    }

                                    if (Math.Sign(collisionNormal.y) > 0)
                                    {
                                        entity.y = blockRect.Y - entityCollider.Height + 1;
                                    }
                                    else {
                                        entity.y = blockRect.Bottom - 1;
                                    }
                                }
                                
                                if(((Math.Sign(collisionNormal.x) != Math.Sign(entity.velocityX) && entity.velocityX != 0) || (Math.Sign(collisionNormal.x) != Math.Sign(entity.accelerationX ) && entity.accelerationX != 0)) && collisionNormal.x != 0)
                                {
                                    helpDebug = true;
                                    System.Diagnostics.Debug.WriteLine(entity.x + ", " + entity.velocityX + ", " + entity.accelerationX);

                                    entity.velocityX -= (1 + entity.bounceCoefficient) * entity.velocityX;
                                    entity.accelerationX -= entity.accelerationX;
                                    System.Diagnostics.Debug.WriteLine(entity.x + ", " + entity.velocityX + ", " + entity.accelerationX);
                                    if (Math.Sign(collisionNormal.x) > 0)
                                    {
                                        entity.x = blockRect.Right -  1;
                                    }
                                    else
                                    {
                                        entity.x = blockRect.Left - entityCollider.Width + 1;
                                    }
                                    System.Diagnostics.Debug.WriteLine(entity.x);
                                }
                                
                            }
                        }
                    }
                }
            }

        }

        public (double, double) computeCollisionNormal(Rectangle entityCollider, Rectangle blockRect) {
            (double x, double y) collisionNormal = (0, 0);
            (int x, int y) approximateCollisionDirection = (entityCollider.Center.X - blockRect.Center.X, entityCollider.Center.Y - blockRect.Center.Y);
            
            if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Right from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {
                   
                    if (verticalOverlap > horizontalOverlap)
                    {
                        
                        return (-1, 0);
                    }
                    else
                    {
                        return (0, 1);
                    }
                }
            } else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Left from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {
                   
                    if (verticalOverlap > horizontalOverlap)
                    {
                        
                        return (1, 0);
                    }
                    else
                    {
                        return (0, 1);
                    }
                }
            } else if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y >= 0)
            { //Top Right from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {
                    

                    if (verticalOverlap > horizontalOverlap)
                    {
                        return (-1, 0);
                    }
                    else
                    {
                        return (0, -1);
                    }
                }
            }
            else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y >= 0)
            { //Top Left from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {
                    
                    if (verticalOverlap > horizontalOverlap)
                    {
                        
                        return (1, 0);
                    }
                    else
                    {
                        return (0, -1);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("Has returned a (0,0) collision normal from " + approximateCollisionDirection.x + ", " + approximateCollisionDirection.y);
            return collisionNormal;
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
            minVelocityY = 0.01;
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


