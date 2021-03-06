﻿using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Discord;
using Discord.Commands;

namespace BrackeysBot.Commands
{
    public class HelpCommand : ModuleBase
    {
        private readonly CommandService _commands;
        private readonly CustomizedCommandTable _customCommands;

        public HelpCommand(CommandService commands, CustomizedCommandTable customCommands)
        {
            _commands = commands;
            _customCommands = customCommands;
        }

        [Command ("help")]
        [HelpData("help", "Displays this menu.")]
        public async Task Help ()
        {
            EmbedBuilder commandDialog = GetCustomCommandDialog();
            EmbedBuilder helpDialog = GetHelpDialog(UserType.Everyone);
            try
            {
                if (commandDialog != null)
                    await Context.User.SendMessageAsync(string.Empty, false, commandDialog);

                await Context.User.SendMessageAsync(string.Empty, false, helpDialog);

                await ReplyAsync("Help has been sent to your DMs! :white_check_mark:");
            }
            catch
            {
                if (commandDialog != null)
                    await ReplyAsync(string.Empty, false, commandDialog);

                var msg2 = await ReplyAsync(string.Empty, false, helpDialog);
            }
        }

        [Command("modhelp")]
        [HelpData("modhelp", "Displays this menu.", AllowedRoles = UserType.Staff)]
        public async Task ModHelp ()
        {
            EmbedBuilder helpDialog = GetHelpDialog(UserType.Staff);
            try
            {
                await Context.User.SendMessageAsync(string.Empty, false, helpDialog);
            }
            catch
            {
                await ReplyAsync(string.Empty, false, helpDialog);
            }
        }

        [Command("customcommands"), Alias("cclist")]
        [HelpData("customcommands", "Displays all the registered custom commands.")]
        public async Task CustomCommands()
        {
            EmbedBuilder commandDialog = GetCustomCommandDialog();
            if (commandDialog == null)
            {
                commandDialog = new EmbedBuilder()
                    .WithColor(new Color(165, 79, 121))
                    .WithTitle("Custom Commands")
                    .WithDescription("No custom commands registered!");
            }

            await ReplyAsync(string.Empty, false, commandDialog);
        }
        
        /// <summary>
        /// Returns the dialog displaying the custom commands.
        /// </summary>
        private EmbedBuilder GetCustomCommandDialog()
        {
            if (_customCommands.CommandNames.Length == 0)
                return null;

            EmbedBuilder eb = new EmbedBuilder()
                .WithColor(new Color(165, 79, 121))
                .WithTitle("Custom Commands");

            StringBuilder commands = new StringBuilder();
            foreach (string command in _customCommands.CommandNames)
            {
                commands.AppendLine($"{command}");
            }
            eb.WithDescription(commands.ToString());

            return eb;
        }

        /// <summary>
        /// Returns the help dialog for a specific mode.
        /// </summary>
        private EmbedBuilder GetHelpDialog(UserType userType)
        {
            EmbedBuilder eb = new EmbedBuilder()
                .WithColor(new Color(247, 22, 131))
                .WithTitle("BrackeysBot")
                .WithDescription("The official Brackeys Discord bot! Commands are:");

            string prefix = BrackeysBot.Configuration["prefix"];

            var commands = GetCommandDataCollection(userType);
            foreach (var command in commands)
            {
                string title = prefix + command.Usage;
                string content = command.Description;
                eb.AddField(title, content);
            }

            return eb;
        }

        /// <summary>
        /// Gathers the data for all commands found in the command modules.
        /// </summary>
        private IEnumerable<HelpDataAttribute> GetCommandDataCollection (UserType userType)
        {
            var commandList = new List<HelpDataAttribute>();

            foreach (ModuleInfo module in _commands.Modules)
                foreach (CommandInfo command in module.Commands)
                {
                    if (command.Attributes.FirstOrDefault(a => a is HelpDataAttribute) is HelpDataAttribute data)
                    {
                        if (userType == data.AllowedRoles)
                        {
                            commandList.Add(data as HelpDataAttribute);
                        }
                    }
                }

            var ordered = commandList.OrderBy(data => data.ListOrder);
            return ordered;
        }
    }
}
