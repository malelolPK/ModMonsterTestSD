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
    private float scale = 0.75f; // Zmniejszona skala - dostosuj według potrzeb

    public SlimeBoss(string name, Vector2 position, int facingDir)
        : base(name, position, facingDir)
    {
        // Ustawiamy oryginalny rozmiar sprite'a (zgodny z rozmiarem pliku PNG)
        this.Sprite.spriteWidth.Value = 60;  // Szerokość oryginalnego obrazka
        this.Sprite.spriteHeight.Value = 70; // Wysokość oryginalnego obrazka

        // Definiowanie animacji
        this.Sprite.SpriteWidth = 60;
        this.Sprite.SpriteHeight = 70;


        this.Sprite = new AnimatedSprite("Characters\\Monsters\\SlimeBoss");
        this.Sprite.CurrentFrame = 0; // Ustawienie pierwszej klatki animacji
        this.Sprite.interval = 150f; // Szybkość animacji (ms)
        this.Sprite.Animate(Game1.currentGameTime, 0, 4, 150f); // Animacja 4 klatek


        // Dodatkowe właściwości
        this.Health = 100;
        this.MaxHealth = 100;
        this.damageToFarmer.Value = 50;
        this.defaultAnimationInterval.Value = 200; // Szybsza animacja
    }



}