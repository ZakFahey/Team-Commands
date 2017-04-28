using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace TeamCommands {
    [ApiVersion(2, 1)]
    public class TeamCommands : TerrariaPlugin {
        public TeamCommands(Main game) : base(game) {
        }
        public override void Initialize() {
            Commands.ChatCommands.Add(new Command("", TeamDo, "team") {
                HelpText = "Runs a command to an entire team. Format: /team [command]. Any user-specific TShock command will work."
            });
        }
        public override Version Version {
            get { return new Version("1.2"); }
        }
        public override string Name {
            get { return "Team Commands"; }
        }
        public override string Author {
            get { return "GameRoom"; }
        }
        public override string Description {
            get { return "Runs commands to entire team."; }
        }

        int TeamColorToID(string color, TSPlayer who) {
            switch (color.ToLower()) {
                case "none":
                case "white": return 0;
                case "red": return 1;
                case "green": return 2;
                case "blue": return 3;
                case "yellow": return 4;
                case "all": return 5;
                default: return who.Team;
            }
        }

        string TeamIDToColor(int ID, bool capitalize = true) {
            string ret;
            switch (ID) {
                case 0: ret = "The white team";
                    break;
                case 1: ret = "The red team";
                    break;
                case 2: ret = "The green team";
                    break;
                case 3: ret = "The blue team";
                    break;
                case 4: ret = "The yellow team";
                    break;
                case 5: ret = "Everyone";
                    break;
                default: ret = "";
                    break;
            }
            if (!capitalize) ret = ret.ToLower();
            return ret;
        }

        void TeamDo(CommandArgs e) {
            if (e.Parameters.Count == 0) e.Player.SendErrorMessage("Invalid syntax! Proper syntax: /team <command>. Type /team help for all commands.");
            else switch (e.Parameters[0]) {

                case "kick":
                    if (CommandIsValid(e, Permissions.kick, 2, "kick <team> [reason]"))
                        if (e.Parameters[1].Length == 0) e.Player.SendErrorMessage("Missing team name.");
                        else {
                            string reason = e.Parameters.Count > 2 ? String.Join(" ", e.Parameters.Skip(2)) : "Misbehavior.";
                            var team = TeamColorToID(e.Parameters[1], e.Player);
                            foreach (TSPlayer player in TShock.Players)
                                if (player != null && (team == player.Team || team == 5))
                                    TShock.Utils.Kick(player, reason, !e.Player.RealPlayer, true, e.Player.Name);
                                TShock.Utils.Broadcast(string.Format("{0} was kicked for '{1}'", TeamIDToColor(team), reason.ToLower()), Color.Green);
                        }
                    break;

                case "ban":
                    if (CommandIsValid(e, Permissions.ban, 2, "ban <team> [reason]")) {
                        string reason = e.Parameters.Count > 2 ? String.Join(" ", e.Parameters.Skip(2)) : "Misbehavior.";
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && (team == player.Team || team == 5) && (!player.Group.HasPermission(Permissions.immunetoban) || e.Player.RealPlayer)) {
                                var user = TShock.Users.GetUserByID(player.Index);
                                var knownIps = JsonConvert.DeserializeObject<List<string>>(user.KnownIps);
                                TShock.Bans.AddBan(knownIps.Last(), user.Name, user.UUID, reason, false, e.Player.User.Name);
                            }
                        TShock.Utils.Broadcast(string.Format("{0} was banned for '{1}'", TeamIDToColor(team), reason.ToLower()), Color.Green);
                    }
                    break;

                case "tempban":
                    if (CommandIsValid(e, Permissions.ban, 3, "tempban <team> <time> [reason]")) {
                        int time;
                        if (!TShock.Utils.TryParseTime(e.Parameters[2], out time)) {
                            e.Player.SendErrorMessage("Invalid time string! Proper format: _d_h_m_s, with at least one time specifier.");
                            e.Player.SendErrorMessage("For example, 1d and 10h-30m+2m are both valid time strings, but 2 is not.");
                            return;
                        }
                        string reason = e.Parameters.Count > 3 ? String.Join(" ", e.Parameters.Skip(3)) : "Misbehavior.";
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && (team == player.Team || team == 5) && (!player.Group.HasPermission(Permissions.immunetoban) || e.Player.RealPlayer))
                                    if (TShock.Bans.AddBan(player.IP, player.Name, player.UUID, reason, false, e.Player.Name, DateTime.UtcNow.AddSeconds(time).ToString("s")))
                                        player.Disconnect(String.Format("Banned: {0}", reason));
                        TShock.Utils.Broadcast(string.Format("{0} was banned for '{1}'", TeamIDToColor(team), reason.ToLower()), Color.Green);
                    }
                    break;

                case "mute":
                    if (CommandIsValid(e, Permissions.mute, 2, "mute <team> [reason]")) {
                        string reason = e.Parameters.Count > 3 ? String.Join(" ", e.Parameters.Skip(3)) : "Misbehavior.";
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && (team == player.Team || team == 5) && !player.Group.HasPermission(Permissions.mute))
                                player.mute = true;
                        TShock.Utils.Broadcast(string.Format("{0} was muted for '{1}'", TeamIDToColor(team), reason.ToLower()), Color.Green);
                    }
                    break;

                case "unmute":
                    if (CommandIsValid(e, Permissions.mute, 2, "mute <team>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && (team == player.Team || team == 5))
                                player.mute = false;
                        TShock.Utils.Broadcast(string.Format("{0} was unmuted by {1}.", TeamIDToColor(team), e.Player.Name), Color.Green);
                    }
                    break;

                case "tempgroup":
                    if (CommandIsValid(e, Permissions.settempgroup, 3, "tempgroup <team> <new group>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        if (!TShock.Groups.GroupExists(e.Parameters[2]))
                            e.Player.SendErrorMessage(string.Format("Could not find group {0}", e.Parameters[2]));
                        else {
                            var g = TShock.Utils.GetGroup(e.Parameters[2]);
                            foreach (TSPlayer player in TShock.Players)
                                if (player != null && (team == player.Team || team == 5)) {
                                    player.tempGroup = g;
                                    player.SendSuccessMessage(string.Format("Your group has temporarily been changed to {0}", g.Name));
                                }
                            e.Player.SendSuccessMessage(string.Format("You have changed {0}'s group to {1}", TeamIDToColor(team, false), g.Name));
                        }
                    }
                    break;

                case "usergroup":
                    if (CommandIsValid(e, Permissions.managegroup, 3, "usergroup <team> <new group>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        if (!TShock.Groups.GroupExists(e.Parameters[2]))
                            e.Player.SendErrorMessage(string.Format("Could not find group {0}", e.Parameters[2]));
                        else {
                            var g = TShock.Utils.GetGroup(e.Parameters[2]);
                            foreach (TSPlayer player in TShock.Players)
                                if (player != null && (team == player.Team || team == 5)) {
                                    player.Group = g;
                                    player.SendSuccessMessage(string.Format("Your group has been changed to {0}", g.Name));
                                }
                            e.Player.SendSuccessMessage(string.Format("You have changed {0}'s group to {1}", TeamIDToColor(team, false), g.Name));
                        }
                    }
                    break;

                case "give":
                case "g":
                    if (CommandIsValid(e, Permissions.item, 3, "give <item type/id> <team> [item amount] [prefix id/name]"))
                        if (e.Parameters[1].Length == 0) e.Player.SendErrorMessage("Missing item name/id.");
                        else if (e.Parameters[2].Length == 0) e.Player.SendErrorMessage("Missing team name.");
                        else {
                            int itemAmount = 0;
                            int prefix = 0;
                            var items = TShock.Utils.GetItemByIdOrName(e.Parameters[1]);
                            string plStr = e.Parameters[2];
                            if (e.Parameters.Count == 4)
                                int.TryParse(e.Parameters[3], out itemAmount);
                            else if (e.Parameters.Count == 5) {
                                int.TryParse(e.Parameters[3], out itemAmount);
                                var prefixIds = TShock.Utils.GetPrefixByIdOrName(e.Parameters[4]);
                                if (items[0].accessory && prefixIds.Contains(42)) {
                                    prefixIds.Remove(42);
                                    prefixIds.Remove(76);
                                    prefixIds.Add(76);
                                }
                                else if (!items[0].accessory && prefixIds.Contains(42))
                                    prefixIds.Remove(76);
                                if (prefixIds.Count == 1)
                                    prefix = prefixIds[0];
                            }

                            if (items.Count == 0)
                                e.Player.SendErrorMessage("Invalid item type!");
                            else if (items.Count > 1)
                                TShock.Utils.SendMultipleMatchError(e.Player, items.Select(i => i.Name));
                            else {
                                var item = items[0];
				                if (item.type >= 1 && item.type < Main.maxItemTypes) {
                                    var team = TeamColorToID(e.Parameters[2], e.Player);
                                    foreach (TSPlayer player in TShock.Players)
                                        if (player != null && (team == player.Team || team == 5) && (player.InventorySlotAvailable || (item.type > 70 && item.type < 75) || item.ammo > 0 || item.type == 58 || item.type == 184)) {
                                            if (itemAmount == 0 || itemAmount > item.maxStack)
                                                itemAmount = item.maxStack;
                                           player.GiveItem(item.type, item.Name, item.width, item.height, itemAmount, prefix);
                                           if (player != e.Player)
                                                player.SendSuccessMessage(string.Format("{0} gave you {1} {2}(s).", e.Player.Name, itemAmount, item.Name));
                                        }
                                    e.Player.SendSuccessMessage(string.Format("Gave {0} {1} {2}(s).", TeamIDToColor(team, false), itemAmount, item.Name));
                                } else e.Player.SendErrorMessage("Invalid item type!");
                            }
                        }
                    break;

                case "tphere":
                    if (CommandIsValid(e, Permissions.tpothers, 2, "tphere <team>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && player.Active && !player.Dead && player.Team == team && player != e.Player) {
                                player.Teleport(e.TPlayer.position.X, e.TPlayer.position.Y);
								player.SendSuccessMessage(String.Format("You were teleported to {0}.", e.Player.Name));
                            }
                        e.Player.SendSuccessMessage("Teleported {0} to yourself.", TeamIDToColor(team, false));
                    }
                    break;

                case "gbuff":
                case "buffplayer":
                    if (CommandIsValid(e, Permissions.buffplayer, 3, "gbuff <team> <buff id/name> [time(seconds)]")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        int id = 0;
                        int time = 60;
                        if (!int.TryParse(e.Parameters[2], out id)) {
                            var found = TShock.Utils.GetBuffByName(e.Parameters[2]);
                                if (found.Count == 0)
                                    e.Player.SendErrorMessage("Invalid buff name!");
                                else if (found.Count > 1)
                                    //TShock.Utils.SendMultipleMatchError(e.Player, found.Select(b => Main.buffName[b]));
                                    TShock.Utils.SendMultipleMatchError(e.Player, found.Select(b => Lang.GetBuffName(b)));
                                else id = found[0];
                        }
                        if (e.Parameters.Count == 4)
                            int.TryParse(e.Parameters[3], out time);
                        if (id > 0 && id < Main.maxBuffTypes) {
                            if (time < 0 || time > short.MaxValue)
                                time = 60;
                            foreach (TSPlayer player in TShock.Players)
                                if (player != null && player.Active && !player.Dead && player.Team == team) {
                                    player.SetBuff(id, time * 60);
                                    if (player != e.Player)
                                        player.SendSuccessMessage(string.Format("{0} has buffed you with {1}({2}) for {3} seconds!",
                                                                            e.Player.Name, TShock.Utils.GetBuffName(id),
                                                                            TShock.Utils.GetBuffDescription(id), (time)));
                                }
                            e.Player.SendSuccessMessage(string.Format("You have buffed {0} with {1}({2}) for {3} seconds!",
                                                                  TeamIDToColor(team, false), TShock.Utils.GetBuffName(id),
                                                                  TShock.Utils.GetBuffDescription(id), (time)));
                        } else e.Player.SendErrorMessage("Invalid buff ID!");
                    }
                    break;

                case "heal":
                    if (CommandIsValid(e, Permissions.heal, 2, "heal <team>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && player.Active && !player.Dead && player.Team == team) {
                                player.Heal();
                                if (player != e.Player)
                                    player.SendSuccessMessage(string.Format("{0} just healed you!", e.Player.Name));
                            }
                        e.Player.SendSuccessMessage(String.Format("You just healed {0}.", TeamIDToColor(team, false)));
                    }
                    break;

                case "kill":
                    if (CommandIsValid(e, Permissions.kill, 2, "kill <team>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && player.Active && !player.Dead && player.Team == team)
                                player.DamagePlayer(999999);
                        TSPlayer.All.SendInfoMessage("{0} killed {1}.", e.Player.Name, TeamIDToColor(team, false));
                    }
                    break;

                case "slap":
                    if (CommandIsValid(e, Permissions.slap, 2, "slap <team> [damage]")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        int damage = 5;
                        if (e.Parameters.Count == 3)
                            int.TryParse(e.Parameters[2], out damage);
                        if (!e.Player.Group.HasPermission(Permissions.kill))
                            damage = TShock.Utils.Clamp(damage, 15, 0);
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && player.Active && !player.Dead && player.Team == team)
                                player.DamagePlayer(damage);
                        TSPlayer.All.SendInfoMessage("{0} slapped {1} for {2} damage.", e.Player.Name, TeamIDToColor(team, false), damage);
                    }
                    break;

                case "playing":
                case "online":
                case "who":
                    if (CommandIsValid(e, "", 2, "who <team> [pagenumber]")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        int pageNumber = 1;
                        if (e.Parameters.Count > 2)
                            int.TryParse(e.Parameters[2], out pageNumber);
                        var plrs = new List<string>();
                        foreach (TSPlayer player in TShock.Players)
                            if (player != null && player.Active && player.Team == team)
                                plrs.Add(player.Name);
                        e.Player.SendSuccessMessage("Online Players ({0}/{1})", plrs.Count, TShock.Config.MaxSlots);
                        PaginationTools.SendPage(e.Player, pageNumber, plrs,
                            new PaginationTools.Settings {
                                IncludeHeader = false,
                                FooterFormat = string.Format("Type /team who {0} {{0}} for more.", TeamIDToColor(team))
                            }
                        );
                    }
                    break;

                case "sendwarp":
                    if (CommandIsValid(e, Permissions.tpothers, 3, "sendwarp <team> <warpname>")) {
                        var team = TeamColorToID(e.Parameters[1], e.Player);
                        var warp = TShock.Warps.Find(e.Parameters[2]);
                        if (warp.Position != Point.Zero) {
                            foreach (TSPlayer player in TShock.Players)
                                if (player != null && player.Active && !player.Dead && player.Team == team)
                                    if (player.Teleport(warp.Position.X * 16, warp.Position.Y * 16) && player != e.Player)
                                        player.SendSuccessMessage(String.Format("{0} warped you to {1}.", e.Player.Name, e.Parameters[2]));
                            e.Player.SendSuccessMessage(String.Format("You warped {0} to {1}.", TeamIDToColor(team, false), e.Parameters[2]));
                        }
                        else e.Player.SendErrorMessage("Specified warp not found.");
                    }
                    break;

                /*
                case "annoy":
                    break;
                 * 
                case "confuse":
                    break;
                 * 
                case "rocket":
                    break;
                 * 
                case "firework":
                    break;

                case "overridessc":
                case "ossc":
                    break;*/

                case "help": {
                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(e.Parameters, 1, e.Player, out pageNumber))
                        return;
                    var commands = new List<string> { "kick", "ban", "tempban", "usergroup", "tempgroup", "give", "tphere", "mute", "unmute", "sendwarp", "gbuff", "heal",
                                                      "kill", "slap", "playing" };
                    PaginationTools.SendPage(e.Player, pageNumber, PaginationTools.BuildLinesFromTerms(commands),
                            new PaginationTools.Settings {
                                HeaderFormat = "Team Commands ({0}/{1}):",
                                FooterFormat = "Type /team help {0} for more commands."
                            }
                        );
                    }
                    break;
            }
        }

        bool CommandIsValid(CommandArgs e, string permission, int argumentCount, string correctSyntax) {
            if (permission != "" && !e.Player.Group.HasPermission(permission)) {
                e.Player.SendErrorMessage("You don't have permission to use that command.");
                return false;
            }
            if (e.Parameters.Count < argumentCount) {
                e.Player.SendErrorMessage(String.Concat("Invalid syntax! Proper syntax : /team ", correctSyntax));
                return false;
            }
            return true;
        }
    }
}