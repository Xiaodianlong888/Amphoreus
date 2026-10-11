using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.NPCs.Boss.SFeir_Boss
{
    public class LieManager
    {
        private NPC owner;
        private ActiveLie currentLie;
        private int lieTimer;
        private int lieCooldown = 0;

        private uint lastAnnounceTime = 0;
        private const uint ANNOUNCE_COOLDOWN = 180;

        private List<LieDefinition> lieLibrary = new List<LieDefinition>();

        public LieManager(NPC npc)
        {
            owner = npc;
            InitializeLies();
        }

        private void InitializeLies()
        {
            lieLibrary.Add(new LieDefinition
            {
                Id = "SpeedLie",
                AnnounceText = "我比风还要快！",
                SurfaceEffect = (npc, player) => {
                    var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
                    if (globalNPC != null)
                        globalNPC.speedMultiplier = 1.5f;
                    if ((uint)Main.GameUpdateCount - lastAnnounceTime > ANNOUNCE_COOLDOWN)
                    {
                        CombatText.NewText(player.getRect(), Color.LightBlue, "Boss加速了！");
                        lastAnnounceTime = (uint)Main.GameUpdateCount;
                    }
                },
                TrapEffect = (npc, player) => {
                    var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
                    if (globalNPC != null)
                        globalNPC.speedMultiplier = 3.0f;
                    CombatText.NewText(npc.getRect(), Color.Red, "骗到你咯！");
                    lieCooldown = 240;
                },
                DelayBeforeTrap = 180,
                Duration = 300,
                Stage = 1
            });

            lieLibrary.Add(new LieDefinition
            {
                Id = "DefenseLie",
                AnnounceText = "你的攻击伤不到我！",
                SurfaceEffect = (npc, player) => {
                    npc.defense += 25;
                    if ((uint)Main.GameUpdateCount - lastAnnounceTime > ANNOUNCE_COOLDOWN)
                    {
                        CombatText.NewText(player.getRect(), Color.LightBlue, "Boss防御提升了！");
                        lastAnnounceTime = (uint)Main.GameUpdateCount;
                    }
                },
                TrapEffect = (npc, player) => {
                    var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
                    if (globalNPC != null)
                        globalNPC.healFromDamage = true;
                    CombatText.NewText(npc.getRect(), Color.Red, "都说了没用啦~");
                    lieCooldown = 240;
                },
                DelayBeforeTrap = 120,
                Duration = 300,
                Stage = 2
            });

            lieLibrary.Add(new LieDefinition
            {
                Id = "GoldLie",
                AnnounceText = "我的金币会咬人哦！",
                SurfaceEffect = (npc, player) => {
                    var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
                    if (globalNPC != null)
                        globalNPC.goldMultiplier = 2f;
                    if ((uint)Main.GameUpdateCount - lastAnnounceTime > ANNOUNCE_COOLDOWN)
                    {
                        CombatText.NewText(player.getRect(), Color.Gold, "金币加倍！");
                        lastAnnounceTime = (uint)Main.GameUpdateCount;
                    }
                },
                TrapEffect = (npc, player) => {
                    var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
                    if (globalNPC != null)
                        globalNPC.goldHurt = true;
                    CombatText.NewText(npc.getRect(), Color.Red, "贪婪的代价~");
                    lieCooldown = 300;
                },
                DelayBeforeTrap = 180,
                Duration = 360,
                Stage = 2
            });
        }

        public void OnCoinResult(int coinFace)
        {
            if (lieCooldown > 0)
                return;
            if (coinFace == 1)
                CombatText.NewText(owner.getRect(), Color.Gold, "正面！", true);
            else
                CombatText.NewText(owner.getRect(), Color.Silver, "反面~", true);
            ActivateRandomLie();
        }

        public void ActivateRandomLie()
        {
            if (lieLibrary.Count == 0 || lieCooldown > 0)
                return;
            int currentStage = owner.GetGlobalNPC<SFeirGlobalNPC>()?.currentStage ?? 1;
            var availableLies = lieLibrary.FindAll(l => l.Stage <= currentStage);
            if (availableLies.Count == 0)
                return;
            int index = Main.rand.Next(availableLies.Count);
            currentLie = new ActiveLie(availableLies[index]);
            Player target = Main.player[owner.target];
            if (target != null && target.active)
            {
                currentLie.Activate(owner, target);
                if ((uint)Main.GameUpdateCount - lastAnnounceTime > ANNOUNCE_COOLDOWN)
                {
                    ShowLieAnnouncement(currentLie.Definition.AnnounceText);
                    lastAnnounceTime = (uint)Main.GameUpdateCount;
                }
            }
        }

        public void Update()
        {
            if (lieCooldown > 0)
                lieCooldown--;
            if (currentLie != null && currentLie.IsActive)
            {
                lieTimer++;
                currentLie.Update(owner, lieTimer);
                if (!currentLie.TrapTriggered && lieTimer >= currentLie.Definition.DelayBeforeTrap)
                {
                    Player target = Main.player[owner.target];
                    if (target != null && target.active)
                        currentLie.TriggerTrap(owner, target);
                }
                if (lieTimer >= currentLie.Definition.Duration)
                {
                    currentLie.End(owner);
                    currentLie = null;
                    lieTimer = 0;
                    lieCooldown = 120;
                }
            }
        }

        private void ShowLieAnnouncement(string text)
        {
            CombatText.NewText(owner.getRect(), new Color(255, 105, 180), $"【谎言】{text}", true);
            if (Main.netMode != NetmodeID.Server)
                Main.NewText($"【谎言】{text}", new Color(255, 105, 180));
        }
    }

    public class LieDefinition
    {
        public string Id;
        public string AnnounceText;
        public Action<NPC, Player> SurfaceEffect;
        public Action<NPC, Player> TrapEffect;
        public int DelayBeforeTrap;
        public int Duration;
        public int Stage;
    }

    public class ActiveLie
    {
        public LieDefinition Definition;
        public bool IsActive;
        public bool TrapTriggered;

        public ActiveLie(LieDefinition def)
        {
            Definition = def;
        }

        public void Activate(NPC npc, Player target)
        {
            IsActive = true;
            TrapTriggered = false;
            Definition.SurfaceEffect?.Invoke(npc, target);
        }

        public void Update(NPC npc, int timer)
        {
            // 持续效果可以在这里实现
        }

        public void TriggerTrap(NPC npc, Player target)
        {
            if (!TrapTriggered)
            {
                TrapTriggered = true;
                Definition.TrapEffect?.Invoke(npc, target);
            }
        }

        public void End(NPC npc)
        {
            IsActive = false;
            npc.defense = Math.Max(20, npc.defense - 25);
            var globalNPC = npc.GetGlobalNPC<SFeirGlobalNPC>();
            if (globalNPC != null)
            {
                globalNPC.speedMultiplier = 1f;
                globalNPC.healFromDamage = false;
                globalNPC.goldMultiplier = 1f;
                globalNPC.goldHurt = false;
            }
        }
    }
}