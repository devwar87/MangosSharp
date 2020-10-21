﻿//
//  Copyright (C) 2013-2020 getMaNGOS <https://getmangos.eu>
//  
//  This program is free software. You can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation. either version 2 of the License, or
//  (at your option) any later version.
//  
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY. Without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Mangos.Cluster.Globals;
using Mangos.Cluster.Handlers.Guild;
using Mangos.Cluster.Network;
using Mangos.Common.Enums.Chat;
using Mangos.Common.Enums.Global;
using Mangos.Common.Enums.Group;
using Mangos.Common.Enums.Guild;
using Mangos.Common.Enums.Misc;
using Mangos.Common.Enums.Player;
using Mangos.Common.Enums.Social;
using Mangos.Common.Globals;
using Mangos.Common.Legacy;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Mangos.Cluster.Handlers
{
    public class WcHandlerCharacter
    {
        private readonly ClusterServiceLocator clusterServiceLocator;

        public WcHandlerCharacter(ClusterServiceLocator clusterServiceLocator)
        {
            this.clusterServiceLocator = clusterServiceLocator;
        }

        public class CharacterObject : IDisposable
        {
            private readonly ClusterServiceLocator clusterServiceLocator;

            public CharacterObject(ulong g, ClientClass objCharacter, ClusterServiceLocator clusterServiceLocator)
            {
                this.clusterServiceLocator = clusterServiceLocator;
                ChatFlag = ChatFlag.FLAGS_NONE;
                Guid = g;
                Client = objCharacter;
                ReLoad();
                Access = Client.Access;
                var argobjCharacter = this;
                this.clusterServiceLocator._WC_Handlers_Social.LoadIgnoreList(argobjCharacter);
                this.clusterServiceLocator._WorldCluster.CHARACTERs_Lock.AcquireWriterLock(this.clusterServiceLocator._Global_Constants.DEFAULT_LOCK_TIMEOUT);
                this.clusterServiceLocator._WorldCluster.CHARACTERs.Add(Guid, this);
                this.clusterServiceLocator._WorldCluster.CHARACTERs_Lock.ReleaseWriterLock();
            }

            public ulong Guid;
            public ClientClass Client;
            public bool IsInWorld = false;
            public uint Map;
            public uint Zone;
            public float PositionX;
            public float PositionY;
            public float PositionZ;
            public float PositionO;
            public AccessLevel Access;
            public string Name;
            public int Level;
            public Races Race;
            public Classes Classe;
            public byte Gender;
            public DateTime Time = DateAndTime.Now;
            public int Latency = 0;
            public List<ulong> IgnoreList = new List<ulong>();
            public List<string> JoinedChannels = new List<string>();
            public bool AFK;
            public bool DND;
            public string AfkMessage;
            public uint GuildInvited = 0U;
            public ulong GuildInvitedBy = 0UL;
            public WC_Guild.Guild Guild = null;
            public byte GuildRank = 0;
            public WC_Handlers_Group.Group Group = null;
            public bool GroupAssistant = false;
            public bool GroupInvitedFlag = false;

            public bool IsInGroup => Group is object && GroupInvitedFlag == false;

            public bool IsGroupLeader
            {
                get
                {
                    if (Group is null)
                        return false;
                    return ReferenceEquals(Group.Members[Group.Leader], this);
                }
            }

            public bool IsInRaid => Group is object && Group.Type == GroupType.RAID;

            public bool IsInGuild => Guild is object;

            public bool IsGuildLeader => Guild is object && Guild.Leader == Guid;

            public bool IsGuildRightSet(GuildRankRights rights)
            {
                return Guild is object && (Guild.RankRights[GuildRank] & (uint)rights) == (uint)rights;
            }

            public bool Side
            {
                get
                {
                    switch (Race)
                    {
                        case var @case when @case == Races.RACE_DWARF:
                        case var case1 when case1 == Races.RACE_GNOME:
                        case var case2 when case2 == Races.RACE_HUMAN:
                        case var case3 when case3 == Races.RACE_NIGHT_ELF:
                            {
                                return false;
                            }

                        default:
                            {
                                return true;
                            }
                    }
                }
            }

            public IWorld GetWorld => clusterServiceLocator._WC_Network.WorldServer.Worlds[Map];

            public void ReLoad()
            {
                // DONE: Get character info from DB
                var MySQLQuery = new DataTable();
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Query(string.Format("SELECT * FROM characters WHERE char_guid = {0};", Guid), ref MySQLQuery);
                if (MySQLQuery.Rows.Count > 0)
                {
                    Race = (Races)MySQLQuery.Rows[0].As<byte>("char_race");
                    Classe = (Classes)MySQLQuery.Rows[0].As<byte>("char_class");
                    Gender = MySQLQuery.Rows[0].As<byte>("char_gender");
                    Name = MySQLQuery.Rows[0].As<string>("char_name");
                    Level = MySQLQuery.Rows[0].As<byte>("char_level");
                    Zone = MySQLQuery.Rows[0].As<uint>("char_zone_id");
                    Map = MySQLQuery.Rows[0].As<uint>("char_map_id");
                    PositionX = MySQLQuery.Rows[0].As<float>("char_positionX");
                    PositionY = MySQLQuery.Rows[0].As<float>("char_positionY");
                    PositionZ = MySQLQuery.Rows[0].As<float>("char_positionZ");

                    // DONE: Get guild info
                    uint GuildID = MySQLQuery.Rows[0].As<uint>("char_guildId");
                    if (GuildID > 0L)
                    {
                        if (clusterServiceLocator._WC_Guild.GUILDs.ContainsKey(GuildID) == false)
                        {
                            var tmpGuild = new WC_Guild.Guild(GuildID);
                            Guild = tmpGuild;
                        }
                        else
                        {
                            Guild = clusterServiceLocator._WC_Guild.GUILDs[GuildID];
                        }

                        GuildRank = MySQLQuery.Rows[0].As<byte>("char_guildRank");
                    }
                }
                else
                {
                    clusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DATABASE, "Failed to load expected results from:");
                    clusterServiceLocator._WorldCluster.Log.WriteLine(LogType.DATABASE, string.Format("SELECT * FROM characters WHERE char_guid = {0};", Guid));
                }
            }

            /* TODO ERROR: Skipped RegionDirectiveTrivia */
            private bool _disposedValue; // To detect redundant calls

            // IDisposable
            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                    // TODO: set large fields to null.
                    Client = null;

                    // DONE: Update character status in database
                    clusterServiceLocator._WorldCluster.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_online = 0, char_logouttime = '{1}' WHERE char_guid = '{0}';", Guid, clusterServiceLocator._Functions.GetTimestamp(DateAndTime.Now)));

                    // NOTE: Don't leave group on normal disconnect, only on logout
                    if (IsInGroup)
                    {
                        // DONE: Tell the group the member is offline
                        var response = clusterServiceLocator._Functions.BuildPartyMemberStatsOffline(Guid);
                        Group.Broadcast(response);
                        response.Dispose();

                        // DONE: Set new leader and loot master
                        Group.NewLeader(this);
                        Group.SendGroupList();
                    }

                    // DONE: Notify friends for logout
                    var argobjCharacter = this;
                    clusterServiceLocator._WC_Handlers_Social.NotifyFriendStatus(argobjCharacter, (FriendStatus)FriendResult.FRIEND_OFFLINE);

                    // DONE: Notify guild for logout
                    if (IsInGuild)
                    {
                        var argobjCharacter1 = this;
                        clusterServiceLocator._WC_Guild.NotifyGuildStatus(argobjCharacter1, GuildEvent.SIGNED_OFF);
                    }

                    // DONE: Leave chat
                    while (JoinedChannels.Count > 0)
                    {
                        if (clusterServiceLocator._WS_Handler_Channels.CHAT_CHANNELs.ContainsKey(JoinedChannels[0]))
                        {
                            var argCharacter = this;
                            clusterServiceLocator._WS_Handler_Channels.CHAT_CHANNELs[JoinedChannels[0]].Part(argCharacter);
                        }
                        else
                        {
                            JoinedChannels.RemoveAt(0);
                        }
                    }

                    clusterServiceLocator._WorldCluster.CHARACTERs_Lock.AcquireWriterLock(clusterServiceLocator._Global_Constants.DEFAULT_LOCK_TIMEOUT);
                    clusterServiceLocator._WorldCluster.CHARACTERs.Remove(Guid);
                    clusterServiceLocator._WorldCluster.CHARACTERs_Lock.ReleaseWriterLock();
                }

                _disposedValue = true;
            }

            // This code added by Visual Basic to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            /* TODO ERROR: Skipped EndRegionDirectiveTrivia */
            public void Transfer(float posX, float posY, float posZ, float ori, int thisMap)
            {
                var p = new PacketClass(Opcodes.SMSG_TRANSFER_PENDING);
                p.AddInt32(thisMap);
                Client.Send(p);
                p.Dispose();

                // Actions Here
                IsInWorld = false;
                GetWorld.ClientDisconnect(Client.Index);
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_positionX = {0}, char_positionY = {1}, char_positionZ = {2}, char_orientation = {3}, char_map_id = {4} WHERE char_guid = {5};", Strings.Trim(Conversion.Str(posX)), Strings.Trim(Conversion.Str(posY)), Strings.Trim(Conversion.Str(posZ)), Strings.Trim(Conversion.Str(ori)), thisMap, Guid));

                // Do global transfer
                clusterServiceLocator._WC_Network.WorldServer.ClientTransfer(Client.Index, posX, posY, posZ, ori, (uint)thisMap);
            }

            public void Transfer(float posX, float posY, float posZ, float ori)
            {
                var p = new PacketClass(Opcodes.SMSG_TRANSFER_PENDING);
                p.AddInt32((int)Map);
                Client.Send(p);
                p.Dispose();

                // Actions Here
                IsInWorld = false;
                GetWorld.ClientDisconnect(Client.Index);
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Update(string.Format("UPDATE characters SET char_positionX = {0}, char_positionY = {1}, char_positionZ = {2}, char_orientation = {3}, char_map_id = {4} WHERE char_guid = {5};", Strings.Trim(Conversion.Str(posX)), Strings.Trim(Conversion.Str(posY)), Strings.Trim(Conversion.Str(posZ)), Strings.Trim(Conversion.Str(ori)), Map, Guid));

                // Do global transfer
                clusterServiceLocator._WC_Network.WorldServer.ClientTransfer(Client.Index, posX, posY, posZ, ori, Map);
            }
            // Login
            public void OnLogin()
            {
                // DONE: Update character status in database
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Update("UPDATE characters SET char_online = 1 WHERE char_guid = " + Guid + ";");

                // DONE: SMSG_ACCOUNT_DATA_MD5
                var argcharacter = this;
                clusterServiceLocator._Functions.SendAccountMD5(Client, argcharacter);

                // DONE: SMSG_TRIGGER_CINEMATIC
                var q = new DataTable();
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Query(string.Format("SELECT char_moviePlayed FROM characters WHERE char_guid = {0} AND char_moviePlayed = 0;", Guid), ref q);
                if (q.Rows.Count > 0)
                {
                    clusterServiceLocator._WorldCluster.GetCharacterDatabase().Update("UPDATE characters SET char_moviePlayed = 1 WHERE char_guid = " + Guid + ";");
                    var argcharacter1 = this;
                    clusterServiceLocator._Functions.SendTriggerCinematic(Client, argcharacter1);
                }

                // DONE: SMSG_LOGIN_SETTIMESPEED
                var argcharacter2 = this;
                clusterServiceLocator._Functions.SendGameTime(Client, argcharacter2);

                // DONE: Server Message Of The Day
                clusterServiceLocator._Functions.SendMessageMOTD(Client, "Welcome to World of Warcraft.");
                clusterServiceLocator._Functions.SendMessageMOTD(Client, string.Format("This server is using {0} v.{1}", clusterServiceLocator._Functions.SetColor("[mangosVB]", 255, 0, 0), Assembly.GetExecutingAssembly().GetName().Version));

                // DONE: Guild Message Of The Day
                var argobjCharacter = this;
                clusterServiceLocator._WC_Guild.SendGuildMOTD(argobjCharacter);

                // DONE: Social lists
                var argcharacter3 = this;
                clusterServiceLocator._WC_Handlers_Social.SendFriendList(Client, argcharacter3);
                var argcharacter4 = this;
                clusterServiceLocator._WC_Handlers_Social.SendIgnoreList(Client, argcharacter4);

                // DONE: Send "Friend online"
                var argobjCharacter1 = this;
                clusterServiceLocator._WC_Handlers_Social.NotifyFriendStatus(argobjCharacter1, (FriendStatus)FriendResult.FRIEND_ONLINE);

                // DONE: Send online notify for guild
                var argobjCharacter2 = this;
                clusterServiceLocator._WC_Guild.NotifyGuildStatus(argobjCharacter2, GuildEvent.SIGNED_ON);

                // DONE: Put back character in group if disconnected
                foreach (KeyValuePair<long, WC_Handlers_Group.Group> tmpGroup in clusterServiceLocator._WC_Handlers_Group.GROUPs)
                {
                    for (byte i = 0, loopTo = (byte)(tmpGroup.Value.Members.Length - 1); i <= loopTo; i++)
                    {
                        if (tmpGroup.Value.Members[i] is object && tmpGroup.Value.Members[i].Guid == Guid)
                        {
                            tmpGroup.Value.Members[i] = this;
                            tmpGroup.Value.SendGroupList();
                            var response = new PacketClass(0) { Data = GetWorld.GroupMemberStats(Guid, 0) };
                            var argobjCharacter3 = this;
                            tmpGroup.Value.BroadcastToOther(response, argobjCharacter3);
                            response.Dispose();
                            return;
                        }
                    }
                }
            }

            public void OnLogout()
            {
                // DONE: Leave group
                if (IsInGroup)
                {
                    var argobjCharacter = this;
                    Group.Leave(argobjCharacter);
                }

                // DONE: Leave chat
                while (JoinedChannels.Count > 0)
                {
                    if (clusterServiceLocator._WS_Handler_Channels.CHAT_CHANNELs.ContainsKey(JoinedChannels[0]))
                    {
                        var argCharacter = this;
                        clusterServiceLocator._WS_Handler_Channels.CHAT_CHANNELs[JoinedChannels[0]].Part(argCharacter);
                    }
                    else
                    {
                        JoinedChannels.RemoveAt(0);
                    }
                }
            }

            public void SendGuildUpdate()
            {
                uint GuildID = 0U;
                if (Guild is object)
                    GuildID = Guild.ID;
                GetWorld.GuildUpdate(Guid, GuildID, GuildRank);
            }

            public ChatFlag ChatFlag;

            public void SendChatMessage(ulong thisguid, string message, ChatMsg msgType, int msgLanguage, string channelName)
            {
                if (thisguid == 0m)
                    thisguid = Guid;
                if (string.IsNullOrEmpty(channelName))
                    channelName = "Global";
                var msgChatFlag = ChatFlag;
                if (msgType == ChatMsg.CHAT_MSG_WHISPER_INFORM || msgType == ChatMsg.CHAT_MSG_WHISPER)
                    msgChatFlag = clusterServiceLocator._WorldCluster.CHARACTERs[thisguid].ChatFlag;
                var packet = clusterServiceLocator._Functions.BuildChatMessage(thisguid, message, msgType, (LANGUAGES)msgLanguage, (byte)msgChatFlag, channelName);
                Client.Send(packet);
                packet.Dispose();
            }
        }

        public ulong GetCharacterGUIDByName(string Name)
        {
            ulong GUID = 0UL;
            clusterServiceLocator._WorldCluster.CHARACTERs_Lock.AcquireReaderLock(clusterServiceLocator._Global_Constants.DEFAULT_LOCK_TIMEOUT);
            foreach (KeyValuePair<ulong, CharacterObject> objCharacter in clusterServiceLocator._WorldCluster.CHARACTERs)
            {
                if (clusterServiceLocator._CommonFunctions.UppercaseFirstLetter(objCharacter.Value.Name) == clusterServiceLocator._CommonFunctions.UppercaseFirstLetter(Name))
                {
                    GUID = objCharacter.Value.Guid;
                    break;
                }
            }

            clusterServiceLocator._WorldCluster.CHARACTERs_Lock.ReleaseReaderLock();
            if (GUID == 0m)
            {
                var q = new DataTable();
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Query(string.Format("SELECT char_guid FROM characters WHERE char_name = \"{0}\";", clusterServiceLocator._Functions.EscapeString(Name)), ref q);
                if (q.Rows.Count > 0)
                {
                    return q.Rows[0].As<ulong>("char_guid");
                }
                else
                {
                    return 0UL;
                }
            }
            else
            {
                return GUID;
            }
        }

        public string GetCharacterNameByGUID(string GUID)
        {
            if (clusterServiceLocator._WorldCluster.CHARACTERs.ContainsKey(Conversions.ToULong(GUID)))
            {
                return clusterServiceLocator._WorldCluster.CHARACTERs[Conversions.ToULong(GUID)].Name;
            }
            else
            {
                var q = new DataTable();
                clusterServiceLocator._WorldCluster.GetCharacterDatabase().Query(string.Format("SELECT char_name FROM characters WHERE char_guid = \"{0}\";", GUID), ref q);
                if (q.Rows.Count > 0)
                {
                    return q.Rows[0].As<string>("char_name");
                }
                else
                {
                    return "";
                }
            }
        }
    }
}