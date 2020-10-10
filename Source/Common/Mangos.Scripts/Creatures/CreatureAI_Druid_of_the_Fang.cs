﻿// 
// Copyright (C) 2013-2020 getMaNGOS <https://getmangos.eu>
// 
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 

using System;
using Mangos.Common.Enums.Chat;
using Mangos.Common.Enums.Misc;

namespace Mangos.Scripts.Creatures
{
    public class CreatureAI_Druid_of_the_Fang : Mangos.World.AI.WS_Creatures_AI.BossAI
    {
        private const int AI_UPDATE = 1000;
        private const int SLUMBER_CD = 10000; // - Unable to implement this as for the time being due to threat issues in the core.
        private const int Healing_Touch_CD = 20000;
        private const int Serpent_Form_CD = 40000;
        private const int Lightning_Bolt_CD = 6000;
        private const int Slumber_Spell = 8040;
        private const int Healing_Spell = 23381;
        private const int Spell_Serpent_Form = 8041; // Not sure how this will work. 
        private const int Spell_Lightning_Bolt = 9532;
        public int NextWaypoint = 0;
        public int NextLightningBolt = 0;
        public int NextSerpentForm = 0;
        public int NextHealingTouch = 0;
        public int NextSlumber = 0;
        public int CurrentWaypoint = 0;

        public CreatureAI_Druid_of_the_Fang(ref Mangos.World.Objects.WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            this.AllowedMove = false;
            Creature.Flying = false;
            Creature.VisibleDistance = 700f;
        }

        public override void OnThink()
        {
            NextLightningBolt -= AI_UPDATE;
            NextSerpentForm -= AI_UPDATE;
            NextHealingTouch -= AI_UPDATE;
            NextSlumber -= AI_UPDATE;
            if (NextLightningBolt <= 0)
            {
                NextLightningBolt = Lightning_Bolt_CD;
                this.aiCreature.CastSpell(Spell_Lightning_Bolt, this.aiTarget); // Lightning bolt on current target.
            }
        }

        public void CastLightning()
        {
            for (int i = 0; i <= 3; i++)
            {
                Mangos.World.Objects.WS_Base.BaseUnit Target = this.aiCreature;
                if (Target is null)
                    return;
                this.aiCreature.CastSpell(Spell_Lightning_Bolt, this.aiTarget);
            }
        }

        public override void OnHealthChange(int Percent)
        {
            base.OnHealthChange(Percent);
            if (Percent <= 30)
            {
                try
                {
                    this.aiCreature.CastSpellOnSelf(Spell_Serpent_Form);
                }
                catch (Exception)
                {
                    this.aiCreature.SendChatMessage("I have failed to cast Serpent Form. This is a problem. Please report this to the developers.", ChatMsg.CHAT_MSG_MONSTER_YELL, LANGUAGES.LANG_UNIVERSAL);
                }
            }
        }
    }
}