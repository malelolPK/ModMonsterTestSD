using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using System.Reflection;
using StardewValley.Extensions;
using Color = Microsoft.Xna.Framework.Color;
using StardewValley.Menus;
using Netcode;

using SNAPI_monster_test.Bosses;
using xTile.Tiles;

// refrection dostanie się do data nawet jakbyś nie powineneś
// typeof(GameLocation).GetField("seasonOverride")

namespace SNAPI_monster_test
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        public static bool IsBossAlive = true;
        public static int timeUntilElevatorLightUp = 150;

        public override void Entry(IModHelper helper)
        {
            // Rejestracja komendy debugującej
            //helper.ConsoleCommands.Add("spawn_boss", "Spawnuje bossa", this.SpawnBossCommand);
            //helper.Events.Input.ButtonPressed += this.OnbuttonPressed;

            helper.Events.Player.Warped += PlayerWarpedMine10;
            GameLocation.RegisterTileAction("SlimeBossDoor", this.HandleUnlockGate);
        }

        private void PlayerWarpedMine10(object? sender, WarpedEventArgs e)
        {

            if (e.NewLocation is MineShaft mineshaft)
            {
                if (mineshaft.mineLevel == 10)

                {
                    if (IsBossAlive)
                    {

                        Game1.player.Position = new Vector2(29 * 64, 15 * 64); // wspórzedne są mnożone przez 64 rozmiar płytki zatem jakbym chciał aby był na płątce 10, 20 to robię 10*64, 20*64

                        bool isBossIsDefended = false;

                        GameLocation location = mineshaft;
                        SlimeBoss boss = new SlimeBoss("SlimeBoss", new Vector2(16 * 64, 33 * 64), 2, this.Monitor);


                        location.characters.Add(boss);
                        this.Monitor.Log("Boss został przyzwany");

                        Point eleventorLight = new Point(4, 11);


                        //GameTime time = new GameTime();
                        //this.Monitor.Log($"time {time}", LogLevel.Debug);

                        //if (timeUntilElevatorLightUp > 0)
                        //{
                        //    timeUntilElevatorLightUp -= time.ElapsedGameTime.Milliseconds;

                        //    if (timeUntilElevatorLightUp <= 0)
                        //    {
                        //        int? pitch = 0;

                        //    }
                        //}
                        if (eleventorLight.X != -1 && eleventorLight.Y != -1)
                        {
                            xTile.Tiles.StaticTile customTile = new xTile.Tiles.StaticTile(
                                mineshaft.map.GetLayer("Buildings"),
                                mineshaft.map.GetTileSheet("mine_frost_dark_dangerousNORMAL"),
                                BlendMode.Alpha,
                                48
                               );
                            mineshaft.map.GetLayer("Buildings").Tiles[eleventorLight.X, eleventorLight.Y] = customTile;
                            //mineshaft.setMapTile(eleventorLight.X, eleventorLight.Y, 48, "Buildings", "mine_frost_dark_dangerousNORMAL");
                            Game1.currentLightSources.Add(new LightSource($"Mine_{mineshaft.mineLevel}_Elevator", 4, new Vector2(eleventorLight.X, eleventorLight.Y) * 64f, 2f, Color.Black, LightSource.LightContext.None, 0L, mineshaft.NameOrUniqueName));

                            Type type = typeof(MineShaft);
                            FieldInfo elevatorShouldDingField = type.GetField("elevatorShouldDing", BindingFlags.NonPublic | BindingFlags.Instance);

                            if (elevatorShouldDingField != null)
                            {
                                // Pobierz obiekt NetBool
                                NetBool elevatorShouldDing = (NetBool)elevatorShouldDingField.GetValue(mineshaft);
                                // Ustaw jego wartość
                                elevatorShouldDing.Value = false;

                                this.Monitor.Log("Ustawiono elevatorShouldDing.Value = false", LogLevel.Debug);
                            }
                        }

                    }
                    if (mineshaft.mineLevel == 11)
                    {
                        IsBossAlive = false;
                    }
                }
            }
        }


        private bool HandleUnlockGate(GameLocation location, string[] args, Farmer player, Microsoft.Xna.Framework.Point point)
        {
            const string flagaBoss = "SlimeBoss";

            // Sprawdź czy boss został pokonany
            if (!player.mailReceived.Contains(flagaBoss))
            {
                Game1.activeClickableMenu = new DialogueBox("Opierdol kiełbase draskusa zanim przejdziesz dalej...");
                return false;
            }

            Game1.drawObjectDialogue("Drzwi otworzyły się!");

            RemoveDoorAnimation(location, point);



            return true;
        }
        private void RemoveDoorAnimation(GameLocation location, Microsoft.Xna.Framework.Point point)
        {
            // Dodaj zdarzenie aktualizacji gry
            int remainingDoors = 4;
            int blackFront = 14;
            bool doorsRemoved = false;

            this.Helper.Events.GameLoop.UpdateTicked += (sender, e) =>
            {
                // Wykonuj co 30 ticków (około pół sekundy)
                if (!doorsRemoved && e.Ticks % 30 == 0 && remainingDoors > 0)
                {
                    xTile.Layers.Layer buildingsLayer = location.Map.GetLayer("Buildings");
                    if (buildingsLayer != null)
                    {
                        buildingsLayer.Tiles[point.X - 1, point.Y - (4 - remainingDoors)] = null;
                        buildingsLayer.Tiles[point.X, point.Y - (4 - remainingDoors)] = null;
                        buildingsLayer.Tiles[point.X + 1, point.Y - (4 - remainingDoors)] = null;
                        remainingDoors--;
                        // Zagraj dźwięk
                        Game1.playSound("doorClose");

                        // Ustaw flagę, gdy wszystkie drzwi zostały usunięte
                        if (remainingDoors == 0)
                        {
                            doorsRemoved = true;
                        }
                    }
                }

                // Po usunięciu drzwi, usuń cały kwadrat czarnych części z przodu
                if (doorsRemoved && blackFront >= 0)
                {
                    xTile.Layers.Layer frontsLayer = location.Map.GetLayer("Front");
                    if (frontsLayer != null)
                    {
                        // Usuń cały kwadrat 14x14 kafelków
                        for (int x = point.X - 14; x < point.X + 14; x++)
                        {
                            for (int y = point.Y - 12; y < point.Y + 12; y++)
                            {
                                frontsLayer.Tiles[x, y] = null;
                            }
                        }
                        blackFront = -1; // Ustawiamy od razu na -1 żeby nie powtarzać tej operacji
                    }
                }
            };
        }

        //private void OnbuttonPressed(object? sender, ButtonPressedEventArgs e)
        //{
        //    if (Context.IsWorldReady && e.Button == SButton.K)
        //    {
        //        SpawnBossCommand("spawn_boss", Array.Empty<string>());
        //    }
        //}

        //private void SpawnBossCommand(string command, string[] args)
        //{
        //    GameLocation location = Game1.player.currentLocation;

        //    SlimeBoss boss = new SlimeBoss("SlimeBoss", new Vector2(Game1.player.Position.X + 64, Game1.player.Position.Y), 2, this.Monitor);


        //    // Dodaj bossa do lokacji
        //    location.characters.Add(boss);
        //    Monitor.Log("Boss został spawniony!", LogLevel.Info);
        //}

    }
}
