using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using MonoMod.Cil;
using ReLogic.Graphics;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Terraria;
using Terraria.Localization; 
using Terraria.Utilities;
using Terraria.GameContent.Dyes;
using Terraria.GameContent.UI;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace telepotion
{
	public class telepotionBuff : ModBuff
	{
		public override void SetDefaults() {
			DisplayName.SetDefault("Telegraphing");
			Description.SetDefault("You are about to teleport");
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
			canBeCleared = false;
		}
		public override void Update(Player player, ref int buffIndex) {
			player.GetModPlayer<telepotionPlayer>().telepotionEffect = true;
			if (player.GetModPlayer<telepotionPlayer>().OpenMap && !Main.mapFullscreen) {
				CombatText.NewText(player.getRect(),Color.Pink,"Telegraph canceled");
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
	public class telepotionNPC : GlobalNPC
	{
		public override void NPCLoot(NPC npc) {
			if (Main.hardMode && !npc.boss && npc.lifeMax > 1 && npc.damage > 0 && !npc.friendly && npc.position.Y > Main.rockLayer * 16.0 && npc.value > 0f && Main.rand.NextBool(Main.expertMode ? 2 : 1, 5))
			{
				if (Main.player[Player.FindClosest(npc.position, npc.width, npc.height)].ZoneHoly && Main.rand.NextBool(200))
				{
					Item.NewItem(npc.getRect(), ModContent.ItemType<telepotionItem>()); // U get soul of light after some filtering
				}
			}
		}
	}
	public class telepotionItem : ModItem
	{
        public override void SetStaticDefaults()
        {
			DisplayName.SetDefault("Telegraph Potion");
			string map = " around the map";
			if (telepotionConfig.get.GoDiscovered) {map = " around discovered map";}
            Tooltip.SetDefault("Allows you to teleport"+map);
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 26;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Orange;
            item.value = Item.buyPrice(gold: 1);
            item.buffType = ModContent.BuffType<telepotionBuff>(); //Specify an existing buff to be applied when used.
            item.buffTime = 60000; //The amount of time the buff declared in item.buffType will last in ticks. 5400 / 60 is 90, so this buff will last 90 seconds.
        }
		bool white;
		public override void UpdateInventory(Player player) {
			if (!player.HasBuff(BuffID.ChaosState)) {item.color = Color.Lerp(item.color,Color.White,0.1f);}
			else {item.color = Color.Lerp(item.color,Color.Orange,0.1f);}
		}
		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.SoulofLight);
			recipe.AddIngredient(ItemID.TeleportationPotion);
			recipe.AddIngredient(ItemID.WormholePotion);
			recipe.AddIngredient(ItemID.CrystalShard,3);
			recipe.AddTile(TileID.Bottles);
			recipe.SetResult(this,5);
			recipe.AddRecipe();
		}
		public override void ModifyTooltips(List<TooltipLine> list) {
			if (Main.LocalPlayer.HasBuff(BuffID.ChaosState)) {
				list.Add(new TooltipLine(mod,"og","Cannot be used while in Chaos State"){overrideColor = Color.Red});
			}
        }
		public override bool CanUseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				if (player.HasBuff(BuffID.ChaosState)) {
					return false;
				}
			}
			return base.CanUseItem(player);
		}
		public override bool PreDrawTooltipLine(DrawableTooltipLine line, ref int yOffset) {
            if (line.mod == "Terraria" && line.Name == "ItemName")
            {
                Main.spriteBatch.End(); //end and begin main.spritebatch to apply a shader
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);
                GameShaders.Armor.Apply(GameShaders.Armor.GetShaderIdFromItemId(ItemID.BurningHadesDye), item, null); //use living rainbow dye shader
                Utils.DrawBorderString(Main.spriteBatch, line.text, new Vector2(line.X, line.Y), Color.White, 1); //draw the tooltip manually
                Main.spriteBatch.End(); //then end and begin again to make remaining tooltip lines draw in the default way
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
                return false;
            }
            return true;
        }
		/*
		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
			Color itemColor, Vector2 origin, float scale) {
			if (Main.LocalPlayer.HasBuff(BuffID.ChaosState)) {
				Main.spriteBatch.End(); 
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);
				GameShaders.Armor.Apply(GameShaders.Armor.GetShaderIdFromItemId(ItemID.BurningHadesDye), item, null);
			}
			return true;
		}
		public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,Color itemColor, Vector2 origin, float scale) {
			Main.spriteBatch.End(); //then end and begin again to make remaining tooltip lines draw in the default way
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
		}*/
    }
	public class telepotionPlayer : ModPlayer
	{
		public bool telepotionEffect;
		public bool OpenMap;
		public override void PostUpdate() {
			if (telepotionEffect) {
				if (Main.rand.NextBool(10)){Dust.NewDust(player.Center, player.width,player.height, 272, 0f, 0f, 0, Color.Orange, 1f);}
				if (!OpenMap) {
					telepotion.meScale = 2f;
					Main.mapFullscreen = true;
					Main.resetMapFull = true;
					OpenMap = true;
				}
			}
			else {OpenMap = false;}
		}
		public override void ResetEffects() {
			telepotionEffect = false;
		}
	}
	[Label("Telegraph Potion Mod Config")]
	public class telepotionConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ServerSide;
		public static telepotionConfig get => ModContent.GetInstance<telepotionConfig>();

		[Header("Telegraphic potion")]

		[Label("Teleport into tiles")]
		[Tooltip("Allow player to teleport into solid tiles")]
		[DefaultValue(true)]
		public bool GoSolid;

		[Label("Teleport into Discovered Area Only")]
		[Tooltip("Allow player to teleport into discovered area only")]
		[DefaultValue(true)]
		public bool GoDiscovered;

		[Label("Teleport Delay")]
		[Tooltip("The teleport delay in seconds")]
		[Range(0, 10)]
		[Increment(1)]
		[DefaultValue(3)]
		[Slider] 
		public int GoDelay;

	}
	public class telepotion : Mod
	{
		internal enum NetThing : byte
		{
			RequestTeleport
		}
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			NetThing msgType = (NetThing)reader.ReadByte();
			switch (msgType) {
				// This message sent by the server to initialize the Volcano Tremor on clients
				case NetThing.RequestTeleport:
					Vector2 destination = reader.ReadVector2();
					Main.player[whoAmI].Teleport(destination, 1, 0);
					RemoteClient.CheckSection(whoAmI, destination, 1);
					NetMessage.SendData(65, -1, -1, null, 0, whoAmI, destination.X, destination.Y, 1, 0, 0);
					break;
				default:
					Logger.WarnFormat("telepotion : Oh no netcode sucks {0}", msgType);
					break;
			}
		}
		public static float meScale = 2f;
		int meTimer;
		string meSay;
		public override void PostDrawFullscreenMap(ref string mouseText)
		{
			Player player = Main.LocalPlayer;
			telepotionPlayer p = player.GetModPlayer<telepotionPlayer>();
			if (!Main.gameMenu && p.telepotionEffect)
			{
				Main.spriteBatch.End(); //end and begin main.spritebatch to apply a shader
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.UIScaleMatrix);
                GameShaders.Armor.Apply(GameShaders.Armor.GetShaderIdFromItemId(ItemID.BurningHadesDye), player, null); //use living rainbow dye shader
				//math
				meScale = MathHelper.Lerp(meScale,1f,0.1f);
                Utils.DrawBorderString(Main.spriteBatch, "Right click to teleport", new Vector2(60, Main.screenHeight - 30), Color.White, meScale); //draw the tooltip manually
                Main.spriteBatch.End(); //then end and begin again to make remaining tooltip lines draw in the default way
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

				Terraria.GameInput.PlayerInput.SetZoom_Unscaled();

				if (meTimer > 0) {
					mouseText = meSay;
					meTimer--;
				}

				if (Main.mouseRight && Main.keyState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.LeftControl))
				{
					int mapWidth = Main.maxTilesX * 16;
					int mapHeight = Main.maxTilesY * 16;
					Vector2 cursorPosition = new Vector2(Main.mouseX, Main.mouseY);

					cursorPosition.X -= Main.screenWidth / 2;
					cursorPosition.Y -= Main.screenHeight / 2;

					Vector2 mapPosition = Main.mapFullscreenPos;
					Vector2 cursorWorldPosition = mapPosition;

					cursorPosition /= 16;
					cursorPosition *= 16 / Main.mapFullscreenScale;
					cursorWorldPosition += cursorPosition;
					cursorWorldPosition *= 16;
					cursorWorldPosition.Y -= player.height;
					if (cursorWorldPosition.X < 0) cursorWorldPosition.X = 0;
					else if (cursorWorldPosition.X + player.width > mapWidth) cursorWorldPosition.X = mapWidth - player.width;
					if (cursorWorldPosition.Y < 0) cursorWorldPosition.Y = 0;
					else if (cursorWorldPosition.Y + player.height > mapHeight) cursorWorldPosition.Y = mapHeight - player.height;

					if (Main.Map.IsRevealed((int)cursorWorldPosition.X/16,(int)cursorWorldPosition.Y/16)) {
						Tile tile = Framing.GetTileSafely(cursorWorldPosition);
						if ((!tile.active() && Main.tileSolid[tile.type]) || telepotionConfig.get.GoSolid) {
							if (Main.netMode == 0) // single
							{
								player.Teleport(cursorWorldPosition, 1, 0);
								player.position = cursorWorldPosition;
								player.velocity = Vector2.Zero;
								player.fallStart = (int)(player.position.Y / 16f);
								player.ClearBuff(ModContent.BuffType<telepotionBuff>());
								player.AddBuff(BuffID.ChaosState,60*telepotionConfig.get.GoDelay);
								Main.mapFullscreen = false;
								for (int i = 0; i < 50; i++) {
									Vector2 speed = Main.rand.NextVector2CircularEdge(1f, 1f);
									Dust d = Dust.NewDustPerfect(player.Center, 182, speed * 5, Scale: 1.5f);
									d.noGravity = true;
									d.color = Color.Orange;
								}
							}
							else // 1, client
							{
								//ErrorLogger.Log("Teleport");
								//HEROsModNetwork.GeneralMessages.RequestTeleport(cursorWorldPosition);
								var netMessage = GetPacket();
								netMessage.Write((byte)NetThing.RequestTeleport);
								netMessage.WriteVector2(cursorWorldPosition);
								netMessage.Send();
							}
						}
						else {
							meTimer = 30;
							meSay = "[c/"+Color.Red.Hex3()+": "+ "Cannot teleport to solid tiles]";
						}
					}
					else {
						meTimer = 30;
						meSay = "[c/"+Color.Red.Hex3()+": "+ "Cannot teleport to undiscovered area]";
					}
				}
				Terraria.GameInput.PlayerInput.SetZoom_UI();
			}
			else {meTimer = 0;}
		}
	}
}