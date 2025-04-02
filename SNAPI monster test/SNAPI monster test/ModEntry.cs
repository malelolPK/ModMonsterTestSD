using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Monsters;

namespace SNAPI_monster_test
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        private bool IsBossLoaded;

        public override void Entry(IModHelper helper)
        {
            // Sprawdź czy boss został załadowany
            helper.Events.GameLoop.SaveLoaded += (s, e) => CheckBossPresence();
            helper.Events.GameLoop.DayStarted += (s, e) => CheckBossPresence();

            // Rejestracja komendy debugującej
            //helper.ConsoleCommands.Add("spawn_boss", "Spawnuje bossa", this.SpawnBossCommand);
            helper.Events.Input.ButtonPressed += this.OnbuttonPressed;
        }

        private void OnbuttonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Context.IsWorldReady && e.Button == SButton.K)
            {
                SpawnBossCommand("spawn_boss", Array.Empty<string>());
            }
        }

        private void CheckBossPresence()
        {
            var monsterData = this.Helper.GameContent.Load<Dictionary<string, string>>("Data/Monsters");
            IsBossLoaded = monsterData.ContainsKey("SlimeBoss");

            this.Monitor.Log($"Boss loaded: {IsBossLoaded}", LogLevel.Info);
        }

        private void SpawnBossCommand(string command, string[] args)
        {
            if (!IsBossLoaded)
            {
                this.Monitor.Log("Boss nie został poprawnie załadowany!", LogLevel.Error);
                return;
            }



            GameLocation location = Game1.player.currentLocation;

            SlimeBoss boss = new SlimeBoss("SlimeBoss", new Vector2(Game1.player.Position.X + 64, Game1.player.Position.Y), 2);


            // Dodaj bossa do lokacji
            location.characters.Add(boss);
            this.Monitor.Log("Boss został spawniony!", LogLevel.Info);
        }

    }
}
public class SlimeBoss : Monster
{
    private bool hasSpawnedSlimes = false;
    private int slimesToSpawn = 5;
    private int spawnTimer = 0;
    private const int SPAWN_INTERVAL = 60; // 60 klatek ≈ 1 sekunda przy 60 FPS

    public SlimeBoss(string name, Vector2 position, int facingDir)
        : base(name, position, facingDir)
    {
        // Inicjalizacja sprite'a
        this.Sprite = new AnimatedSprite("Characters\\Monsters\\SlimeBoss");

        // Ustawienie wymiarów sprite'a
        int frameWidth = 60;
        int frameHeight = 64;
        this.Sprite.SpriteWidth = frameWidth;
        this.Sprite.SpriteHeight = frameHeight;

        // Ustawienie skali
        this.Scale = 0.5f;

        // Ustawienie prostokąta źródłowego dla pierwszej klatki
        this.Sprite.SourceRect = new Rectangle(0, 0, frameWidth, frameHeight);

        // Tworzenie animacji
        List<FarmerSprite.AnimationFrame> animation = new List<FarmerSprite.AnimationFrame>();
        for (int i = 0; i < 4; i++)
        {
            animation.Add(new FarmerSprite.AnimationFrame(i, 150));
        }

        this.Sprite.CurrentAnimation = animation;
        this.Sprite.loop = true;
        this.Sprite.interval = 175f;

        // Właściwości potwora
        this.Health = 100;
        this.MaxHealth = 100;
        this.damageToFarmer.Value = 50;
    }

    public override void update(GameTime time, GameLocation location)
    {
        base.update(time, location);

        // Sprawdź, czy boss ma mniej niż 50% HP i jeszcze nie zaczął spawnować slimów
        if (this.Health <= this.MaxHealth / 2 && !hasSpawnedSlimes)
        {
            // Rozpocznij procedurę spawnowania slimów
            hasSpawnedSlimes = true;
            spawnTimer = 0;
        }

        // Jeśli trwa spawnowanie slimów
        if (hasSpawnedSlimes && slimesToSpawn > 0)
        {
            spawnTimer++;

            // Co sekundę (60 klatek) spawnuj nowego slime'a
            if (spawnTimer >= SPAWN_INTERVAL)
            {
                SpawnSlime(location);
                spawnTimer = 0;
                slimesToSpawn--;
            }
        }
    }

    private void SpawnSlime(GameLocation location)
    {
        // Stwórz nowego slime'a w losowym miejscu wokół bossa
        Random random = new Random();
        Vector2 offset = new Vector2(
            (float)random.Next(-128, 128),
            (float)random.Next(-128, 128)
        );

        Vector2 slimePosition = this.Position + offset;

        // Tworzenie nowego slime'a
        // Używamy standardowego Green Slime z gry
        GreenSlime slime = new GreenSlime(slimePosition, 0);

        // Ustawienie właściwości slime'a
        slime.Health = 50;
        slime.MaxHealth = 50;
        slime.Scale = 0.75f; // Nieco mniejszy niż boss

        // Dodanie slime'a do lokacji
        location.characters.Add(slime);

        // Efekt wizualny pojawienia się slime'a
        location.temporarySprites.Add(new TemporaryAnimatedSprite(
            6, // Indeks animacji eksplozji
            slimePosition,
            Color.LightGreen,
            8, // Ilość klatek
            false,
            100f, // Czas trwania każdej klatki
            0,
            -1,
            -1f,
            -1,
            0
        ));

        // Efekt dźwiękowy
        Game1.playSound("slime");
    }

    //// Opcjonalnie: dodaj wizualny wskaźnik kiedy boss zaczyna spawnować slime'y
    //public override void draw(SpriteBatch b)
    //{
    //    base.draw(b);

    //    // Wizualny wskaźnik że boss jest w trybie spawnowania
    //    if (hasSpawnedSlimes && slimesToSpawn > 0)
    //    {
    //        // Dodaj efekt świecenia wokół bossa
    //        float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 100.0f) * 0.5f + 0.5f;
    //        Vector2 standingPosition = this.getStandingPosition();

    //        Vector2 drawPosition = Game1.GlobalToLocal(
    //            Game1.viewport,
    //            new Vector2(
    //                this.Position.X + this.Sprite.SpriteWidth * 0.5f,
    //                this.Position.Y + this.Sprite.SpriteHeight * 0.5f
    //            )
    //        );

    //        float size = Math.Max(this.Sprite.SpriteWidth, this.Sprite.SpriteHeight) * this.Scale * 1.5f;
    //        b.Draw(
    //            Game1.mouseCursors,
    //            drawPosition,
    //            new Rectangle(331, 1985, 28, 28), // Efekt świecenia z tekstury gry
    //            Color.LightGreen * 0.5f * pulse,
    //            0f,
    //            new Vector2(14f, 14f),
    //            size / 28f,
    //            SpriteEffects.None,
    //            standingPosition.Y / 10000f - 0.001f
    //        );
    //    }
    //}
}
