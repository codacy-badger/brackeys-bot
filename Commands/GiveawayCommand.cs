﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using System.Text;

namespace BrackeysBot.Commands
{
    public class GiveawayCommand : ModuleBase
    {
        private readonly SettingsTable _settings;

        private const string GIVEAWAY_EMOTE_IDENTIFIER = "brackeys-emote";
        private const string GIVEAWAY_MESSAGE_IDENTIFIER = "giveaway-messageid";

        private static List<ulong> _blacklistedUsers = new List<ulong>();

        public GiveawayCommand (SettingsTable settings)
        {
            _settings = settings;
        }

        [Command("initgiveaway")]
        [HelpData("initgiveaway <message>", "Initializes a new giveaway, with a specified message.", AllowedRoles = UserType.Staff)]
        public async Task InitializeGiveaway ([Remainder] string message)
        {
            // Delete the invokation message
            await Context.Message.DeleteAsync();

            // Reply with the specified message
            var botMessage = await ReplyAsync(message);

            // Find the giveaway emote and add it as a reaction
            var emote = _settings.Get(GIVEAWAY_EMOTE_IDENTIFIER);
            await botMessage.AddReactionAsync(new Emoji(emote));

            // Save the giveaway message id
            string identifier = $"{botMessage.Channel.Id}/{botMessage.Id.ToString()}";
            if (!_settings.Has(GIVEAWAY_MESSAGE_IDENTIFIER))
                _settings.Add(GIVEAWAY_MESSAGE_IDENTIFIER, identifier);
            else
                _settings.Set(GIVEAWAY_MESSAGE_IDENTIFIER, identifier);
        }

        [Command("draw")]
        [HelpData("draw <usercount> <includestaff>", "Performs a giveaway with a set number of winners.", AllowedRoles = UserType.Staff)]
        public async Task PerformGiveaway (int userCount, bool includeStaff = true)
        {
            if (!TryParseChannelMessageID(out ulong channelId, out ulong messageId))
                throw new Exception("Invalid giveaway message ID.");

            // Get the randomized users
            var randomizedUsers = await GetRandomizedGiveawayUsers(channelId, messageId, includeStaff);

            if (randomizedUsers.Count() < 1)
            {
                await ReplyAsync("Apparently, no valid user has entered the giveaway yet ...");
                return;
            }

            // Select the users, capped by usercount
            IUser[] selectedUsers = randomizedUsers.Count() > userCount ? randomizedUsers.Take(userCount).ToArray() : randomizedUsers.ToArray();

            // Build the message
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("The drawn users of the giveaways are ...");

            foreach (IUser user in selectedUsers)
            {
                sb.AppendLine($"• { user.Mention }");
                _blacklistedUsers.Add(user.Id);
            }

            sb.AppendLine();
            sb.AppendLine("Congratulations to the winners!");

            await ReplyAsync(sb.ToString());
        }

        [Command("draw")]
        [HelpData("draw", "Draws a single user out of the giveaway pool. Also blacklists the user so he can't be drawn again.", AllowedRoles = UserType.Staff)]
        public async Task Draw (bool includeStaff = true)
        {
            if (!TryParseChannelMessageID(out ulong channelId, out ulong messageId))
                throw new Exception("Invalid giveaway message ID.");

            // Get the randomized users
            var randomizedUsers = await GetRandomizedGiveawayUsers(channelId, messageId, includeStaff);

            if (randomizedUsers.Count() < 1)
            {
                await ReplyAsync("Apparently, no valid user has entered the giveaway yet ...");
                return;
            }

            // Get the first one (random)
            var drawnUser = randomizedUsers.First();
            _blacklistedUsers.Add(drawnUser.Id);

            string message = $"{ drawnUser.Mention } was drawn!";
            await ReplyAsync(message);
        }

        [Command("cleardraw")]
        [HelpData("cleardraw", "Clears the draw blacklist.", AllowedRoles = UserType.Staff)]
        public async Task ClearDraw()
        {
            int count = _blacklistedUsers.Count;

            _blacklistedUsers.Clear();

            string message = $"{ count } users have been cleared from the blacklist.";
            await ReplyAsync(message);
        }

        private async Task<IEnumerable<IGuildUser>> GetRandomizedGiveawayUsers(ulong channelId, ulong messageId, bool includeStaff)
        {
            // Get the message by its id
            var giveawayMessage = await (await Context.Guild.GetChannelAsync(channelId) as IMessageChannel).GetMessageAsync(messageId) as IUserMessage;

            // Get all users that reacted to the message with the giveaway emote
            var emote = _settings.Get(GIVEAWAY_EMOTE_IDENTIFIER);
            var giveawayUsers = await giveawayMessage.GetReactionUsersAsync(emote, 1000);

            // Project the users into IGuildUsers
            var guildUsers = await Context.Guild.GetUsersAsync(CacheMode.AllowDownload);
            var projectedGuildUsers = giveawayUsers.Select(u => guildUsers.FirstOrDefault(g => g.Id == u.Id)).Where(u => u != default(IGuildUser));

            // Filter out bots, blacklisted users and conditionally staff
            var users = projectedGuildUsers.Where(u => !(u.IsBot || _blacklistedUsers.Contains(u.Id)));
            if (!includeStaff) users = users.Where(u => !(u as IGuildUser)?.HasStaffRole() ?? false);

            // Randomize the users
            Random rng = new Random();
            var randomizedUsers = users.OrderBy(u => rng.Next());

            return randomizedUsers;
        }

        private bool TryParseChannelMessageID(out ulong channelId, out ulong messageId)
        {
            channelId = 0;
            messageId = 0;

            if (!_settings.Has(GIVEAWAY_MESSAGE_IDENTIFIER))
                return false;

            string[] parts = _settings.Get(GIVEAWAY_MESSAGE_IDENTIFIER).Split('/');
            try
            {
                channelId = ulong.Parse(parts[0]);
                messageId = ulong.Parse(parts[1]);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
