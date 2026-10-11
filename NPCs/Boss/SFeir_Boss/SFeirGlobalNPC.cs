using Terraria;
using Terraria.ModLoader;

namespace Amphoreus.NPCs.Boss.SFeir_Boss
{
    public class SFeirGlobalNPC : GlobalNPC
    {
        public float speedMultiplier = 1f;
        public bool healFromDamage = false;
        public float goldMultiplier = 1f;
        public bool goldHurt = false;
        public int currentStage = 1;

        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            speedMultiplier = 1f;
            healFromDamage = false;
            goldMultiplier = 1f;
            goldHurt = false;
        }
    }
}