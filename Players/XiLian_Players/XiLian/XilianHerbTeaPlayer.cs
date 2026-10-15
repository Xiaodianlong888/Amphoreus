using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Amphoreus.Players.XiLian_Players.XiLian
{
    /// <summary>
    /// 花茶系统：5 种花茶增益的实际效果。
    /// 每种 Buff 在 ModBuff.Update 中把自己的标志位置为 true，
    /// 这里在 ResetEffects 里统一清空，再按标志施加效果。
    /// </summary>
    public class XiLianHerbTeaPlayer : ModPlayer
    {
        // 花茶标志（由对应 ModBuff 每帧置位）
        public bool sunflowerActive;
        public bool moonflowerActive;
        public bool shiningRootActive;
        public bool fireblossomActive;
        public bool chillthornActive;

        private int lifeRegenTimer;
        private int manaRegenTimer;

        public override void ResetEffects()
        {
            sunflowerActive = false;
            moonflowerActive = false;
            shiningRootActive = false;
            fireblossomActive = false;
            chillthornActive = false;
        }

        /// <summary>月光草：生命上限 +50，魔力上限 +80（在原有基础上叠加）。</summary>
        public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
        {
            health = StatModifier.Default;
            mana = StatModifier.Default;

            if (moonflowerActive)
            {
                health.Base += 50;
                mana.Base += 80;
            }
        }

        public override void PostUpdateMiscEffects()
        {
            // ---- 太阳花：移动速度 ----
            if (sunflowerActive)
            {
                Player.moveSpeed += 0.1f;
            }

            // ---- 闪耀根：防御与减伤 ----
            if (shiningRootActive)
            {
                Player.statDefense += 5;
                Player.endurance *= 1.05f; // 5% 伤害减免，必须乘法叠加

                bool standingStill =
                    Player.velocity.Length() < 0.1f &&
                    !Player.controlLeft && !Player.controlRight &&
                    !Player.controlUp && !Player.controlDown &&
                    !Player.controlJump && !Player.mount.Active;

                if (standingStill)
                {
                    Player.endurance *= 1.05f; // 额外 5%
                    Player.aggro -= 400;       // 降低仇恨，敌人更不容易锁定
                }
            }

            // ---- 火焰花：免疫冰冻与霜火 ----
            if (fireblossomActive)
            {
                Player.buffImmune[BuffID.Frozen] = true;
                Player.buffImmune[BuffID.Chilled] = true;
                Player.buffImmune[BuffID.Frostburn] = true;
                Player.buffImmune[BuffID.Frostburn2] = true;
            }

            // ---- 寒颤棘：免疫着火与燃烧 ----
            if (chillthornActive)
            {
                Player.buffImmune[BuffID.OnFire] = true;
                Player.buffImmune[BuffID.OnFire3] = true;
                Player.buffImmune[BuffID.Burning] = true;
            }
        }

        /// <summary>太阳花：缓慢恢复生命（每 60 帧 1 点）。</summary>
        public override void UpdateLifeRegen()
        {
            if (!sunflowerActive)
            {
                lifeRegenTimer = 0;
                return;
            }

            lifeRegenTimer++;
            if (lifeRegenTimer >= 60)
            {
                lifeRegenTimer = 0;
                if (Player.statLife < Player.statLifeMax2)
                {
                    Player.statLife++;
                    Player.HealEffect(1);
                }
            }
        }

        /// <summary>月光草：缓慢恢复魔力（每 30 帧 1 点）。</summary>
        public override void PostUpdate()
        {
            if (!moonflowerActive)
            {
                manaRegenTimer = 0;
                return;
            }

            manaRegenTimer++;
            if (manaRegenTimer >= 30)
            {
                manaRegenTimer = 0;
                if (Player.statMana < Player.statManaMax2)
                {
                    Player.statMana++;
                    Player.ManaEffect(1);
                }
            }
        }
    }
}
