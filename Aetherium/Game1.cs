//Author: Mark Rozin 
//File Name: Game1.cs 
//Project Name: Aetherium
//Creation Date: Dec. 14, 2023
//Modified Date: Jan. 21, 2024 
//Description: Battle adventure game with objective of capturing all 3 keys and finding gem.

//loops - had A LOT of array - loop combinations, including zombie interactions, zombie spawning, platform collisions, etc.
//arrays - array of for all zombie properties, platform rectagnles, spike rectangles, item counts, item prices, etc.
//selection - had various switch statements, most notably inventory selection, and loading into platform levels
//methods - had methods for various draw commands, randomizing boulders, and platforming collisions

using GameUtility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Aetherium
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        static Random rng = new Random();

        //Define gamestate constants
        const int STORY = 0;
        const int ARENA1 = 1;
        const int KEY1 = 2;
        const int ENDGAME = 3;
        const int SHOP = 10;
        const int LUCK_CARDS = 13;
        const int BALLROOM = 14;
        const int INSTRUCTIONS = 15;

        //Define moving constants
        const int STOPPED = 0;
        const int POSITIVE = 1;
        const int NEGATIVE = -1;

        //Define constants for collision recs for the platformer
        const int FEET = 0;
        const int HEAD = 1;
        const int LEFT = 2;
        const int RIGHT = 3;

        //Define the directions x and y for all gamestates with movement (no y for key gamestate because its platformer)
        int dirXPlayer = POSITIVE;
        int dirYPlayer = POSITIVE;
        int ballroomDirXPlayer = POSITIVE;
        int ballroomDirYPlayer = POSITIVE;
        int dirXKeyPlayer = POSITIVE;

        //Set player points, keys acquired, and amount of items to 0
        int playerPoints = 0;
        int keyCount = 0;
        int[] itemCounts = { 0, 0, 0, 0, 0 };

        //Set all the prices for items in the shop
        //(this is for the cheat)
        //int[] itemPrices = { 10, 10, 10, 10, 10 };
        int[] itemPrices = { 100, 70, 30, 60, 40 };

        //Set how much healing each potion gives you
        int smallPotionAddition = 20;
        int mediumPotionAddition = 40;
        int bigPotionAddition = 60;

        //Set which side the boulders start on
        int bouldersSide = 2;

        //Set the initial lucky card value 
        int luckyCard = rng.Next(1, 3);

        //Define screen dimensions
        int screenWidth;
        int screenHeight;

        //Define initial gamestate
        int gameState = STORY;

        //Set all the zombie healths and player health to 100
        int playerHealth = 100;
        int[] zombieHealths = { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };

        //Set the remainder for the zombie spawns
        int iRemainder;

        //Set the multiplier for all the text info boxes in the shop
        int shopTextLocsMultiplier;

        //Set the amount of points added and amount of damage dealt with each hit
        int pointsAddition = 30;
        int hitDamage = 40;

        //Set the initial inventory slot to middle (2 on the array)
        int inventorySelectedCount = 2;

        //Set all the X and Y directions for all the boulders (Y is the same, just doesn't move)
        int dirXBoulder1 = NEGATIVE;
        int dirXBoulder2 = POSITIVE;
        int dirXBoulder3 = NEGATIVE;
        int dirYAllBoulders = STOPPED;

        //Set inital state to button not pressed, game not over, player grounded, key not captured, and key not captured
        bool buttonPressed = false;
        bool gameOver = false;
        bool grounded = true;
        bool keyCaptured = false;

        //Set inital state to not being hit by a boulder or a zombie
        bool boulderIntersection = false;
        bool playerHit = false;

        //Set inital state to zombies not frozen, haven't hit void, and haven't collided with monkeybomb
        bool[] zombieVoidCollisionChecks = { false, false, false, false, false, false, false, false, false, false };
        bool[] zombieFrozenCheck = { false, false, false, false, false, false, false, false, false, false };
        bool[] zombieMonkeyBombCollisionCheck = { false, false, false, false, false, false, false, false, false, false };

        //Define zombies active array (to check if they're active)
        bool[] zombiesActive = new bool[10];

        //Set that they haven't failed a buy or hovered over a box to all the items
        bool[] buyFail = { false, false, false, false, false, false };
        bool[] shopInfoBoxes = { false, false, false, false, false, false };

        //Set the freezeSplash and monkey bomb to not active and not finished
        bool freezeSplashFinished = false;
        bool freezeSplashActive = false;
        bool monkeyBombActive = false;
        bool monkeyBombTriggered = false;

        //Set that they haven't failed a portal or opening a cage initially or hovered over either
        bool failedPortal = false;
        bool failedCageAttempt = false;
        bool hoverCage = false;
        bool portalHover = false;

        //Define that they haven't tried to max out their health yet
        bool maxHealth;

        //Define that they haven't doubled their points or turned over either card or finished the luck cards
        bool pointsDoubled = false;
        bool leftCardOpened = false;
        bool rightCardOpened = false;
        bool luckCardsOver = false;

        //Set the doulbe points and instakill to not appeared or active or finished or expired
        bool doublePointsAppeared = false;
        bool instaKillAppeared = false;
        bool doublePointsActive = false;
        bool instaKillActive = false;
        bool doublePointsFinished;
        bool instaKillFinished;
        bool doublePointsExpired = false;
        bool instaKillExpired = false;

        //Define the background music and sound effects
        Song bgMusic;
        SoundEffect slashSnd;

        //Store the maximum speed of translation per second for all speeds and also jumpspeed for platformer
        float maxSpeedPlayer = 130f;
        float ballroomMaxSpeedPlayer = 130f;
        float playerKeyMaxSpeed = 80f;
        float jumpSpeed = -5.5f;

        //Define gravity, acceleration, and friction constant
        const float GRAVITY = 9.8f / 60;
        const float ACCEL = 5f;
        const float FRICTION = ACCEL * 0.5f;

        //Define the mouse, keyboard, and previous keyboard
        MouseState mouse;
        KeyboardState kb;
        KeyboardState prevKb;

        //Establish fonts in the game
        SpriteFont storyFont;
        SpriteFont instructionsFont;
        SpriteFont titleFont;
        SpriteFont hudFont;

        //Define all rectangles used in the game
        Rectangle[] spikeRecs = new Rectangle[7];
        Rectangle[] shopTextRecs = new Rectangle[6];
        Rectangle[] platformRecs = new Rectangle[5];
        Rectangle[] zombieRecs = new Rectangle[10];
        Rectangle[] playerCollisionRecs = new Rectangle[4];
        Rectangle[] zombieBlankHealthBarRecs = new Rectangle[10];
        Rectangle[] zombieHealthBarRecs = new Rectangle[10];
        Rectangle[] inventoryRecs = new Rectangle[5];
        Rectangle[] zombieSpawnRecs = new Rectangle[4];
        Rectangle playerHealthBarRec;
        Rectangle playerBlankHealthBarRec;
        Rectangle playerKeyHealthBarRec;
        Rectangle playerKeyBlankHealthBarRec;
        Rectangle[] shopItemRecs = new Rectangle[6];
        Rectangle exitRec;
        Rectangle crystalRec;
        Rectangle doublePointsRec;
        Rectangle instaKillRec;
        Rectangle cageRec;
        Rectangle monkeyBombRec;
        Rectangle boulder1Rec;
        Rectangle boulder2Rec;
        Rectangle boulder3Rec;
        Rectangle terrainRec;
        Rectangle playerRec;
        Rectangle portalRec;
        Rectangle playerKeyRec;
        Rectangle[] ballroomPortals = new Rectangle[2];
        Rectangle restartButtonRec;
        Rectangle getawayPortalRec;
        Rectangle ballroomPlayerRec;
        Rectangle keyArenaRec;
        Rectangle goldRec;
        Rectangle leftCardRec;
        Rectangle rightCardRec;
        Rectangle voidRec;
        Rectangle keyRec;

        //Define the distances between zombies and: the void, monkeybomb, freezeSplash, player
        double[] monkeyBombDistances = new double[10];
        double[] zombieDistances = new double[10];
        double[] voidDistances = new double[10];
        double[] freezeSplashDistances = new double[10];

        //Define the zombie speeds
        float[] zombieSpeeds = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1 };

        //Define the text locations 
        Vector2[] shopTextLocs = new Vector2[6];
        Vector2 instructionsTitleLoc;
        Vector2 instaKillTitleLoc;
        Vector2 shopTitleLoc;
        Vector2 endGameTextLoc;
        Vector2 pointsTextLoc;
        Vector2 failedPortalTextLoc;
        Vector2 hoverCageTextLoc;
        Vector2 cageFailedTextLoc;
        Vector2[] shopInfoTextLocs = new Vector2[24];
        Vector2[] inventoryCountTextLocs = new Vector2[5];
        Vector2 keyCountTextLoc;
        Vector2[] endGameTextLocs = new Vector2[2];
        Vector2[] instructionsTextLocs = new Vector2[7];
        Vector2[] storyTextLocs = new Vector2[5];

        //Set the zombie, player (in every state), freeze splash, boulders, monkeyBomb
        Vector2[] zombiesPos = new Vector2[10];
        Vector2 freezeSplashPos;
        Vector2 playerKeyPos;
        Vector2 boulder1Pos;
        Vector2 boulder2Pos;
        Vector2 boulder3Pos;
        Vector2 ballroomPlayerPos;
        Vector2 monkeyBombPos;
        Vector2 playerPos;

        //Define the zombie directions (x and y)
        Vector2 zombieDir;

        //Define the boulder speeds, player speeds (in every state), and the forces (friction and gravity) in the platformer and set them accordingly
        Vector2 speedBoulder1 = new Vector2(STOPPED, STOPPED);
        Vector2 speedBoulder2 = new Vector2(STOPPED, STOPPED);
        Vector2 speedBoulder3 = new Vector2(STOPPED, STOPPED);
        Vector2 playerKeySpeed = new Vector2(3f, 0f);
        Vector2 forces = new Vector2(FRICTION, GRAVITY);
        Vector2 speedPlayer = new Vector2(POSITIVE, POSITIVE);
        Vector2 ballroomSpeedPlayer = new Vector2(POSITIVE, POSITIVE);

        //Define all strings being used in the string
        const string ENDGAME_TEXT = "DEATH";
        const string COUNT_TEXT = "COUNT: ";
        const string SHOP_TEXT = "SHOP";
        string[] shopInfoTexts =
            {
        "Large Potion",
        "Cost: 100 GOLD",
        "",
        "Heals 60 Health",

        "Medium Potion",
        "Cost: 70 GOLD",
        "",
        "Heals 40 Health",

        "Small Potion",
        "Cost: 30 GOLD",
        "",
        "Heals 20 Health",

        "Monkey Bomb",
        "Cost: 60 GOLD",
        "Lures zombies",
        "and explodes",

        "Gamble",
        "Cost: FREE",
        "50/50 - Double",
        "gold or lose it all",

        "Freeze Splash",
        "Cost: 40 GOLD",
        "Click to freeze ",
        "nearby zombies",
        };
        string[] storyTexts =
        {
            "Eldoria is overrun by zombies, and only you can save it! ",
            "The fabled Lumina Crystal, a legendary magical gem,",
            "holds the key to reversing the apocalypse. ",
            "Embark on a daring quest, be the hero Eldoria needs",
            "and restore this once-thriving land with the power of the Lumina Crystal."
        };
        string endgameMessage1 = "You did it! You found the Lumina Crystal,";
        string endgameMessage2 = "and now Eldoria can rest in peace!";
        string portalFailedText = "NOT ENOUGH GOLD - 800 GOLD NEEDED";
        string cageFailedText = "NEEDS ALL 3 KEYS TO BE OPENED";

        //Define all the images and backgrounds used in the game
        Texture2D terrainBg;
        Texture2D whiteBg;

        Texture2D playerImg;
        Texture2D zombieImg;
        Texture2D portalImg;
        Texture2D boulderImg;
        Texture2D potionImg;
        Texture2D playingCardImg;
        Texture2D whiteAreaImg;
        Texture2D fullBagImg;
        Texture2D emptyBagImg;
        Texture2D voidImg;
        Texture2D platformImg;
        Texture2D monkeyBombImg;
        Texture2D healthBarBlankImg;
        Texture2D keyImg;
        Texture2D spikeImg;
        Texture2D blankSquareImg;
        Texture2D doublePointsImg;
        Texture2D instaKillImg;
        Texture2D goldImg;
        Texture2D restartButtonImg;
        Texture2D cageImg;
        Texture2D crystalImg;
        Texture2D exitImg;
        Texture2D freezeSplashImg;
        Texture2D luckCardsImg;

        //Define the max speed of the boulders
        float maxSpeedBoulders = 200f;

        //Define all timers used in the game
        Timer boulderInvincibilityTimer;
        Timer monkeyBombTimer;
        Timer zombieSpawnTimer;
        Timer boulderTimer;
        Timer freezeSplashTimer;
        Timer playerAttackCooldownTimer;
        Timer[] zombieVoidCooldownTimers = new Timer[10];
        Timer spikeCooldownTimer;
        Timer[] shopBuyingCooldownTimers = new Timer[6];
        Timer[] zombieAttackCooldownTimers = new Timer[10];
        Timer doublePointsActiveTimer;
        Timer instaKillActiveTimer;
        Timer doublePointsGapTimer;
        Timer instaKillGapTimer;
        Timer doublePointsAppearedTimer;
        Timer instaKillAppearedTimer;
        Timer instructionsPreviewCooldown;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            //Set mouse to visible in specific gamestates
            if ((gameState == LUCK_CARDS) || (gameState == ARENA1) || (gameState == STORY) || (gameState == SHOP) || gameState == ENDGAME || (gameState == INSTRUCTIONS) || gameOver == true)
            {
                IsMouseVisible = true;
            }

            //Store the preferred screen width and height into variables
            screenWidth = graphics.PreferredBackBufferWidth;
            screenHeight = graphics.PreferredBackBufferHeight;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            //Load in all the fonts in the game
            storyFont = Content.Load<SpriteFont>("Fonts/StoryFont");
            titleFont = Content.Load<SpriteFont>("Fonts/TitleFont");
            hudFont = Content.Load<SpriteFont>("Fonts/HUDFont");
            instructionsFont = Content.Load<SpriteFont>("Fonts/RealInstructionsFont");

            //Load in all the images in the game
            terrainBg = Content.Load<Texture2D>("Images/Sprites/greenTerrain");
            playerImg = Content.Load<Texture2D>("Images/Sprites/minecraftKnight");
            zombieImg = Content.Load<Texture2D>("Images/Sprites/zombieFlat3");
            portalImg = Content.Load<Texture2D>("Images/Sprites/portal2");
            boulderImg = Content.Load<Texture2D>("Images/Sprites/boulder");
            potionImg = Content.Load<Texture2D>("Images/Sprites/potion7");
            playingCardImg = Content.Load<Texture2D>("Images/Sprites/playingCard");
            whiteAreaImg = Content.Load<Texture2D>("Images/Sprites/RectangleBorder");
            fullBagImg = Content.Load<Texture2D>("Images/Sprites/fullBag");
            emptyBagImg = Content.Load<Texture2D>("Images/Sprites/emptyBag");
            voidImg = Content.Load<Texture2D>("Images/Sprites/void3");
            platformImg = Content.Load<Texture2D>("Images/Sprites/platformRec");
            monkeyBombImg = Content.Load<Texture2D>("Images/Sprites/monkeyBomb");
            healthBarBlankImg = Content.Load<Texture2D>("Images/Sprites/rectangleBorder2Amazing");
            keyImg = Content.Load<Texture2D>("Images/Sprites/keyImg");
            freezeSplashImg = Content.Load<Texture2D>("Images/Sprites/freezeSplash");
            luckCardsImg = Content.Load<Texture2D>("Images/Sprites/luckCards");
            spikeImg = Content.Load<Texture2D>("Images/Sprites/spikeImg");
            blankSquareImg = Content.Load<Texture2D>("Images/Sprites/blankSquare");
            doublePointsImg = Content.Load<Texture2D>("Images/Sprites/doublePoints");
            instaKillImg = Content.Load<Texture2D>("Images/Sprites/instaKill2");
            goldImg = Content.Load<Texture2D>("Images/Sprites/goldImg");
            restartButtonImg = Content.Load<Texture2D>("Images/Sprites/restartButton");
            cageImg = Content.Load<Texture2D>("Images/Sprites/cageImg");
            whiteBg = Content.Load<Texture2D>("Images/Backgrounds/whiteBg");
            crystalImg = Content.Load<Texture2D>("Images/Sprites/Crystal");
            exitImg = Content.Load<Texture2D>("Images/Sprites/exitButton");

            //Load in the audio used in the game
            bgMusic = Content.Load<Song>("Audio/Music/01 - Damned");
            slashSnd = Content.Load<SoundEffect>("Audio/Sounds/slash-21834");

            //Set master volume to 2%, make it repeat, and play it
            MediaPlayer.Volume = 0.02f;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(bgMusic);

            //Set all the timers used in the game, most start off as false, some of them like the cooldowns will be true so that the cooldown doesn't happen on first instance
            boulderTimer = new Timer(6000, false);
            monkeyBombTimer = new Timer(5000, false);
            zombieSpawnTimer = new Timer(2000, true);
            freezeSplashTimer = new Timer(6000, false);
            playerAttackCooldownTimer = new Timer(800, true);
            boulderInvincibilityTimer = new Timer(3000, false);
            doublePointsActiveTimer = new Timer(10000, false);
            instaKillActiveTimer = new Timer(5000, false);
            doublePointsGapTimer = new Timer(rng.Next(20000, 30000), true);
            instaKillGapTimer = new Timer(rng.Next(40000, 50000), true);
            doublePointsAppearedTimer = new Timer(4000, false);
            instaKillAppearedTimer = new Timer(3000, false);
            spikeCooldownTimer = new Timer(2000, true);
            instructionsPreviewCooldown = new Timer(3000, true);

            //Setting the array of timers but using a loop for efficiency
            for (int h = 0; h <= 5; h++)
            {
                shopBuyingCooldownTimers[h] = new Timer(300, true);
            }
            for (int h = 0; h <= 9; h++)
            {
                zombieVoidCooldownTimers[h] = new Timer(5000, true);
            }
            for (int q = 0; q <= 9; q++)
            {
                zombieAttackCooldownTimers[q] = new Timer(rng.Next(1500, 2100), true);
            }

            //Setting all rectangles used int he game
            crystalRec = new Rectangle((screenWidth / 2) - ((int)(crystalImg.Width * 0.6) / 2), 80, (int)(crystalImg.Width * 0.6), (int)(crystalImg.Height * 0.6));
            goldRec = new Rectangle(600, 30, (int)(goldImg.Width * 0.03), (int)(goldImg.Height * 0.03));
            keyArenaRec = new Rectangle(600, 80, (int)(goldImg.Width * 0.03), (int)(goldImg.Height * 0.03));
            doublePointsRec = new Rectangle(1000, 500, (int)(doublePointsImg.Width * 0.2), (int)(doublePointsImg.Height * 0.2));
            instaKillRec = new Rectangle(1000, 500, (int)(doublePointsImg.Width * 0.2), (int)(doublePointsImg.Height * 0.2));
            ballroomPortals[0] = new Rectangle((screenWidth / 2) - (portalImg.Width / 20), 10, (portalImg.Width / 10), (portalImg.Height / 10));
            ballroomPortals[1] = new Rectangle((screenWidth / 2) - (portalImg.Width / 20), 470 - (portalImg.Height / 10), (portalImg.Width / 10), (portalImg.Height / 10));
            exitRec = new Rectangle(20, 20, (int)(exitImg.Width * 0.09), (int)(exitImg.Height * 0.09));
            restartButtonRec = new Rectangle(1000, 500, (int)(restartButtonImg.Width * 0.2), (int)(restartButtonImg.Height * 0.2));
            cageRec = new Rectangle((screenWidth / 2) - ((int)(cageImg.Width * 0.05) / 2), 220, (int)(cageImg.Width * 0.05), (int)(cageImg.Height * 0.05));
            terrainRec = new Rectangle(0, 0, screenWidth, screenHeight);
            playerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
            platformRecs[0] = new Rectangle(0, 300, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
            platformRecs[1] = new Rectangle(200, 250, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
            platformRecs[2] = new Rectangle(400, 350, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
            platformRecs[3] = new Rectangle(600, 270, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
            playerKeyRec = new Rectangle(10, platformRecs[0].Y - playerKeyRec.Height, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
            ballroomPlayerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
            portalRec = new Rectangle(10, 10, (portalImg.Width / 10), (portalImg.Height / 10));
            shopItemRecs[0] = new Rectangle((screenWidth / 4) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) - 60, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            shopItemRecs[1] = new Rectangle((2 * (screenWidth / 4)) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) - 60, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            shopItemRecs[2] = new Rectangle((3 * (screenWidth / 4)) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) - 60, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            shopItemRecs[3] = new Rectangle((screenWidth / 4) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) + 120, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            shopItemRecs[4] = new Rectangle((2 * (screenWidth / 4)) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) + 120, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            shopItemRecs[5] = new Rectangle((3 * (screenWidth / 4)) - (int)(potionImg.Width * 0.125), (screenHeight / 4) + (int)(potionImg.Width * 0.125) + 120, (int)(potionImg.Width * 0.25), (int)(potionImg.Height * 0.25));
            boulder1Rec = new Rectangle(screenWidth, 60 - ((int)(boulderImg.Height * 0.2)) / 2, (int)(boulderImg.Width * 0.2), (int)(boulderImg.Height * 0.2));
            boulder2Rec = new Rectangle(0 - (int)(boulderImg.Width * 0.2), 240 - ((int)(boulderImg.Height * 0.2)) / 2, (int)(boulderImg.Width * 0.2), (int)(boulderImg.Height * 0.2));
            boulder3Rec = new Rectangle(screenWidth, 420 - ((int)(boulderImg.Height * 0.2)) / 2, (int)(boulderImg.Width * 0.2), (int)(boulderImg.Height * 0.2));
            leftCardRec = new Rectangle((int)(playingCardImg.Width * 0.7) - 80, 100, (int)(playingCardImg.Width * 0.7), (int)(playingCardImg.Height * 0.7));
            rightCardRec = new Rectangle(screenWidth - ((int)(playingCardImg.Width * 0.7) * 2) + 80, 100, (int)(playingCardImg.Width * 0.7), (int)(playingCardImg.Height * 0.7));
            voidRec = new Rectangle(((screenWidth / 2) - (((int)(voidImg.Width * 0.1)) / 2) - 20), (screenHeight / 2) - (((int)(voidImg.Height * 0.1)) / 2), (int)(voidImg.Width * 0.1), (int)(voidImg.Height * 0.1));
            spikeRecs[0] = new Rectangle(platformRecs[0].X + 80, platformRecs[0].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
            spikeRecs[1] = new Rectangle(platformRecs[1].X + 30, platformRecs[1].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
            spikeRecs[2] = new Rectangle(platformRecs[2].X + 70, platformRecs[2].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
            spikeRecs[3] = new Rectangle(platformRecs[3].X + 30, platformRecs[3].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
            zombieSpawnRecs[0] = new Rectangle((screenWidth / 2) - (((int)(zombieImg.Width * 0.08)) / 2), 0, (int)(zombieImg.Width * 0.08), (int)(zombieImg.Height * 0.08));
            zombieSpawnRecs[1] = new Rectangle(0, (screenHeight / 2) - (((int)(zombieImg.Height * 0.08)) / 2), (int)(zombieImg.Width * 0.08), (int)(zombieImg.Height * 0.08));
            zombieSpawnRecs[2] = new Rectangle((screenWidth / 2), (screenHeight - (int)(playerImg.Height * 0.08)), (int)(zombieImg.Width * 0.08), (int)(zombieImg.Height * 0.08));
            zombieSpawnRecs[3] = new Rectangle(screenWidth - ((int)(zombieImg.Width * 0.08)), (screenHeight / 2) - (((int)(zombieImg.Height * 0.08)) / 2), (int)(zombieImg.Width * 0.08), (int)(zombieImg.Height * 0.08));
            keyRec = new Rectangle(platformRecs[3].X + (platformRecs[3].Width / 2) - ((int)(keyImg.Width * 0.1) / 2) + 40, platformRecs[3].Y - ((int)(keyImg.Height * 0.1)) - 20, (int)(keyImg.Width * 0.1), (int)(keyImg.Height * 0.1));
            getawayPortalRec = new Rectangle(1000, 500, (int)(portalImg.Width * 0.1), (int)(portalImg.Height * 0.1));

            //Set the text locations (measuring the string when it needs to be centered on the screen)
            endGameTextLocs[0] = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString(endgameMessage1).X / 2), 350);
            endGameTextLocs[1] = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString(endgameMessage2).X / 2), 410);
            hoverCageTextLoc = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString("Press SPACE To Open").X / 2), 100);
            cageFailedTextLoc = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString(cageFailedText).X / 2), 300);
            instructionsTitleLoc = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString("INSTRUCTIONS").X / 2), 50);
            instaKillTitleLoc = new Vector2((screenWidth / 2) - (instructionsFont.MeasureString("INSTRUCTIONS").X / 2), 100);
            shopTitleLoc = new Vector2(30, 25);
            pointsTextLoc = new Vector2(680, 42);
            keyCountTextLoc = new Vector2(680, 92);
            endGameTextLoc = new Vector2((screenWidth / 2) - ((titleFont.MeasureString(ENDGAME_TEXT).X / 2)), 410);
            failedPortalTextLoc = new Vector2((screenWidth / 2) - ((instructionsFont.MeasureString(portalFailedText).X / 2)), 220);

            //Setting text array locations but using loops for efficiency (different math applied to each to make indexes match locations)
            for (int d = 0; d <= 6; d++)
            {
                instructionsTextLocs[d] = new Vector2(20, 100 + (50 * d));

            }
            for (int e = 0; e <= 5; e++)
            {
                shopTextRecs[e] = new Rectangle(shopItemRecs[e].X, shopItemRecs[e].Y - 40, (int)(whiteAreaImg.Width * 0.4) + 15, (int)(whiteAreaImg.Height * 0.2));
            }

            //Putting the text locations on the text rectangles
            for (int z = 0; z <= 5; z++)
            {
                shopTextLocs[z].X = ((float)(shopTextRecs[z].X) + 15);
                shopTextLocs[z].Y = ((float)(shopTextRecs[z].Y) + 8);
            }

            //Setting text locs and also the inventory recs inside because its the same number so more efficient
            for (int z = 0; z <= 4; z++)
            {
                inventoryRecs[z] = new Rectangle(470 + (int)(blankSquareImg.Width * 0.2 * z), 400, (int)(blankSquareImg.Width * 0.2), (int)(blankSquareImg.Height * 0.2));
                inventoryCountTextLocs[z] = new Vector2(inventoryRecs[z].X + 5, inventoryRecs[z].Y);
                storyTextLocs[z] = new Vector2(40, 45 + (90 * z));
            }
            for (int r = 0; r <= 23; r++)
            {
                shopTextLocsMultiplier = ((2 * ((r + 4) % 4)) + 1);
                if (shopTextLocsMultiplier > 4)
                {
                    shopTextLocsMultiplier += 2;
                }
                shopInfoTextLocs[r] = new Vector2((shopItemRecs[(int)((Math.Floor((double)(r / 4))))].X + 15), (shopItemRecs[(int)((Math.Floor((double)(r / 4))))].Y + (8 * shopTextLocsMultiplier)));
            }

            //Setting the positions equal to the rectangle locations
            playerPos = new Vector2(playerRec.X, playerRec.Y);
            playerKeyPos = new Vector2(playerKeyRec.X, playerKeyRec.Y);
            ballroomPlayerPos = new Vector2(ballroomPlayerRec.X, ballroomPlayerRec.Y);
            boulder1Pos = new Vector2(boulder1Rec.X, boulder1Rec.Y);
            boulder2Pos = new Vector2(boulder2Rec.X, boulder2Rec.Y);
            boulder3Pos = new Vector2(boulder3Rec.X, boulder3Rec.Y);

            //Still setting the position equal to the rectangle locs but using loop for efficiency 
            for (int i = 0; i <= 9; i++)
            {
                zombiesPos[i] = new Vector2(zombieRecs[i].X, zombieRecs[i].Y);
            }

            //Set platforming collision recs
            SetPlayerRecs();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // TODO: Add your update logic here

            //Defining mouse, keyboard, and the previous kb (last millisecond)
            mouse = Mouse.GetState();
            prevKb = kb;
            kb = Keyboard.GetState();

            //Updating all timers in gametime
            boulderTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            monkeyBombTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            boulderInvincibilityTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            zombieSpawnTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            freezeSplashTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            playerAttackCooldownTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            spikeCooldownTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            doublePointsActiveTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            doublePointsGapTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            doublePointsAppearedTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            instaKillActiveTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            instaKillGapTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            instaKillAppearedTimer.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            instructionsPreviewCooldown.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            for (int k = 0; k <= 9; k++)
            {
                zombieAttackCooldownTimers[k].Update(gameTime.ElapsedGameTime.TotalMilliseconds);
                zombieVoidCooldownTimers[k].Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            }
            for (int a = 0; a <= 5; a++)
            {
                shopBuyingCooldownTimers[a].Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            //Using a button pressed bool for simplicity (checks if button is pressed)
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                buttonPressed = true;
            }
            if (mouse.LeftButton != ButtonState.Pressed)
            {
                buttonPressed = false;
            }

            //Checking gamestate
            switch (gameState)
            {
                //Story gamestate
                case STORY:
                    //If my the preview cooldown is done, then they can press the button, and if they do they go to instructions. Reset timer for instructions
                    if (instructionsPreviewCooldown.IsFinished() == true)
                    {
                        if (buttonPressed == true)
                        {
                            gameState = INSTRUCTIONS;
                            instructionsPreviewCooldown.ResetTimer(true);
                        }
                    }
                    break;

                //Instructions gamestate
                case INSTRUCTIONS:

                    //If they hit escape, go to arena
                    if ((kb.IsKeyDown(Keys.Escape)) && (!prevKb.IsKeyDown(Keys.Escape)))
                    {
                        gameState = ARENA1;
                    }

                    //If the timer is finished, they can press the button, and if they do it goes to arena.
                    if (instructionsPreviewCooldown.IsFinished() == true)
                    {
                        if (buttonPressed == true)
                        {
                            instructionsPreviewCooldown.ResetTimer(true);
                            gameState = ARENA1;
                        }
                    }
                    break;

                //Arena gamestate
                case ARENA1:

                    //If they press the restart button, restart game
                    if ((buttonPressed == true) && (restartButtonRec.Contains(mouse.Position)))
                    {
                        RestartGame();
                    }

                    //If the game is over (they die), spawn in the restart button. While the game is still going, the restart button rests in the sandbox.
                    if (gameOver == true)
                    {
                        restartButtonRec = new Rectangle(350, 200, (int)(restartButtonImg.Width * 0.1), (int)(restartButtonImg.Height * 0.1));
                    }
                    else
                    {
                        restartButtonRec = new Rectangle(1000, 500, (int)(restartButtonImg.Width * 0.1), (int)(restartButtonImg.Height * 0.1));
                    }

                    //If they hit I, go to instructions
                    if (kb.IsKeyDown(Keys.I) && !prevKb.IsKeyDown(Keys.I))
                    {
                        gameState = INSTRUCTIONS;
                    }




                    //If the double points gap timer is finished and it hasn't appeared yet, bring the rectangle in, set it to appeared (so it doesn't come in here again), and start the appeared timer
                    if ((doublePointsGapTimer.IsFinished() == true) && (doublePointsAppeared == false))
                    {
                        doublePointsRec = new Rectangle(rng.Next(100, 700), rng.Next(0, 400), (int)(doublePointsImg.Width * 0.2), (int)(doublePointsImg.Height * 0.2));
                        doublePointsAppeared = true;
                        doublePointsAppearedTimer.ResetTimer(true);
                        doublePointsAppearedTimer.Activate();
                    }
                    //If they intersect it and its appeared and the gap timer's already finished, set double points active to true, double the points you get per kill, throw the rectangle in the sandbox, turn on the active timer, and set the finished variable to true (so it doesn't spam in the next if statement)
                    if (playerRec.Intersects(doublePointsRec) && (doublePointsAppeared == true) && (doublePointsGapTimer.IsFinished() == true))
                    {
                        doublePointsActive = true;
                        pointsAddition = 60;
                        doublePointsRec = new Rectangle(1000, 500, (int)(doublePointsImg.Width * 0.2), (int)(doublePointsImg.Height * 0.2));
                        doublePointsActiveTimer.ResetTimer(true);
                        doublePointsAppearedTimer.ResetTimer(false);
                        doublePointsFinished = true;
                    }
                    //If it hasn't expired and its appeared and the appeared timer is done, we set it to being expired and finished, and it hasn't appeared (it they miss it, none of the logic happens, but the timer still resets and everything)
                    if ((doublePointsExpired == false) && (doublePointsAppeared == true) && (doublePointsAppearedTimer.IsFinished() == true))
                    {
                        doublePointsFinished = true;
                        doublePointsExpired = true;
                        doublePointsAppeared = false;
                    }
                    //If the active timer or the appeared timer is finished AND THE DOUBLE POINTS IS FINISHED (happens in both cases), we revert everything back to normal (points back to 30, throw the rectangle in the sandbox, start the gap timer, and throw all the bools to false, so we can repeat when it finishes again)
                    if (((doublePointsActiveTimer.IsFinished() == true) || (doublePointsAppearedTimer.IsFinished() == true)) && (doublePointsFinished == true))
                    {
                        pointsAddition = 30;
                        doublePointsRec = new Rectangle(1000, 500, (int)(doublePointsImg.Width * 0.2), (int)(doublePointsImg.Height * 0.2));
                        doublePointsGapTimer.ResetTimer(true);
                        doublePointsGapTimer.Activate();
                        doublePointsActive = false;
                        doublePointsAppeared = false;
                        doublePointsFinished = false;
                        doublePointsExpired = false;

                    }

                    //If the insta kill gap timer is finished and it hasn't appeared yet, bring the rectangle in, set it to appeared (so it doesn't come in here again), and start the appeared timer
                    if ((instaKillGapTimer.IsFinished() == true) && (instaKillAppeared == false))
                    {
                        instaKillRec = new Rectangle(rng.Next(100, 700), rng.Next(0, 400), (int)(instaKillImg.Width * 0.05), (int)(instaKillImg.Height * 0.05));
                        instaKillAppeared = true;
                        instaKillAppearedTimer.ResetTimer(true);
                        instaKillAppearedTimer.Activate();
                    }

                    //If they intersect it and its appeared and the gap timer's already finished, set insta kill active to true, turn damage to 100, throw the rectangle in the sandbox, turn on the active timer, and set the finished variable to true (so it doesn't spam in the next if statement)
                    if (playerRec.Intersects(instaKillRec) && (instaKillAppeared == true) && (instaKillGapTimer.IsFinished() == true))
                    {
                        instaKillActive = true;
                        hitDamage = 100;

                        instaKillRec = new Rectangle(1000, 500, (int)(doublePointsImg.Width * 0.05), (int)(instaKillImg.Height * 0.05));
                        instaKillActiveTimer.ResetTimer(true);
                        instaKillAppearedTimer.ResetTimer(false);
                        instaKillFinished = true;
                    }
                    //If it hasn't expired and its appeared and the appeared timer is done, we set it to being expired and finished, and it hasn't appeared (it they miss it, none of the logic happens, but the timer still resets and everything)
                    if ((instaKillExpired == false) && (instaKillAppeared == true) && (instaKillAppearedTimer.IsFinished() == true))
                    {
                        instaKillFinished = true;
                        instaKillExpired = true;
                        instaKillAppeared = false;
                    }
                    //If the active timer or the appeared timer is finished AND THE DOUBLE POINTS IS FINISHED (happens in both cases), we revert everything back to normal (damage back to 40, throw the rectangle in the sandbox, start the gap timer, and throw all the bools to false, so we can repeat when it finishes again)
                    if (((instaKillActiveTimer.IsFinished() == true) || (instaKillAppearedTimer.IsFinished() == true)) && (instaKillFinished == true))
                    {
                        hitDamage = 40;
                        instaKillRec = new Rectangle(1000, 500, (int)(instaKillImg.Width * 0.05), (int)(instaKillImg.Height * 0.05));
                        instaKillGapTimer.ResetTimer(true);
                        instaKillGapTimer.Activate();
                        instaKillActive = false;
                        instaKillAppeared = false;
                        instaKillFinished = false;
                        instaKillExpired = false;

                    }

                    //If the zombie spawn timer is done (big timer to spawn zombies in)
                    if (zombieSpawnTimer.IsFinished() == true)
                    {
                        for (int j = 0; j <= 9; j++)
                        {
                            //If a zombie isn't active, we spawn it in
                            if (zombiesActive[j] == false)
                            {
                                //Just some math to calculate what rectangle to put in the zombie (we have 10 zombies and 4 positions, and each zombie, the position shifts)
                                iRemainder = (j + 4) % 4;

                                //Spawn the zombie in at the correct spawn rec
                                zombieRecs[j] = zombieSpawnRecs[iRemainder];

                                //Set the position equal to the rec, set it's active to true, reset its timer so it can loop again, and set its health back to 100
                                zombiesPos[j].X = zombieRecs[j].X;
                                zombiesPos[j].Y = zombieRecs[j].Y;
                                zombiesActive[j] = true;
                                zombieSpawnTimer.ResetTimer(true);
                                zombieHealths[j] = 100;

                                break;
                            }
                        }
                    }

                    //If freeze splash isn't ative and they're on the freeze splash slot AND they press the button, we say freeze splash isn't finished and we track the mouse's position
                    if (freezeSplashActive == false)
                    {
                        if (inventorySelectedCount == 4)
                        {
                            if ((buttonPressed == true) && (itemCounts[4] > 0))
                            {
                                freezeSplashFinished = false;
                                freezeSplashPos.X = mouse.Position.X;
                                freezeSplashPos.Y = mouse.Position.Y;
                            }
                        }
                    }

                    //Huge zombie cycling loop
                    for (int i = 0; i <= 9; i++)
                    {
                        //Calculating the distance from each zombie to the monkeybomb, void, player, and freezesplash using distance formula
                        monkeyBombDistances[i] = Math.Sqrt(Math.Pow((zombiesPos[i].X + (zombieRecs[i].Width / 2)) - (monkeyBombPos.X + (monkeyBombRec.Width / 2)), 2) + Math.Pow((zombiesPos[i].Y + (zombieRecs[i].Height / 2)) - (monkeyBombPos.Y + (monkeyBombRec.Height / 2)), 2));
                        voidDistances[i] = Math.Sqrt(Math.Pow((zombiesPos[i].X + (zombieRecs[i].Width / 2)) - (voidRec.X + (voidRec.Width / 2)), 2) + Math.Pow((zombiesPos[i].Y + (zombieRecs[i].Height / 2)) - (voidRec.Y + (voidRec.Height / 2)), 2));
                        freezeSplashDistances[i] = Math.Sqrt(Math.Pow((zombiesPos[i].X + (zombieRecs[i].Width / 2)) - (freezeSplashPos.X), 2) + Math.Pow((zombiesPos[i].Y + (zombieRecs[i].Height / 2)) - (freezeSplashPos.Y), 2));
                        zombieDistances[i] = Math.Sqrt(Math.Pow((zombiesPos[i].X + (zombieRecs[i].Width / 2)) - (playerPos.X + (playerRec.Width / 2)), 2) + Math.Pow((zombiesPos[i].Y + (zombieRecs[i].Height / 2)) - (playerPos.Y + (playerRec.Height / 2)), 2));

                        //If the zombie is alive
                        if (zombiesActive[i] == true)
                        {
                            //If their health is 0 or below (they die), set it to false and reset the timer, add points to the player too
                            if ((zombieHealths[i] <= 0))
                            {
                                zombiesActive[i] = false;
                                zombieSpawnTimer.ResetTimer(true);
                                playerPoints += pointsAddition;
                            }

                            //If the void cooldown is up and they're in it, you take 30 health away, and then reset it, this way it can't spam into this if statement and doesn't kill them instantly
                            if (zombieVoidCooldownTimers[i].IsFinished() == true)
                            {
                                if (voidDistances[i] < 60)
                                {
                                    zombieHealths[i] -= 30;
                                    zombieVoidCooldownTimers[i].ResetTimer(true);
                                }
                            }

                            //If the monkeybomb timer is finished and they're nearby it and monkey bomb is active, the zombie takes 40 damage
                            if ((monkeyBombTimer.IsFinished() == true) && (monkeyBombDistances[i] < 100) && (monkeyBombActive == true))
                            {
                                zombieHealths[i] -= 40;
                            }

                            //If the monkeybomb is active
                            if (monkeyBombActive == true)
                            {
                                //Set the zombies to be running to the monkey bomb
                                zombieDir = new Vector2((monkeyBombPos.X - 10) - zombiesPos[i].X, monkeyBombPos.Y - zombiesPos[i].Y);
                                zombieDir.Normalize();
                                zombiesPos[i].X += (zombieDir.X * zombieSpeeds[i]);
                                zombiesPos[i].Y += (zombieDir.Y * zombieSpeeds[i]);

                                //Put the rectangle on the screen and update its position
                                monkeyBombRec = new Rectangle((int)(monkeyBombPos.X), (int)(monkeyBombPos.Y), (int)(monkeyBombImg.Width * 0.08), (int)(monkeyBombImg.Height * 0.08));
                                monkeyBombRec.X = (int)(monkeyBombPos.X);
                                monkeyBombRec.Y = (int)(monkeyBombPos.Y);
                            }

                            //If monkeybomb isnt active (normal gameplay)
                            else
                            {
                                //Zombies chase player
                                zombieDir = new Vector2((playerPos.X - 7) - zombiesPos[i].X, playerPos.Y - zombiesPos[i].Y);
                                zombieDir.Normalize();
                                zombiesPos[i] += (zombieDir * zombieSpeeds[i]);

                            }

                            //Update zombie positions
                            zombieRecs[i].X = (int)(zombiesPos[i].X);
                            zombieRecs[i].Y = (int)(zombiesPos[i].Y);

                            //Update zombie health bars (the rectangle width corresponds to the zombie's health, so it updates in real time
                            zombieBlankHealthBarRecs[i] = new Rectangle((int)(zombiesPos[i].X - 10), (int)(zombiesPos[i].Y - 20), 100, 15);
                            zombieHealthBarRecs[i] = new Rectangle(zombieBlankHealthBarRecs[i].X, zombieBlankHealthBarRecs[i].Y, zombieHealths[i], 15);


                            //If the zombies are close to the freeze splash, and they're not already frozen, and the button's been pressed, and they're on the right inventory slot
                            if (freezeSplashDistances[i] < 130)
                            {
                                if (zombieFrozenCheck[i] == false)
                                {
                                    if (buttonPressed == true)
                                    {
                                        if ((inventorySelectedCount == 4))
                                        {
                                            //Zombies can't move, frozen splash is set to active, reset freeze splash timer, and set the frozen check to true for the specific zombie
                                            zombieSpeeds[i] = 0;
                                            freezeSplashTimer.ResetTimer(true);
                                            freezeSplashTimer.Activate();
                                            freezeSplashActive = true;
                                            zombieFrozenCheck[i] = true;
                                        }
                                    }
                                }
                            }

                            //If the freeze splash timer is finished
                            if ((freezeSplashTimer.IsFinished() == true))
                            {
                                //If the zombie's speed is at zero (is frozen), we turn the zombie speeds back to normal (use a little math for that), set the freeze splash to inactive, and the frozen check for that specific zombie is false too
                                if (zombieSpeeds[i] == 0)
                                {
                                    zombieSpeeds[i] = (((float)i + 1f) / 10f);
                                    zombieFrozenCheck[i] = false;
                                    freezeSplashActive = false;

                                    //If freeze splash is finished, we take one away from the count and set it to finished
                                    if (freezeSplashFinished == false)
                                    {
                                        itemCounts[4] -= 1;
                                        freezeSplashFinished = true;
                                    }

                                }
                            }
                            //If they hit space and the attack cooldown is done and they're close to the zombie
                            if ((kb.IsKeyDown(Keys.Space)) && (!prevKb.IsKeyDown(Keys.Space) && (playerAttackCooldownTimer.IsFinished() == true) && (zombieDistances[i] <= 100)))
                            {
                                //Make the zombie take the damage, reset the attack cooldown timer, and play the attack sound
                                slashSnd.CreateInstance().Play();
                                zombieHealths[i] -= hitDamage;
                                playerAttackCooldownTimer.ResetTimer(true);
                            }

                            //If the zombie attack cooldown is done and they're nearby (the hit range is closer for the zombie)
                            if ((zombieAttackCooldownTimers[i].IsFinished() == true) && (zombieDistances[i] <= 60))
                            {
                                //Zombie does 5 damage and reset the zombie cooldown timer
                                playerHealth -= 5;
                                playerHit = true;
                                zombieAttackCooldownTimers[i].ResetTimer(true);
                            }
                            else
                            {
                                playerHit = false;
                            }
                        }
                    }
                    //Update player health bar (the rectangle width corresponds to the zombie's health, so it updates in real time
                    playerBlankHealthBarRec = new Rectangle((int)(playerPos.X - 20), (int)(playerPos.Y - 20), 100, 15);
                    playerHealthBarRec = new Rectangle(playerBlankHealthBarRec.X, playerBlankHealthBarRec.Y, playerHealth, 15);



                    //Update player's speed
                    speedPlayer.X = dirXPlayer * (maxSpeedPlayer * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedPlayer.Y = dirYPlayer * (maxSpeedPlayer * (float)gameTime.ElapsedGameTime.TotalSeconds);

                    //Add the speeds to the every object's true position 
                    playerPos.X = playerPos.X + speedPlayer.X;
                    playerPos.Y = playerPos.Y + speedPlayer.Y;

                    //Set the every object's bounding box position equal to its true position
                    playerRec.X = (int)(playerPos.X);
                    playerRec.Y = (int)(playerPos.Y);


                    //Movement code
                    if (kb.IsKeyDown(Keys.W))
                    {
                        dirYPlayer = -1;
                    }
                    if (kb.IsKeyDown(Keys.S))
                    {
                        dirYPlayer = 1;
                    }
                    if (kb.IsKeyDown(Keys.A))
                    {
                        dirXPlayer = -1;
                    }
                    if (kb.IsKeyDown(Keys.D))
                    {
                        dirXPlayer = 1;
                    }
                    if (!kb.IsKeyDown(Keys.W) && (!(kb.IsKeyDown(Keys.S))))
                    {
                        dirYPlayer = 0;
                    }
                    if (!kb.IsKeyDown(Keys.A) && (!(kb.IsKeyDown(Keys.D))))
                    {
                        dirXPlayer = 0;
                    }

                    //If they scroll left, we take one away from inventory selected count (scroll left), but if we're already at the far left, it switches to right
                    if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left))
                    {
                        if (inventorySelectedCount == 0)
                        {
                            inventorySelectedCount = 4;
                        }
                        else
                        {
                            inventorySelectedCount -= 1;
                        }
                    }
                    //Same thing but with right
                    if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right))
                    {
                        if (inventorySelectedCount == 4)
                        {
                            inventorySelectedCount = 0;
                        }
                        else
                        {
                            inventorySelectedCount += 1;
                        }
                    }
                    //Clamp player's position to the screen width and height
                    playerPos.X = MathHelper.Clamp(playerPos.X, 0, screenWidth - playerRec.Width);
                    playerPos.Y = MathHelper.Clamp(playerPos.Y, 0, screenHeight - playerRec.Height);

                    //If they intersect the portal, they go to the ballroom 
                    if (playerRec.Intersects(portalRec))
                    {
                        gameState = BALLROOM;
                    }

                    //If they hit Q, they go to the shop
                    if ((kb.IsKeyDown(Keys.Q)) && (!prevKb.IsKeyDown(Keys.Q)))
                    {
                        gameState = SHOP;
                    }

                    //Update boulder speeds
                    speedBoulder1.X = dirXBoulder1 * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedBoulder1.Y = dirYAllBoulders * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedBoulder2.X = dirXBoulder2 * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedBoulder2.Y = dirYAllBoulders * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedBoulder3.X = dirXBoulder3 * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    speedBoulder3.Y = dirYAllBoulders * (maxSpeedBoulders * (float)gameTime.ElapsedGameTime.TotalSeconds);

                    //Add the speeds to the every object's true position 
                    boulder1Pos.X = boulder1Pos.X + speedBoulder1.X;
                    boulder1Pos.Y = boulder1Pos.Y + speedBoulder1.Y;
                    boulder2Pos.X = boulder2Pos.X + speedBoulder2.X;
                    boulder2Pos.Y = boulder2Pos.Y + speedBoulder2.Y;
                    boulder3Pos.X = boulder3Pos.X + speedBoulder3.X;
                    boulder3Pos.Y = boulder3Pos.Y + speedBoulder3.Y;

                    //Set the every object's bounding box position equal to its true position
                    boulder1Rec.X = (int)(boulder1Pos.X);
                    boulder1Rec.Y = (int)(boulder1Pos.Y);
                    boulder2Rec.X = (int)(boulder2Pos.X);
                    boulder2Rec.Y = (int)(boulder2Pos.Y);
                    boulder3Rec.X = (int)(boulder3Pos.X);
                    boulder3Rec.Y = (int)(boulder3Pos.Y);

                    //Set that they haven't tried to max out their health yet (buy a potionat high health)
                    maxHealth = false;

                    //Switch statement to check what inventory slot we're on
                    switch (inventorySelectedCount)
                    {
                        case 0:

                            //If we're on big potion slot and they press E and they hvae 1 or more potions, you add health and take one big potion away UNLESS it'll max out their health, then you just set max Health to true(which will show a red rectangle)
                            if (kb.IsKeyDown(Keys.E) && (!prevKb.IsKeyDown(Keys.E)))
                            {
                                if ((playerHealth + bigPotionAddition > 100) || (itemCounts[0] < 1))
                                {
                                    maxHealth = true;
                                }
                                else
                                {
                                    playerHealth += bigPotionAddition;
                                    itemCounts[0] -= 1;
                                }
                            }
                            break;

                        //If we're on medium potion slot and they press E and they have 1 or more potions, you add health and take one medium potion away UNLESS it'll max out their health, then you just set max Health to true (which will show a red rectangle, symbolizing that they can't)
                        case 1:

                            if (kb.IsKeyDown(Keys.E) && (!prevKb.IsKeyDown(Keys.E)))
                            {
                                if ((playerHealth + mediumPotionAddition > 100) || (itemCounts[1] < 1))
                                {
                                    maxHealth = true;
                                }
                                else
                                {
                                    playerHealth += mediumPotionAddition;
                                    itemCounts[1] -= 1;
                                }
                            }
                            break;

                        //If we're on small potion slot and they press E and they have 1 or more potions, you add health and take one small potion away UNLESS it'll max out their health, then you just set max Health to true (which will show a red rectangle, symbolizing that they can't)
                        case 2:

                            if (kb.IsKeyDown(Keys.E) && (!prevKb.IsKeyDown(Keys.E)))
                            {
                                if ((playerHealth + smallPotionAddition > 100) || (itemCounts[2] < 1))
                                {
                                    maxHealth = true;
                                }
                                else
                                {
                                    playerHealth += smallPotionAddition;
                                    itemCounts[2] -= 1;
                                }
                            }
                            break;

                        //If we're on monkeybomb slot and they press E and they have 1 or more monkeybombs and its not currently active, you place it down wherever the player is, start the timer, and put it to active. If they don't have any, you show a red rectangle, symbolizing that they can't
                        case 3:

                            if (kb.IsKeyDown(Keys.E) && (!prevKb.IsKeyDown(Keys.E)) && (monkeyBombActive == false))
                            {
                                if (itemCounts[3] > 0)
                                {
                                    monkeyBombActive = true;
                                    monkeyBombTriggered = false;
                                    monkeyBombPos.X = playerPos.X + 5;
                                    monkeyBombPos.Y = playerPos.Y + 20;
                                    monkeyBombTimer.ResetTimer(true);
                                }
                                else
                                {
                                    maxHealth = true;
                                }
                            }
                            break;
                    }

                    //If the monkeybomb timer's finished, you set it to inactive, and you subtract one monkeybomb from the count (put that around a bool so it cant spam into it)
                    if (monkeyBombTimer.IsFinished() == true)
                    {
                        monkeyBombActive = false;
                        if (monkeyBombTriggered == false)
                        {
                            itemCounts[3] -= 1;
                            monkeyBombTriggered = true;
                        }
                    }

                    //If the boulder is on the right and it hits the left, you rng what side it'll be on next and also call RandomizeBoulders (controls the movement and timers for the boulders)
                    if (bouldersSide == 2)
                    {
                        if (boulder1Pos.X <= 0 - (int)(boulderImg.Width * 0.2))
                        {
                            bouldersSide = rng.Next(1, 3);
                            RandomizeBoulders();
                        }
                    }
                    //If it's on the left, same thing but checking for if it hits right side
                    else
                    {
                        if (boulder1Pos.X >= screenWidth + (int)(boulderImg.Width * 0.2))
                        {
                            bouldersSide = rng.Next(1, 3);
                            RandomizeBoulders();
                        }
                    }

                    //If the timer's finished, then you change the X directions and reset the timer (for next time)
                    if (boulderTimer.IsFinished())
                    {
                        if (bouldersSide == 2)
                        {
                            dirXBoulder1 = NEGATIVE;
                            dirXBoulder2 = POSITIVE;
                            dirXBoulder3 = NEGATIVE;
                            boulderTimer.ResetTimer(false);

                        }
                        if (bouldersSide == 1)
                        {
                            dirXBoulder1 = POSITIVE;
                            dirXBoulder2 = NEGATIVE;
                            dirXBoulder3 = POSITIVE;
                            boulderTimer.ResetTimer(false);
                        }
                    }

                    //If the player hits any of the boulders and cooldown timer isn't active, you say that boulder intersection is true and you reset the timer.
                    if (((playerRec.Intersects(boulder1Rec)) || (playerRec.Intersects(boulder2Rec)) || (playerRec.Intersects(boulder3Rec))) && (!boulderInvincibilityTimer.IsActive()))
                    {
                        boulderIntersection = true;
                        boulderInvincibilityTimer.ResetTimer(true);
                    }
                    //If thats not happening, boulder intersection is false. This way, the invincibility timer has to be done so the damage wont spam
                    else
                    {
                        boulderIntersection = false;
                    }

                    //When the boulder intersection is true, take away 10 health
                    if ((boulderIntersection == true))
                    {
                        playerHealth -= 10;
                    }

                    //If the player's health is zero or lower, the game's over (duh)
                    if (playerHealth <= 0)
                    {
                        gameOver = true;
                    }

                    break;

                //ballroom state (why i called it this i have absolutely no idea there was a basketball on my floor)
                case BALLROOM:

                    //They're not hovering the cage or trying to get in yet
                    failedCageAttempt = false;
                    hoverCage = false;

                    //If they intersect with the cage, you set the cage hover to true (which will tell them to press space)
                    if (ballroomPlayerRec.Intersects(cageRec))
                    {
                        hoverCage = true;

                        //If they press space and they have all the keys, the game's over and they go to endgame. If they have less then 3, its a failed cage attempt (that equals true)
                        if (kb.IsKeyDown(Keys.Space))
                        {
                            if (keyCount >= 3)
                            {
                                gameState = ENDGAME;
                            }
                            else
                            {
                                failedCageAttempt = true;
                            }
                        }

                        //If they intersect with the bottom portal (the one that goes back to the arena)
                    }

                    if (ballroomPlayerRec.Intersects(ballroomPortals[1]))
                    {
                        //You set both the ballroom player rectangle and the arena player rectangle to the middle of the screen in their gamestates(away from the portal), so it doesn't still perceive it as a collision, and then you obviously update the positions
                        playerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                        playerPos = new Vector2(playerRec.X, playerRec.Y);
                        ballroomPlayerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                        ballroomPlayerPos = new Vector2(playerRec.X, playerRec.Y);

                        //And obviously change the gamestate to arena
                        gameState = ARENA1;
                    }

                    //They're not hovering the portal or trying to get in yet
                    failedPortal = false;
                    portalHover = false;

                    //If they intersect with the top portal (the one that takes them to the platformer)
                    if ((ballroomPlayerRec.Intersects(ballroomPortals[0])))
                    {
                        //They're hovering the portal (tells them to hit E)
                        portalHover = true;

                        //If they press E (they're trying to get in)
                        if (kb.IsKeyDown(Keys.E))
                        {
                            //If they have 800 or more points, you take them to the platformer
                            if (playerPoints >= 800)
                            //if (playerPoints >= 0) //(this is for the cheat)
                            {
                                //Take away 800 points, set the ballroom player rectangle back to the middle (so that they dont spam the collision, and obviously update the positions, and throw the getaway portal to the sandbox
                                //playerPoints -= 800;
                                ballroomPlayerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                                ballroomPlayerPos.X = (float)(ballroomPlayerRec.X);
                                ballroomPlayerPos.Y = (float)(ballroomPlayerRec.Y);
                                getawayPortalRec = new Rectangle(1000, 500, (int)(portalImg.Width * 0.1), (int)(portalImg.Height * 0.1));
                                keyCaptured = false;

                                //Check how many keys they have to see what level they'll be on. Every platformer is the same gamestate, I just change it according to how many keys they have
                                switch (keyCount)
                                {
                                    //If they have no keys, set all the platformers, spikes, the player, and the key accordingly
                                    case 0:

                                        platformRecs[0] = new Rectangle(0, 300, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[1] = new Rectangle(200, 250, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[2] = new Rectangle(400, 350, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[3] = new Rectangle(600, 270, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));

                                        spikeRecs[0] = new Rectangle(platformRecs[0].X + 80, platformRecs[0].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[1] = new Rectangle(platformRecs[1].X + 30, platformRecs[1].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[2] = new Rectangle(platformRecs[2].X + 70, platformRecs[2].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[3] = new Rectangle(platformRecs[3].X + 30, platformRecs[3].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));

                                        playerKeyRec = new Rectangle(10, platformRecs[0].Y - playerKeyRec.Height, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                                        playerKeyPos = new Vector2(playerKeyRec.X, playerKeyRec.Y);

                                        keyRec = new Rectangle(platformRecs[3].X + (platformRecs[3].Width / 2) - ((int)(keyImg.Width * 0.1) / 2) + 40, platformRecs[3].Y - ((int)(keyImg.Height * 0.1)) - 20, (int)(keyImg.Width * 0.1), (int)(keyImg.Height * 0.1));
                                        break;

                                    //If they have 1 key, set all the platformers, spikes, the player, and the key accordingly
                                    case 1:

                                        platformRecs[0] = new Rectangle(0, 400, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[1] = new Rectangle(200, 300, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[2] = new Rectangle(400, 250, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[3] = new Rectangle(600, 200, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));

                                        spikeRecs[0] = new Rectangle(platformRecs[0].X + 80, platformRecs[0].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[1] = new Rectangle(platformRecs[1].X + 30, platformRecs[1].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[2] = new Rectangle(platformRecs[2].X + 70, platformRecs[2].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[3] = new Rectangle(platformRecs[3].X + 30, platformRecs[3].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));

                                        playerKeyRec = new Rectangle(10, platformRecs[0].Y - playerKeyRec.Height, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                                        playerKeyPos = new Vector2(playerKeyRec.X, playerKeyRec.Y);

                                        keyRec = new Rectangle(platformRecs[3].X + (platformRecs[3].Width / 2) - ((int)(keyImg.Width * 0.1) / 2) + 40, platformRecs[3].Y - ((int)(keyImg.Height * 0.1)) - 20, (int)(keyImg.Width * 0.1), (int)(keyImg.Height * 0.1));
                                        break;

                                    //If they have 2 keys, set all the platformers, spikes, the player, and the key accordingly
                                    case 2:

                                        platformRecs[0] = new Rectangle(0, 200, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[1] = new Rectangle(platformRecs[0].Width, 200, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[2] = new Rectangle(platformRecs[0].Width * 2, 200, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));
                                        platformRecs[3] = new Rectangle(600, 420, (int)(platformImg.Width * 0.1), (int)(platformImg.Height * 0.02));

                                        spikeRecs[0] = new Rectangle(platformRecs[1].X + 23, platformRecs[0].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[1] = new Rectangle(platformRecs[1].X + 28, platformRecs[1].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[2] = new Rectangle(platformRecs[3].X + 35, platformRecs[3].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));
                                        spikeRecs[3] = new Rectangle(platformRecs[3].X + 30, platformRecs[3].Top - (int)(spikeImg.Height * 0.25), (int)(spikeImg.Width * 0.25), (int)(spikeImg.Height * 0.25));

                                        playerKeyRec = new Rectangle(10, platformRecs[0].Y - playerKeyRec.Height, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
                                        playerKeyPos = new Vector2(playerKeyRec.X, playerKeyRec.Y);

                                        keyRec = new Rectangle(platformRecs[3].X + (platformRecs[3].Width / 2) - ((int)(keyImg.Width * 0.1) / 2) + 40, platformRecs[3].Y - ((int)(keyImg.Height * 0.1)) - 20, (int)(keyImg.Width * 0.1), (int)(keyImg.Height * 0.1));

                                        break;
                                }
                                //And obviously set the gamestate to the platformer 
                                gameState = KEY1;
                                break;
                            }
                            //If they have less than 800 points, it tells them they need more points
                            else
                            {
                                failedPortal = true;
                            }
                        }


                    }
                    //If it doesn't intersect, they're not even hovering the portal
                    else
                    {
                        portalHover = false;
                    }

                    //Clamp the player in the ballroomstate to be between the walls
                    ballroomPlayerPos.X = MathHelper.Clamp(ballroomPlayerPos.X, 0, screenWidth - playerRec.Width);
                    ballroomPlayerPos.Y = MathHelper.Clamp(ballroomPlayerPos.Y, 0, screenHeight - playerRec.Height);

                    //Update the player's speed in real time
                    ballroomSpeedPlayer.X = ballroomDirXPlayer * (ballroomMaxSpeedPlayer * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    ballroomSpeedPlayer.Y = ballroomDirYPlayer * (ballroomMaxSpeedPlayer * (float)gameTime.ElapsedGameTime.TotalSeconds);

                    //Add the speeds to the every object's true position 
                    ballroomPlayerPos.X = ballroomPlayerPos.X + ballroomSpeedPlayer.X;
                    ballroomPlayerPos.Y = ballroomPlayerPos.Y + ballroomSpeedPlayer.Y;

                    //Set the every object's bounding box position equal to its true position
                    ballroomPlayerRec.X = (int)(ballroomPlayerPos.X);
                    ballroomPlayerRec.Y = (int)(ballroomPlayerPos.Y);

                    //Movement code
                    if (kb.IsKeyDown(Keys.W))
                    {
                        ballroomDirYPlayer = -1;
                    }
                    if (kb.IsKeyDown(Keys.S))
                    {
                        ballroomDirYPlayer = 1;
                    }
                    if (kb.IsKeyDown(Keys.A))
                    {
                        ballroomDirXPlayer = -1;
                    }
                    if (kb.IsKeyDown(Keys.D))
                    {
                        ballroomDirXPlayer = 1;
                    }
                    if (!kb.IsKeyDown(Keys.W) && (!(kb.IsKeyDown(Keys.S))))
                    {
                        ballroomDirYPlayer = 0;
                    }
                    if (!kb.IsKeyDown(Keys.A) && (!(kb.IsKeyDown(Keys.D))))
                    {
                        ballroomDirXPlayer = 0;
                    }

                    break;

                //endgame state
                case ENDGAME:

                    //In the endgame state, bring the restart button back from the sandbox into the screen
                    restartButtonRec = new Rectangle(screenWidth - 20 - (int)(restartButtonImg.Width * 0.1), 20, (int)(restartButtonImg.Width * 0.1), (int)(restartButtonImg.Height * 0.1));

                    //If they press the restart button, it restarts
                    if ((buttonPressed == true) && (restartButtonRec.Contains(mouse.Position)))
                    {
                        RestartGame();
                    }

                    //If they press the exit button, it exits
                    if ((buttonPressed == true) && (exitRec.Contains(mouse.Position)))
                    {
                        Exit();
                    }

                    break;

                //platformer state
                case KEY1:

                    //If they press the restart button, the game restarts
                    if ((buttonPressed == true) && (restartButtonRec.Contains(mouse.Position)))
                    {
                        RestartGame();
                    }

                    //If they game is over (they die to a spike), we pull up the restart button, otherwise it goes in the sandbox
                    if (gameOver == true)
                    {
                        restartButtonRec = new Rectangle(350, 200, (int)(restartButtonImg.Width * 0.1), (int)(restartButtonImg.Height * 0.1));
                    }
                    else
                    {
                        restartButtonRec = new Rectangle(1000, 500, (int)(restartButtonImg.Width * 0.1), (int)(restartButtonImg.Height * 0.1));
                    }


                    //If they hit D on the ground and they're not intersecting into anywhere where a platform would be on their right, the X direction becomes positive (they move right) and update the speed in real time
                    if (kb.IsKeyDown(Keys.D) && (grounded == true) && (!playerCollisionRecs[RIGHT].Intersects(platformRecs[3])) && (!playerCollisionRecs[RIGHT].Intersects(platformRecs[1])))
                    {
                        dirXKeyPlayer = POSITIVE;
                        playerKeySpeed.X = dirXKeyPlayer * (playerKeyMaxSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);

                    }
                    //If they hit A on the ground and they're not intersecting into anywhere where a platform would be on their left, the X direction becomes negative (they move left) and update the speed in real time
                    else if (kb.IsKeyDown(Keys.A) && (grounded == true) && (!playerCollisionRecs[LEFT].Intersects(platformRecs[1])))
                    {
                        dirXKeyPlayer = NEGATIVE;
                        playerKeySpeed.X = dirXKeyPlayer * (playerKeyMaxSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds);
                    }

                    else
                    {
                        //You apply friction when the player is on the ground and they're not moving left or right
                        if (grounded == true)
                        {
                            dirXKeyPlayer = STOPPED;
                            playerKeySpeed.X += -Math.Sign(playerKeySpeed.X) * forces.X;
                        }
                    }

                    //If they go in the getaway portal, they've successfully retrieved the key (so you add one) and you pull them back into the ballroom
                    if (playerKeyRec.Intersects(getawayPortalRec))
                    {
                        gameState = BALLROOM;
                        keyCount += 1;
                    }

                    //Jump if the player hits space and is on the ground
                    if (kb.IsKeyDown(Keys.W) && grounded == true)
                    {
                        playerKeySpeed.Y = jumpSpeed;
                    }

                    //If they intersect with the key and it hasn't been captured yet, you state that it's been captured and then you check their keycount.
                    if (playerKeyRec.Intersects(keyRec) && (keyCaptured == false))
                    {
                        keyCaptured = true;

                        //If they have less then 2 (they're on level 1 or 2), you place the key accordingly
                        if (keyCount < 2)
                        {
                            getawayPortalRec = new Rectangle(platformRecs[0].X + (platformRecs[0].Width / 2) - ((int)(portalImg.Width * 0.1) / 2) - 40, platformRecs[0].Y - ((int)(portalImg.Height * 0.1)) - 20, (int)(portalImg.Width * 0.1), (int)(portalImg.Height * 0.1));
                        }

                        //If they have more than 2 (are on level 3), you put it in a different spot. This is needed because the third level platforms is rearranged differently (you can't go back)
                        else
                        {

                            getawayPortalRec = new Rectangle(20, 420, (int)(portalImg.Width * 0.1), (int)(portalImg.Height * 0.1));
                        }
                    }

                    //Add gravity to the player's y Speed (even if not jumping, to allow for falling off ledges)
                    if (grounded == false)
                    {
                        playerKeySpeed.Y += forces.Y;
                    }

                    //Loop for all spike recs
                    for (int p = 0; p <= 3; p++)
                    {
                        //If the spike cooldown is finished and their feet hit a spike, you subtract 45 from their health and reset the timer. The timer makes it so it doesn't spam damage, and we only check for feet because obviously nothing else will hit a spike.
                        if (spikeCooldownTimer.IsFinished() == true)
                        {
                            if (playerCollisionRecs[FEET].Intersects(spikeRecs[p]))
                            {
                                playerHealth -= 45;
                                spikeCooldownTimer.ResetTimer(true);
                            }
                        }
                    }

                    //If the playerhealth is 0 or below here, game is over
                    if (playerHealth <= 0)
                    {
                        gameOver = true;
                    }

                    //Add the speed components to the object's true position

                    playerKeyPos.X += playerKeySpeed.X;
                    playerKeyPos.Y += playerKeySpeed.Y;

                    //If they're not hitting A or D (not moving left or right), their X speed is 0
                    if ((!kb.IsKeyDown(Keys.A)) && (!kb.IsKeyDown(Keys.D)))
                    {
                        playerKeySpeed.X = 0;
                    }

                    //Set the object's drawn position to rounded down true position
                    playerKeyRec.X = (int)playerKeyPos.X;
                    playerKeyRec.Y = (int)playerKeyPos.Y;

                    //Update the player's healthbar according to their rectangles
                    playerKeyBlankHealthBarRec = new Rectangle((int)(playerKeyPos.X - 20), (int)(playerKeyPos.Y - 20), 100, 15);
                    playerKeyHealthBarRec = new Rectangle(playerKeyBlankHealthBarRec.X, playerKeyBlankHealthBarRec.Y, playerHealth, 15);

                    //Set collision recs
                    SetPlayerRecs();
                    
                    //Check for if they're hit a wall (this keeps them from escaping the screen)
                    PlayerWallCollision();

                    //Check if they're hit a platform
                    PlatformCollision();

                    break;

                //shop gamestate
                case SHOP:

                    //If they hit escape, you take them to the arena
                    if ((kb.IsKeyDown(Keys.Escape)) && (!prevKb.IsKeyDown(Keys.Escape)))
                    {
                        gameState = ARENA1;
                    }

                    //all of the buyFails are true initially (checks if they've clicked on an item when they don't have enough creds)
                    for (int t = 0; t <= 5; t++)
                    {
                        buyFail[t] = false;
                    }

                    for (int v = 0; v <= 3; v++)
                    {
                        //If the cooldown timer is finished
                        if (shopBuyingCooldownTimers[v].IsFinished() == true)
                        {
                            //If they're hovering over the rectangle, we show them the information
                            if (shopItemRecs[v].Contains(mouse.Position))
                            {
                                shopInfoBoxes[v] = true;

                                //If they click a button, we check if they have enough creds to buy it
                                if ((buttonPressed == true))
                                {
                                    //If they don't have enough, they failed to buy it and will be shown the red rectangle
                                    if (playerPoints - itemPrices[v] < 0)
                                    {
                                        buyFail[v] = true;
                                    }
                                    //If they have enough, we add one to the item's count, take away its price from the player's points, and reset the timer, so that it won't spam into here and only give the player one item for one click
                                    else
                                    {
                                        itemCounts[v] += 1;
                                        playerPoints -= itemPrices[v];
                                        shopBuyingCooldownTimers[v].ResetTimer(true);
                                    }

                                }
                                //If they aren't clicking, then they obvioulsy aren't failing to buy anything
                                else
                                {
                                    buyFail[v] = false;
                                }
                            }
                            //If they're not hovering, then they obviously aren't failing to buy anything
                            else
                            {
                                shopInfoBoxes[v] = false;
                            }
                        }
                    }

                    //If the cooldown timer is finished
                    if (shopBuyingCooldownTimers[5].IsFinished() == true)
                    {
                        //If they're hovering over the rectangle, we show them the information
                        if (shopItemRecs[5].Contains(mouse.Position))
                        {
                            shopInfoBoxes[5] = true;

                            //If they click a button, we check if they have enough creds to buy it
                            if ((buttonPressed == true))
                            {
                                //If they don't have enough, they failed to buy it and will be shown the red rectangle
                                if (playerPoints - itemPrices[4] < 0)
                                {
                                    buyFail[5] = true;
                                }
                                //If they have enough, we add one to the item's count, take away its price from the player's points, and reset the timer, so that it won't spam into here and only give the player one item for one click
                                else
                                {
                                    itemCounts[4] += 1;
                                    playerPoints -= itemPrices[4];
                                    shopBuyingCooldownTimers[5].ResetTimer(true);
                                }
                            }
                            //If they aren't clicking, then they obvioulsy aren't failing to buy anything
                            else
                            {
                                buyFail[5] = false;
                            }
                        }
                        //If they're not hovering, then they obviously aren't failing to buy anything
                        else
                        {
                            shopInfoBoxes[5] = false;
                        }
                    }

                    //If they're hovering over the rectangle, show the information
                    if (shopItemRecs[4].Contains(mouse.Position))
                    {
                        shopInfoBoxes[4] = true;

                        //If they press the button, set all the luck cards bools to false (like if a card has been opened or if its over) and rerandomize the lucky card, and obviously switch the gamestate
                        if (buttonPressed == true)
                        {
                            luckCardsOver = false;
                            leftCardOpened = false;
                            rightCardOpened = false;
                            pointsDoubled = false;
                            luckyCard = rng.Next(1, 3);
                            gameState = LUCK_CARDS;

                        }
                    }
                    //If they're not hovering, don't show the information
                    else
                    {
                        shopInfoBoxes[4] = false;
                    }

                    break;

                //luck_cards gamestate
                case LUCK_CARDS:

                    //If they press a button and neither card is flipped over
                    if ((buttonPressed == true) && (leftCardOpened == false) && (rightCardOpened == false))
                    {
                        //If they press left, open it 
                        if (leftCardRec.Contains(mouse.Position))
                        {
                            leftCardOpened = true;
                            luckCardsOver = true;
                        }

                        //If they press right, open it 
                        if (rightCardRec.Contains(mouse.Position))
                        {
                            rightCardOpened = true;
                            luckCardsOver = true;
                        }
                    }

                    //If the card is 1 (the lucky one), and its over and the points haven't been doubled, double it and put the bool to true so it doesn't spam in here
                    if ((luckyCard == 1) && (luckCardsOver == true) && pointsDoubled == false)
                    {
                        playerPoints = playerPoints * 2;
                        pointsDoubled = true;
                    }
                    //If it's 2 (the unlucky one) and its over then set the player points to zero
                    if ((luckyCard == 2) && (luckCardsOver == true))
                    {
                        playerPoints = 0;
                    }
                    //If they hit escape, go back to arena, and on the way out, set that lucky cards aren't over for next time
                    if ((kb.IsKeyDown(Keys.Escape)) && (!prevKb.IsKeyDown(Keys.Escape)))
                    {
                        luckCardsOver = false;
                        gameState = ARENA1;
                    }
                    break;
            }

            base.Update(gameTime);
        }

        /// <summary>

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            //TODO: Add your drawing code here

            spriteBatch.Begin();

            switch (gameState)
            {
                //story gamestate
                case STORY:

                    //Draw the story texts at their location, use an array for efficiency
                    for (int y = 0; y <= 4; y++)
                    {
                        spriteBatch.DrawString(storyFont, storyTexts[y], storyTextLocs[y], Color.White);
                    }

                    break;

                case ARENA1:

                    //Draw the background and the void
                    spriteBatch.Draw(terrainBg, terrainRec, Color.Red);
                    spriteBatch.Draw(voidImg, voidRec, Color.White);


                    //If monkey bomb is active, draw it
                    if (monkeyBombActive == true)
                    {
                        spriteBatch.Draw(monkeyBombImg, monkeyBombRec, Color.White);
                    }

                    //Loop to draw all the zombies. If they're frozen, draw them blue, otherwise draw them normal
                    for (int i = 0; i <= 9; i++)
                    {
                        if (zombiesActive[i] == true)
                        {
                            if ((freezeSplashActive == true) && (freezeSplashDistances[i] < 130) && (zombieSpeeds[i] == 0))
                            {
                                spriteBatch.Draw(zombieImg, zombieRecs[i], Color.CornflowerBlue);
                                spriteBatch.Draw(whiteAreaImg, zombieHealthBarRecs[i], Color.LightBlue);
                                spriteBatch.Draw(healthBarBlankImg, zombieBlankHealthBarRecs[i], Color.LightBlue);
                            }
                            else
                            {
                                spriteBatch.Draw(zombieImg, zombieRecs[i], Color.White);
                                spriteBatch.Draw(whiteAreaImg, zombieHealthBarRecs[i], Color.Red);
                                spriteBatch.Draw(healthBarBlankImg, zombieBlankHealthBarRecs[i], Color.Red);
                            }

                        }
                    }

                    GameOverDraw(playerRec, playerBlankHealthBarRec, playerHealthBarRec);

                    //If the player isn't being hit by anything
                    if ((boulderIntersection == false) && (playerHit == false))
                    {
                        if (gameOver == false)
                        {
                            //If the game is still running, show the player and their health bar
                            spriteBatch.Draw(playerImg, playerRec, Color.White);
                            spriteBatch.Draw(whiteAreaImg, playerHealthBarRec, Color.LightGreen);
                            spriteBatch.Draw(healthBarBlankImg, playerBlankHealthBarRec, Color.LightGreen);
                        }
                        else
                        {
                            //If it's over, show the endgame text and the restart button
                            spriteBatch.DrawString(titleFont, ENDGAME_TEXT, endGameTextLoc, Color.White);
                            spriteBatch.Draw(restartButtonImg, restartButtonRec, Color.Purple);
                        }
                    }

                    //If the player is being hit by something, draw it red for that frame
                    else
                    {
                        spriteBatch.Draw(playerImg, playerRec, Color.Red);
                        spriteBatch.Draw(whiteAreaImg, playerHealthBarRec, Color.LightGreen);
                        spriteBatch.Draw(healthBarBlankImg, playerBlankHealthBarRec, Color.LightGreen);
                    }

                    //If the player isn't trying to drink a potion when they can't, draw everything normal (the inventory rectangle white)
                    if (maxHealth == false)
                    {
                        spriteBatch.Draw(whiteAreaImg, inventoryRecs[inventorySelectedCount], Color.Gray);
                    }

                    //If they are trying to, draw it red for that frame for them to see
                    else
                    {
                        spriteBatch.Draw(whiteAreaImg, inventoryRecs[inventorySelectedCount], Color.Red);
                    }

                    //Draw the gold, key, and their counts
                    spriteBatch.Draw(goldImg, goldRec, Color.White);
                    spriteBatch.Draw(keyImg, keyArenaRec, Color.White);
                    spriteBatch.DrawString(instructionsFont, " " + Convert.ToString(keyCount), keyCountTextLoc, Color.White);
                    spriteBatch.DrawString(instructionsFont, " " + Convert.ToString(playerPoints), pointsTextLoc, Color.White);

                    //Draw the double points and instakill symbols
                    spriteBatch.Draw(doublePointsImg, doublePointsRec, Color.White);
                    spriteBatch.Draw(instaKillImg, instaKillRec, Color.White);

                    //Draw the inventory images
                    spriteBatch.Draw(potionImg, inventoryRecs[0], Color.Blue);
                    spriteBatch.Draw(potionImg, inventoryRecs[1], Color.Green);
                    spriteBatch.Draw(potionImg, inventoryRecs[2], Color.Yellow);
                    spriteBatch.Draw(monkeyBombImg, inventoryRecs[3], Color.White);
                    spriteBatch.Draw(freezeSplashImg, inventoryRecs[4], Color.White);

                    //Draw the inventory item counts and the blank squares
                    for (int o = 0; o <= 4; o++)
                    {
                        spriteBatch.Draw(blankSquareImg, inventoryRecs[o], Color.White);
                        spriteBatch.DrawString(hudFont, Convert.ToString(itemCounts[o]), inventoryCountTextLocs[o], Color.White);
                    }

                    //Draw the boulders and the portal
                    spriteBatch.Draw(boulderImg, boulder1Rec, Color.White);
                    spriteBatch.Draw(boulderImg, boulder2Rec, Color.White);
                    spriteBatch.Draw(boulderImg, boulder3Rec, Color.White);
                    spriteBatch.Draw(portalImg, portalRec, Color.Purple);

                    //If double points is active, write it on the screen
                    if (doublePointsActive == true)
                    {
                        spriteBatch.DrawString(instructionsFont, "DOUBLE POINTS", instructionsTitleLoc, Color.Yellow);
                    }

                    //If insta kill is active, write it on the screen
                    if (instaKillActive == true)
                    {
                        spriteBatch.DrawString(instructionsFont, "INSTA KILL", instaKillTitleLoc, Color.Yellow);
                    }

                    break;

                //instructions gamestate
                case INSTRUCTIONS:

                    //Write all the instruction texts (potentially could've made it into an array and loop it, but decided not to because there's different colors for different texts)
                    spriteBatch.DrawString(instructionsFont, "W-A-S-D for movement & Arrow Keys for Inventory Scrolling", instructionsTextLocs[0], Color.Cyan);
                    spriteBatch.DrawString(instructionsFont, "Press E to Use & Space to Attack & I for Instructions", instructionsTextLocs[1], Color.Cyan);
                    spriteBatch.DrawString(instructionsFont, "PRESS Q for SHOP & Esc to Leave Anything", instructionsTextLocs[2], Color.Cyan);
                    spriteBatch.DrawString(instructionsFont, "Drag zombies into the void for more damage", instructionsTextLocs[3], Color.Orange);
                    spriteBatch.DrawString(instructionsFont, "2x (double points) gives double points for each kill", instructionsTextLocs[4], Color.Red);
                    spriteBatch.DrawString(instructionsFont, "The skull (insta kill) gives instant killing abilities", instructionsTextLocs[5], Color.Red);
                    spriteBatch.DrawString(instructionsFont, "Capture all 3 keys to open the door and obtain the Lumina Crystal", instructionsTextLocs[6], Color.Yellow);
                    spriteBatch.DrawString(instructionsFont, "INSTRUCTIONS", instructionsTitleLoc, Color.White);

                    break;

                //ballroom gamestate
                case BALLROOM:

                    //draw the background, the two portals, the cage, and the player
                    spriteBatch.Draw(terrainBg, terrainRec, Color.Purple);
                    spriteBatch.Draw(portalImg, ballroomPortals[0], Color.Black);
                    spriteBatch.Draw(portalImg, ballroomPortals[1], Color.LightCyan);
                    spriteBatch.Draw(cageImg, cageRec, Color.White);
                    spriteBatch.Draw(playerImg, ballroomPlayerRec, Color.White);

                    //If they try opening the cage and fail, tell them they need all the keys
                    if (failedCageAttempt == true)
                    {
                        spriteBatch.DrawString(instructionsFont, cageFailedText, cageFailedTextLoc, Color.Yellow);
                    }

                    //If they hover the cage, tell them how to open it
                    if (hoverCage == true)
                    {
                        spriteBatch.DrawString(instructionsFont, "Press SPACE To Open", hoverCageTextLoc, Color.Yellow);
                    }

                    //If they hover the portal, tell them the cost and how to enter it
                    if (portalHover == true)
                    {
                        spriteBatch.DrawString(instructionsFont, "Press E To Enter - Cost: 800 GOLD", cageFailedTextLoc, Color.Yellow);
                    }

                    //If they try entering the portal and fail, tell them they need 800 gold
                    if (failedPortal == true)
                    {
                        spriteBatch.DrawString(instructionsFont, portalFailedText, failedPortalTextLoc, Color.Yellow);
                    }

                    break;

                //case platformer
                case KEY1:

                    //Draw the background, spikes, and platforms
                    spriteBatch.Draw(terrainBg, terrainRec, Color.Purple);
                    

                    for (int o = 0; o <= 3; o++)
                    {
                        spriteBatch.Draw(platformImg, platformRecs[o], Color.Orange);
                        spriteBatch.Draw(spikeImg, spikeRecs[o], Color.Cyan);
                    }

                    GameOverDraw(playerKeyRec, playerKeyBlankHealthBarRec, playerKeyHealthBarRec);

                    //If the key hasn't been captured, draw the key
                    if (keyCaptured == false)
                    {
                        spriteBatch.Draw(keyImg, keyRec, Color.White);
                    }

                    //If the key has been captured, don't draw the key and draw the getaway portal
                    else
                    {
                        spriteBatch.Draw(portalImg, getawayPortalRec, Color.White);
                    }

                    break;

                //case endgame
                case ENDGAME:

                    //Draw the white background, the endgame texts, the restart button, the crystal, and the exit button
                    spriteBatch.Draw(whiteBg, terrainRec, Color.White);
                    spriteBatch.DrawString(instructionsFont, endgameMessage1, endGameTextLocs[0], Color.Blue);
                    spriteBatch.DrawString(instructionsFont, endgameMessage2, endGameTextLocs[1], Color.Blue);
                    spriteBatch.Draw(restartButtonImg, restartButtonRec, Color.Purple);
                    spriteBatch.Draw(crystalImg, crystalRec, Color.Purple);
                    spriteBatch.Draw(exitImg, exitRec, Color.Purple);
                    break;

                case SHOP:

                    //Draw the images in the shop
                    spriteBatch.Draw(potionImg, shopItemRecs[0], Color.Blue);
                    spriteBatch.Draw(potionImg, shopItemRecs[1], Color.Green);
                    spriteBatch.Draw(potionImg, shopItemRecs[2], Color.Yellow);

                    spriteBatch.Draw(monkeyBombImg, shopItemRecs[3], Color.White);
                    spriteBatch.Draw(freezeSplashImg, shopItemRecs[5], Color.White);
                    spriteBatch.Draw(luckCardsImg, shopItemRecs[4], Color.White);

                    //Draw the textboxes in the shop
                    for (int o = 0; o <= 5; o++)
                    {
                        spriteBatch.Draw(whiteAreaImg, shopTextRecs[o], Color.WhiteSmoke);
                    }

                    //Draw all the counts in the textboxes (don't draw for the luck cards, because they have no count obviously)
                    for (int o = 0; o <= 3; o++)
                    {
                        spriteBatch.DrawString(hudFont, COUNT_TEXT + Convert.ToString(itemCounts[o]), shopTextLocs[o], Color.Black);
                    }
                    spriteBatch.DrawString(hudFont, COUNT_TEXT + Convert.ToString(itemCounts[4]), shopTextLocs[5], Color.Black);

                    //Draw the shop's title
                    spriteBatch.DrawString(titleFont, SHOP_TEXT + " (" + Convert.ToString(playerPoints) + ") - CLICK ON ITEMS TO BUY", shopTitleLoc, Color.Yellow);


                    for (int o = 0; o <= 5; o++)
                    {
                        //Draw the info boxes ONLY if they hover over it
                        if (shopInfoBoxes[o] == true)
                        {
                            spriteBatch.Draw(whiteAreaImg, shopItemRecs[o], Color.White);
                            spriteBatch.DrawString(hudFont, shopInfoTexts[o * 4], shopInfoTextLocs[o * 4], Color.Red);
                            spriteBatch.DrawString(hudFont, shopInfoTexts[(o * 4) + 1], shopInfoTextLocs[(o * 4) + 1], Color.Blue);
                            spriteBatch.DrawString(hudFont, shopInfoTexts[(o * 4) + 2], shopInfoTextLocs[(o * 4) + 2], Color.Black);
                            spriteBatch.DrawString(hudFont, shopInfoTexts[(o * 4) + 3], shopInfoTextLocs[(o * 4) + 3], Color.Black);
                        }
                    }

                    //Draw the red rectangle if they try to buy it and fail
                    for (int y = 0; y <= 5; y++)
                    {
                        if (buyFail[y] == true)
                        {
                            spriteBatch.Draw(whiteAreaImg, shopItemRecs[y], Color.Red);
                        }
                    }
                    break;

                case LUCK_CARDS:

                    //If the right card is opened
                    if (rightCardOpened == true)
                    {
                        //Draw the card flipped over
                        spriteBatch.Draw(whiteAreaImg, rightCardRec, Color.White);

                        //If they got lucky draw a full bag
                        if (luckyCard == 1)
                        {
                            spriteBatch.Draw(fullBagImg, rightCardRec, Color.White);
                        }
                        //If they didn't draw an empty one
                        else
                        {
                            spriteBatch.Draw(emptyBagImg, rightCardRec, Color.White);
                        }
                    }
                    //If they haven't draw it closed
                    else
                    {
                        spriteBatch.Draw(playingCardImg, rightCardRec, Color.Blue);
                    }

                    //If the left card is opened
                    if (leftCardOpened == true)
                    {
                        //Draw the card flipped over
                        spriteBatch.Draw(whiteAreaImg, leftCardRec, Color.White);

                        //If they got unlucky draw an empty bag
                        if (luckyCard == 1)
                        {
                            spriteBatch.Draw(fullBagImg, leftCardRec, Color.White);
                        }
                        //If they got lucky draw a full one
                        else
                        {
                            spriteBatch.Draw(emptyBagImg, leftCardRec, Color.White);
                        }
                    }
                    //If they haven't opened it yet, draw it closed
                    else
                    {
                        spriteBatch.Draw(playingCardImg, leftCardRec, Color.Red);
                    }

                    break;
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        //Pre: None
        //Post: None
        //Desc: Randomize the boulders and control their movements accordingly
        private void RandomizeBoulders()
        {
            if (bouldersSide == 2)
            {
                boulder1Pos.X = (screenWidth);
                boulder1Pos.Y = (60 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder1Rec.X = (int)(boulder1Pos.X);
                boulder1Rec.Y = (int)(boulder1Pos.Y);

                boulder2Pos.X = (0 - (int)(boulderImg.Width * 0.2));
                boulder2Pos.Y = (240 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder2Rec.X = (int)(boulder2Pos.X);
                boulder2Rec.Y = (int)(boulder2Pos.Y);

                boulder3Pos.X = (screenWidth);
                boulder3Pos.Y = (420 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder3Rec.X = (int)(boulder3Pos.X);
                boulder3Rec.Y = (int)(boulder3Pos.Y);
            }
            if (bouldersSide == 1)
            {
                bouldersSide = rng.Next(1, 3);
                boulder1Pos.X = (0 - (int)(boulderImg.Width * 0.2));
                boulder1Pos.Y = (60 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder1Rec.X = (int)(boulder1Pos.X);
                boulder1Rec.Y = (int)(boulder1Pos.Y);

                boulder2Pos.X = (screenWidth);
                boulder2Pos.Y = (240 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder2Rec.X = (int)(boulder2Pos.X);
                boulder2Rec.Y = (int)(boulder2Pos.Y);

                boulder3Pos.X = (0 - (int)(boulderImg.Width * 0.2));
                boulder3Pos.Y = (420 - ((int)(boulderImg.Height * 0.2)) / 2);
                boulder3Rec.X = (int)(boulder3Pos.X);
                boulder3Rec.Y = (int)(boulder3Pos.Y);

                dirXBoulder1 = STOPPED;
                dirXBoulder2 = STOPPED;
                dirXBoulder3 = STOPPED;
            }

            dirXBoulder1 = STOPPED;
            dirXBoulder2 = STOPPED;
            dirXBoulder3 = STOPPED;

            boulderTimer.ResetTimer(true);
            boulderTimer.Activate();
        }
        
        //Pre: None
        //Post: None
        //Desc: Set the player collision recs
        private void SetPlayerRecs()
        {
            playerCollisionRecs[HEAD] = new Rectangle(playerKeyRec.X + 15, playerKeyRec.Y + 5, 27, 24);
            playerCollisionRecs[LEFT] = new Rectangle(playerKeyRec.X + 3, playerKeyRec.Y + 29, 15, 41);
            playerCollisionRecs[RIGHT] = new Rectangle(playerKeyRec.X + 43, playerKeyRec.Y + 29, 15, 41);
            playerCollisionRecs[FEET] = new Rectangle(playerKeyRec.X + 15, playerKeyRec.Y + 55, 27, 54);
        }

        //Pre: None
        //Post: None
        //Desc: Check for wall collisions, and keep player from exiting screen
        private void PlayerWallCollision()
        {
            bool platformCollision = false;

            //If the player hits the side walls, pull them in bounds and stop their horizontal movement
            if (playerKeyRec.X < 0)
            {
                //Player past left side of screen, realign to be exactly the left side and stop movement
                playerKeyRec.X = 0;
                playerKeyPos.X = playerKeyRec.X;
                playerKeySpeed.X = 0;
                platformCollision = true;

            }
            else if (playerKeyRec.Right > screenWidth)
            {
                // Player past right side of screen, realign to be exactly the right side and stop movement
                playerKeyRec.X = screenWidth - playerKeyRec.Width;
                playerKeyPos.X = playerKeyRec.X;
                playerKeySpeed.X = 0;
                platformCollision = true;
            }

            // If the player hits the top/bottom walls, pull them in bounds and stop their vertical movement
            if (playerKeyRec.Y < 0)
            {
                //Player past top side of screen, realign to be exactly the top side and stop movement
                playerKeyRec.Y = 0;
                playerKeyPos.Y = playerKeyRec.Y;
                playerKeySpeed.Y = 0;
                platformCollision = true;
            }
            else if (playerKeyRec.Bottom >= screenHeight)
            {
                //Player past bottom side of screen, realign to be exactly the bottom side and stop movement
                playerKeyRec.Y = screenHeight - playerKeyRec.Height;
                playerKeyPos.Y = playerKeyRec.Y;
                playerKeySpeed.Y = 0;

                //The player just landed on the ground
                grounded = true;
            }
            else
            {
                //The player is off the ground, either jumping or falling
                grounded = false;
            }

            if (platformCollision == true)
            {
                SetPlayerRecs();
            }
        }

        //Pre: None
        //Post: None
        //Desc: Check for platform collisions, and control player's movement accordingly
        private void PlatformCollision()
        {
            bool platformCollision = false;

            for (int i = 0; i <= 4; i++)
            {
                if (playerCollisionRecs[FEET].Intersects(platformRecs[i]))
                {
                    playerRec.Y = platformRecs[i].Y - playerRec.Height;
                    playerPos.Y = playerRec.Y;
                    playerKeySpeed.Y = 0f;
                    grounded = true;
                    platformCollision = true;
                }
                else if (playerCollisionRecs[LEFT].Intersects(platformRecs[i]))
                {
                    playerRec.X = platformRecs[i].X + platformRecs[i].Width + 50;
                    playerPos.X = playerRec.X;
                    playerKeySpeed.X = 0;
                    platformCollision = true;
                }
                else if (playerCollisionRecs[RIGHT].Intersects(platformRecs[i]))
                {
                    playerRec.X = platformRecs[i].X - playerRec.Width - 50;
                    playerPos.X = playerRec.X;
                    playerKeySpeed.X = 0;
                    platformCollision = true;
                }
                else if (playerCollisionRecs[HEAD].Intersects(platformRecs[i]))
                {
                    playerRec.Y = platformRecs[i].Y + platformRecs[i].Height + 1;
                    playerPos.Y = playerRec.Y;
                    playerKeySpeed.Y = playerKeySpeed.Y * -1;
                    platformCollision = true;
                }

                if (platformCollision == true)
                {
                    SetPlayerRecs();
                    platformCollision = false;
                }
            }
        }

        //Pre: None
        //Post: None
        //Desc: Restart the game and set all variables back to normal
        private void RestartGame()
        {
            gameOver = false;
            keyCaptured = false;
            playerPoints = 0;
            playerHealth = 100;
            for (int b = 0; b <= 4; b++)
            {
                itemCounts[b] = 0;
            }

            leftCardOpened = false;
            rightCardOpened = false;
            luckCardsOver = false;
            monkeyBombActive = false;
            monkeyBombTriggered = false;
            doublePointsAppeared = false;
            instaKillAppeared = false;
            doublePointsActive = false;
            instaKillActive = false;
            doublePointsExpired = false;
            instaKillExpired = false;
            failedPortal = false;
            
            for (int j = 0; j <= 9; j++)
            {
                zombieVoidCollisionChecks[j] = false;
                zombieFrozenCheck[j] = false;
                zombieMonkeyBombCollisionCheck[j] = false;
                zombieHealths[j] = 100;
                zombieSpeeds[j] = (((float)j + 1f) / 10f);
            }
            
            freezeSplashFinished = false;
            freezeSplashActive = false;

            keyCount = 0;
            
            for (int w = 0; w <= 9; w++)
            {
                zombiesActive[w] = false;
            }
            boulderTimer.ResetTimer(false);
            monkeyBombTimer.ResetTimer(false);
            freezeSplashTimer.ResetTimer(false);
            zombieSpawnTimer.ResetTimer(true);
            playerAttackCooldownTimer.ResetTimer(true);
            boulderInvincibilityTimer.ResetTimer(false);
            doublePointsActiveTimer.ResetTimer(false);
            instaKillActiveTimer.ResetTimer(false);
            doublePointsGapTimer.ResetTimer(true);
            instaKillGapTimer.ResetTimer(true);
            doublePointsAppearedTimer.ResetTimer(false);
            instaKillAppearedTimer.ResetTimer(false);
            spikeCooldownTimer.ResetTimer(true);
            instructionsPreviewCooldown.ResetTimer(true);

            playerRec = new Rectangle(400, 240, (int)(playerImg.Width * 0.2), (int)(playerImg.Height * 0.2));
            playerPos = new Vector2(playerRec.X, playerRec.Y);

            for (int h = 0; h <= 5; h++)
            {
                shopBuyingCooldownTimers[h].ResetTimer(true);
            }
            for (int h = 0; h <= 9; h++)
            {
                zombieVoidCooldownTimers[h].ResetTimer(true);
            }
            gameState = STORY;
        }

        //Pre: playerRec is a valid rectangle, blankHealthBar is a valid rectangle, fullHealthBar is a valid rectangle
        //Post: None
        //Desc: Draw what needs to be drawn ONLY when the game is over and what needs to be drawn ONLY when the game isn't
                private void GameOverDraw(Rectangle playerRec, Rectangle blankHealthBar, Rectangle fullHealthBar)
        {
            if (gameOver == false)
            {
                //If the game is still running, show the player and their health bar
                spriteBatch.Draw(playerImg, playerRec, Color.White);
                spriteBatch.Draw(whiteAreaImg, fullHealthBar, Color.LightGreen);
                spriteBatch.Draw(healthBarBlankImg, blankHealthBar, Color.LightGreen);
            }
            else
            {
                //If it's over, show the endgame text and the restart button
                spriteBatch.DrawString(titleFont, ENDGAME_TEXT, endGameTextLoc, Color.White);
                spriteBatch.Draw(restartButtonImg, restartButtonRec, Color.Purple);
            }
        }
    }
}