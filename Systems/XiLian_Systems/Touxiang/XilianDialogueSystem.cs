using Amphoreus.UI.XilianDialogueUI;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Amphoreus.Systems.XiLian_Systems.Touxiang
{
    public class XilianDialogueSystem : ModSystem
    {
        internal UserInterface _interface;
        internal XilianDialogueUI _dialogueUI;

        private bool _isActive = false;
        private int _currentNPCWhoAmI = -1;
        private const float CLOSE_DISTANCE = 300f;

        public bool IsActive => _isActive;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                _interface = new UserInterface();
                _dialogueUI = new XilianDialogueUI();
                _dialogueUI.Activate();
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (!_isActive) return;

            bool shouldClose = true;

            if (_currentNPCWhoAmI != -1)
            {
                NPC npc = Main.npc[_currentNPCWhoAmI];
                if (npc.active && npc.ModNPC is NPCs.XiLian.Xilian)
                {
                    float distance = Vector2.Distance(Main.LocalPlayer.Center, npc.Center);
                    if (distance <= CLOSE_DISTANCE)
                        shouldClose = false;
                }
            }

            if (Main.player[Main.myPlayer].dead || Main.playerInventory)
                shouldClose = true;

            if (shouldClose)
            {
                HideUI();
                return;
            }

            _interface?.Update(gameTime);
        }

        public void ShowDialogue(string text, NPC npc)
        {
            _dialogueUI.SetDialogue(text, npc);
            _interface.SetState(_dialogueUI);
            _isActive = true;
            _currentNPCWhoAmI = npc.whoAmI;
            Main.npcChatText = "";
        }

        public void HideUI()
        {
            _interface?.SetState(null);
            _isActive = false;
            _currentNPCWhoAmI = -1;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            var config = ModContent.GetInstance<Configs.UISettings>();
            if (!config.UseCustomDialogue) return;

            int chatIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Chat");
            if (chatIndex != -1)
                layers.RemoveAt(chatIndex);

            int mouseIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseIndex != -1)
            {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                    "Amphoreus: Xilian Dialogue",
                    () => {
                        if (_isActive)
                            _interface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}