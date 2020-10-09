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
    public class CreatureAI_Lucifron : Mangos.World.AI.WS_Creatures_AI.BossAI
    {
        private const int AI_UPDATE = 1000;
        private const int Impending_Doom_Cooldown = 20000;
        private const int Lucifrons_Curse_Cooldown = 20000;
        private const int Shadow_Shock_Cooldown = 6000;
        private const int Impending_Doom = 19702;
        private const int Lucifrons_Curse = 19703;
        private const int Shadow_Shock = 19460;
        public int Phase = 0;
        public int NextImpendingDoom = 0;
        public int NextLucifronsCurse = 0;
        public int NextShadowShock = 0;
        public int NextWaypoint = 0;
        public int CurrentWaypoint = 0;

        public CreatureAI_Lucifron(ref Mangos.World.Objects.WS_Creatures.CreatureObject Creature) : base(ref Creature)
        {
            Phase = 0;
            this.AllowedMove = false;
            Creature.Flying = false;
            Creature.VisibleDistance = 700f;
        }

        public override void OnEnterCombat()
        {
            if (Phase > 1)
                return;
            base.OnEnterCombat();
            this.aiCreature.Flying = false;
            this.AllowedAttack = true;
            Phase = 1;
            // ReinitSpells()
        }

        public override void OnLeaveCombat(bool Reset = true)
        {
            base.OnLeaveCombat(Reset);
            this.AllowedAttack = true;
            Phase = 0;
        }

        public override void OnKill(ref Mangos.World.Objects.WS_Base.BaseUnit Victim)
        {
            // Does he cast a dummy spell on target death?
        }

        public override void OnDeath()
        {
            // Does he do anything on his own death?
        }

        public override void OnThink()
        {
            if (Phase < 1)
                return;
            if (Phase == 1)
            {
                NextImpendingDoom -= AI_UPDATE;
                NextLucifronsCurse -= AI_UPDATE;
                NextShadowShock -= AI_UPDATE;
                if (NextImpendingDoom <= 0)
                {
                    NextImpendingDoom = Impending_Doom_Cooldown;
                    this.aiCreature.CastSpell(Impending_Doom, this.aiTarget); // Impending DOOOOOM!
                }

                if (NextLucifronsCurse <= 0)
                {
                    NextLucifronsCurse = Lucifrons_Curse_Cooldown;
                    this.aiCreature.CastSpell(Lucifrons_Curse, this.aiTarget); // Lucifrons Curse.
                }

                if (NextShadowShock <= 0)
                {
                    NextShadowShock = Shadow_Shock_Cooldown;
                    this.aiCreature.CastSpell(Shadow_Shock, this.aiTarget); // Summon Player
                }
            }

            if (NextWaypoint > 0)
            {
                NextWaypoint -= AI_UPDATE;
                if (NextWaypoint <= 0)
                {
                    On_Waypoint();
                }
            }
        }

        public void Cast_Lucirons_Curse()
        {
            for (int i = 0; i <= 2; i++)
            {
                Mangos.World.Objects.WS_Base.BaseUnit theTarget = this.aiCreature;
                if (theTarget is null)
                    return;
                try
                {
                    this.aiCreature.CastSpell(Lucifrons_Curse, this.aiTarget);
                }
                catch (Exception Ex)
                {
                    this.aiCreature.SendChatMessage("Failed to cast Lucifron's Curse. This is bad. Please report to developers.", ChatMsg.CHAT_MSG_MONSTER_YELL, LANGUAGES.LANG_UNIVERSAL);
                }
            }
        }

        public void Cast_Impending_Doom()
        {
            for (int i = 1; i <= 2; i++)
            {
                Mangos.World.Objects.WS_Base.BaseUnit theTarget = this.aiCreature;
                if (theTarget is null)
                    return;
                try
                {
                    this.aiCreature.CastSpell(Impending_Doom, this.aiTarget);
                }
                catch (Exception Ex)
                {
                    this.aiCreature.SendChatMessage("Failed to cast IMPENDING DOOOOOM! Please report this to a developer.", ChatMsg.CHAT_MSG_MONSTER_YELL, LANGUAGES.LANG_UNIVERSAL);
                }
            }
        }

        public void Cast_Shadow_Shock()
        {
            for (int i = 2; i <= 2; i++)
            {
                var theTarget = this.aiCreature.GetRandomTarget();
                if (theTarget is null)
                    return;
                try
                {
                    this.aiCreature.CastSpell(Shadow_Shock, theTarget.positionX, theTarget.positionY, theTarget.positionZ);
                }
                catch (Exception Ex)
                {
                    this.aiCreature.SendChatMessage("Failed to cast Shadow Shock. Please report this to a developer.", ChatMsg.CHAT_MSG_MONSTER_YELL, LANGUAGES.LANG_UNIVERSAL);
                }
            }
        }

        public void On_Waypoint()
        {
            switch (CurrentWaypoint)
            {
                case 0:
                    {
                        NextWaypoint = this.aiCreature.MoveTo(-0.0f, -0.0f, -0.0f, 0.0f, true); // No Waypoint Coords! Will need to back track from MaNGOS!
                        break;
                    }

                case 1:
                    {
                        NextWaypoint = 10000;
                        // NextSummon = NextWaypoint
                        this.aiCreature.MoveTo(0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }

                case 2:
                    {
                        NextWaypoint = 23000;
                        break;
                    }

                case 3:
                    {
                        NextWaypoint = 10000;
                        this.aiCreature.MoveTo(0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }

                case 4:
                case 6:
                case 8:
                case 10:
                case 12:
                    {
                        NextWaypoint = 23000;
                        break;
                    }

                case 5:
                    {
                        NextWaypoint = 10000;
                        this.aiCreature.MoveTo(-0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }

                case 7:
                    {
                        NextWaypoint = 10000;
                        this.aiCreature.MoveTo(-0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }

                case 9:
                    {
                        NextWaypoint = 10000;
                        this.aiCreature.MoveTo(0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }

                case 11:
                    {
                        NextWaypoint = 10000;
                        this.aiCreature.MoveTo(-0.0f, -0.0f, -0.0f, 0.0f);
                        break;
                    }
            }

            CurrentWaypoint += 1;
            if (CurrentWaypoint > 12)
                CurrentWaypoint = 3;
        }
    }
}