using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using Color = Microsoft.Xna.Framework.Color;

namespace SNAPI_monster_test.Bosses
{

    public class SlimeBoss : Monster
    {

        private IMonitor Monitor;

        private bool hasSpawnedSlimes = false;
        private bool hasSpawnedBats = false;
        private int slimesToSpawn = 5;
        private int batsToSpawn = 3;
        private int spawnTimer = 0;
        private const int SPAWN_INTERVAL = 60; // 60 frames ≈ 1 second at 60 FPS

        // Attack variables
        private int attackCooldown = 0;
        private const int ATTACK_COOLDOWN_TIME = 120; // 2 seconds between attacks
        private float attackRange = 250f; // Range in pixels
        private float chargeSpeed = 6f; // Speed for charge attack
        private bool isCharging = false;
        private Vector2 chargeTarget;
        private int chargeDuration = 0;
        private const int MAX_CHARGE_DURATION = 30; // Half-second charge
        private bool hasAreaAttack = false; // Unlocks when below 25% health

        public SlimeBoss(string name, Vector2 position, int facingDir, IMonitor monitor)
            : base(name, position, facingDir)
        {
            Monitor = monitor; // Potrzebne do debugowania

            this.Sprite = new AnimatedSprite("Characters\\Monsters\\SlimeBoss");
            // Ustawienie spirte wysokość i szerokość
            int frameWidth = 60;
            int frameHeight = 64;
            this.Sprite.SpriteWidth = frameWidth;
            this.Sprite.SpriteHeight = frameHeight;

            this.Scale = 0.5f;

            // Ustawienie prostokąt źródłowy dla pierwszego frame
            this.Sprite.SourceRect = new Microsoft.Xna.Framework.Rectangle(0, 0, frameWidth, frameHeight);

            // Tworzenie animacji 
            List<FarmerSprite.AnimationFrame> animation = new List<FarmerSprite.AnimationFrame>();
            for (int i = 0; i < 4; i++)
            {
                animation.Add(new FarmerSprite.AnimationFrame(i, 150));
            }

            this.Sprite.CurrentAnimation = animation;
            this.Sprite.loop = true;
            this.Sprite.interval = 175f;

            // Monster properties
            this.Health = 100;
            this.MaxHealth = 100;
            this.damageToFarmer.Value = 50;
            this.resilience.Value = 2; // Add some defense
            this.moveTowardPlayerThreshold.Value = 20; // More aggressive
        }
        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            int num = Math.Max(1, damage - resilience.Value);
            if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
            {
                num = -1;
            }
            else
            {
                Health -= num;
                base.currentLocation.playSound("hitEnemy");
                setTrajectory(xTrajectory / 3, yTrajectory / 3);
                if (Health <= 0)
                {
                    base.deathAnimation();
                    Game1.player.mailReceived.Add("SlimeBoss");
                    Monitor.Log($"po   this.Health {this.Health}");
                    Game1.playSound("achievement");
                    Game1.drawObjectDialogue("Boss został pokonany!");
                    // Vector2 dropPosition = new Vector2(this.Position.X, this.Position.Y);
                    // Game1.createItemDebris(new StardewValley.Object("TwojMod_KluczDoDrzwi", 1), dropPosition, -1);
                }
            }

            return num;

        }
        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            // Sprawdzenie czy boss jest poniżej 50% HP oraz jeszcze nie spawnował minionki
            if (this.Health <= this.MaxHealth / 2 && !hasSpawnedSlimes && !hasSpawnedBats)
            {
                hasSpawnedSlimes = true;
                hasSpawnedBats = true;
                spawnTimer = 0;
            }

            // sprawdzenie czy boss jest poniżej 25% HP aby odblokować area attack
            if (this.Health <= this.MaxHealth / 4 && !hasAreaAttack)
            {
                hasAreaAttack = true;
            }

            // Handle slime spawning
            if (hasSpawnedSlimes && slimesToSpawn > 0)
            {
                spawnTimer++;

                // Spawn a new slime every second (60 frames)
                if (spawnTimer >= SPAWN_INTERVAL)
                {
                    SpawnSlime(location);
                    spawnTimer = 0;
                    slimesToSpawn--;
                }
            }

            // Handle bat spawning
            if (hasSpawnedBats && batsToSpawn > 0)
            {
                spawnTimer++;

                // Spawn a new bat every second (60 frames)
                if (spawnTimer >= SPAWN_INTERVAL)
                {
                    SpawnBat(location);
                    spawnTimer = 0;
                    batsToSpawn--;
                }
            }


            // Zwykły atak
            if (!isCharging)
            {
                // Zmniejsz czas odnowienia ataku, jeśli trwa
                if (attackCooldown > 0)
                    attackCooldown--;

                // Znajdź najbliższego gracza jako cel
                Farmer closestFarmer = findPlayer();
                //if (Game1.ticks % 60 == 0) Monitor.Log($"closestFarmer {closestFarmer}", LogLevel.Debug);

                // Sprawdź czy gracz został znaleziony i czy możemy już wykonać atak
                if (closestFarmer != null && attackCooldown <= 0)
                {
                    // Oblicz odległość między bossem a graczem
                    float distanceToFarmer = Vector2.Distance(this.Position, closestFarmer.Position);
                    //if (Game1.ticks % 60 == 0) Monitor.Log($"distanceToFarmer {distanceToFarmer}", LogLevel.Debug);

                    // Sprawdź czy gracz jest w zasięgu ataku
                    if (distanceToFarmer <= attackRange)
                    {
                        // Wybierz rodzaj ataku na podstawie zdrowia bossa i losowości

                        // Jeśli boss ma mniej niż 25% zdrowia i odblokował atak obszarowy, ma 25% szans na jego użycie
                        if (hasAreaAttack && Game1.random.NextDouble() < 0.25)
                        {
                            // Wykonaj atak obszarowy, który uderza wszystkich graczy w pobliżu
                            PerformAreaAttack(location);

                            // Dłuższy czas odnowienia dla silnego ataku obszarowego
                            attackCooldown = ATTACK_COOLDOWN_TIME * 2;
                        }
                        // 60% szans na atak szarżujący
                        else if (Game1.random.NextDouble() < 0.6)
                        {
                            // Rozpocznij atak szarżujący - szybki ruch w kierunku gracza
                            isCharging = true;  // Włącz tryb szarży
                            chargeTarget = closestFarmer.Position;  // Zapisz pozycję gracza jako cel szarży
                            chargeDuration = 0;  // Resetuj licznik czasu trwania szarży
                        }
                        // W przeciwnym razie wykonaj podstawowy atak (skok w kierunku gracza)
                        else
                        {
                            // Oblicz znormalizowany wektor kierunku do gracza
                            Vector2 trajectory = Vector2.Normalize(closestFarmer.Position - this.Position);

                            // Ustaw prędkość bossa w kierunku gracza
                            xVelocity = trajectory.X * 5f;  // Pozioma prędkość
                            yVelocity = 0f - trajectory.Y * 5f;  // Pionowa prędkość

                            // Ustaw czas odnowienia dla podstawowego ataku
                            attackCooldown = ATTACK_COOLDOWN_TIME;

                            // Odtwórz dźwięk slime'a podczas ataku
                            Game1.playSound("slime");
                        }
                    }
                }
            }
            else
            {
                // Obsługa trwającego ataku szarżującego

                // Zwiększ licznik czasu trwania szarży
                chargeDuration++;

                // Oblicz znormalizowany wektor kierunku do celu szarży
                Vector2 direction = Vector2.Normalize(chargeTarget - this.Position);

                // Ustaw prędkość bossa z większą wartością dla efektu szarży
                xVelocity = direction.X * chargeSpeed;  // Pozioma prędkość szarży
                yVelocity = 0f - direction.Y * chargeSpeed;  // Pionowa prędkość szarży (odwrócona, bo w Stardew Valley oś Y jest odwrócona)

                // Twórz efekt śladu za slimem podczas szarży co 5 klatek
                if (chargeDuration % 5 == 0)
                {
                    // Dodaj tymczasowy efekt wizualny śladu slime'a
                    location.temporarySprites.Add(new TemporaryAnimatedSprite(
                        6,  // Indeks animacji eksplozji/rozprysku
                        this.Position,  // Pozycja efektu
                        Color.LightGreen * 0.5f,  // Kolor efektu (jasnozielony z przezroczystością)
                        8,  // Liczba klatek animacji
                        false,  // Nie zapętlaj animacji
                        100f,  // Czas trwania każdej klatki w ms
                        0,  // Opóźnienie przed rozpoczęciem animacji
                        -1,  // Wysokość nad ziemią
                        -1f,  // Prędkość zanikania
                        -1,  // Skala
                        0  // Przezroczystość
                    ));
                }

                // Zakończ atak szarżujący po upływie określonego czasu
                if (chargeDuration >= MAX_CHARGE_DURATION)
                {
                    // Wyłącz tryb szarży
                    isCharging = false;

                    // Ustaw czas odnowienia ataku
                    attackCooldown = ATTACK_COOLDOWN_TIME;

                    // Zatrzymaj bossa (wyzeruj prędkości)
                    xVelocity = 0;
                    yVelocity = 0;
                }
            }

        }

        private void PerformAreaAttack(GameLocation location)
        {


            // Visual effect for area attack
            location.temporarySprites.Add(new TemporaryAnimatedSprite(
                6, // Explosion animation index
                this.Position,
                Color.Green,
                8, // Frame count
                false,
                100f, // Frame duration
                0,
                -1,
                -1f,
                -1,
                0
            )
            { scale = 2f });

            // Find all farmers within range
            float areaAttackRange = 300f;

            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {

                if (Vector2.Distance(this.Position, farmer.Position) <= areaAttackRange)
                {
                    // Deal damage to farmer
                    farmer.takeDamage((int)(damageToFarmer.Value * 0.8), true, this);

                    // Knockback effect
                    Vector2 knockbackDirection = Vector2.Normalize(farmer.Position - this.Position);
                    farmer.setTrajectory(knockbackDirection * 10f);
                }
            }
        }

        private void SpawnSlime(GameLocation location)
        {
            // Create a new slime at a random position around the boss
            Random random = new Random();
            Vector2 offset = new Vector2(
                (float)random.Next(-128, 128),
                (float)random.Next(-128, 128)
            );

            Vector2 slimePosition = this.Position + offset;

            // Create a new slime
            // Using standard Green Slime from the game
            GreenSlime slime = new GreenSlime(slimePosition, 0);

            // Set slime properties
            slime.Health = 50;
            slime.MaxHealth = 50;
            slime.Scale = 0.75f; // Slightly smaller than boss

            // Add slime to location
            location.characters.Add(slime);

            // Visual effect for slime appearance
            location.temporarySprites.Add(new TemporaryAnimatedSprite(
                6, // Explosion animation index
                slimePosition,
                Color.LightGreen,
                8, // Frame count
                false,
                100f, // Frame duration
                0,
                -1,
                -1f,
                -1,
                0
            ));

            // Sound effect
            Game1.playSound("slime");
        }

        private void SpawnBat(GameLocation location)
        {
            // Create a new bat at a random position around the boss
            Random random = new Random();
            Vector2 offset = new Vector2(
                (float)random.Next(-128, 128),
                (float)random.Next(-128, 128)
            );

            Vector2 batPosition = this.Position + offset;

            // Create a new bat
            Bat bat = new Bat(batPosition, 0);

            // Set bat properties
            bat.Health = 50;
            bat.MaxHealth = 50;
            bat.Scale = 1f;

            // Add bat to location
            location.characters.Add(bat);

            // Visual effect for bat appearance
            location.temporarySprites.Add(new TemporaryAnimatedSprite(
                6, // Explosion animation index
                batPosition,
                Color.LightGreen,
                8, // Frame count
                false,
                100f, // Frame duration
                0,
                -1,
                -1f,
                -1,
                0
            ));

            // Sound effect
            Game1.playSound("batScreech");
        }
    }
}
