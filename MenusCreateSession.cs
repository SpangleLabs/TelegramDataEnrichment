using System;
using System.Collections.Generic;
using DreadBot;
using TelegramDataEnrichment.Sessions;

namespace TelegramDataEnrichment
{
    internal class CreateSessionMenu : Menu
    {
        public const string CallbackName = "session_create";

        protected override string Text()
        {
            return "Creating a new session.\nWhat would you like to name the session?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    internal class CreateSessionBatchSizeMenu : Menu
    {
        public const string CallbackName = "session_c_batch";

        protected override string Text()
        {
            return "How many data points should it post at once?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("1", $"{CallbackName}:1", 0);
            keyboard.addCallbackButton("3", $"{CallbackName}:3", 0);
            keyboard.addCallbackButton("5", $"{CallbackName}:5", 0);
            keyboard.addCallbackButton("10", $"{CallbackName}:10", 0);
            return keyboard;
        }
    }

    public class CreateSessionDataSourceTypeMenu : Menu
    {
        public const string CallbackName = "session_c_source_type";
        public const string CallbackDirectorySource = "session_c_source_type:directory";

        protected override string Text()
        {
            return "Select the type of data source to use for this enrichment session";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Files in directory", CallbackDirectorySource, 0);
            return keyboard;
        }
    }

    public class CreateSessionDataSourceDirectoryName : Menu
    {
        private readonly List<string> _directories;
        public const string CallbackName = "session_c_source_d_name";

        public CreateSessionDataSourceDirectoryName(List<string> directories)
        {
            _directories = directories;
        }

        protected override string Text()
        {
            return _directories.Count == 0
                ? "There are no valid input directories."
                : "Please select a directory to read data from:";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            if (_directories.Count == 0)
            {
                keyboard.addCallbackButton("Go back to menu 🔙", RootMenu.CallbackName, 0);
                return keyboard;
            }

            var row = 0;
            foreach (var directory in _directories)
            {
                keyboard.addCallbackButton(directory, $"{CallbackName}:{row}", row++);
            }

            return keyboard;
        }
    }

    internal class CreateSessionRandomOrder : Menu
    {
        public const string CallbackName = "session_s_rand_order";

        protected override string Text()
        {
            return "Should this session present data in a random order, or in the order of the data source?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Original order", $"{CallbackName}:{false}", 0);
            keyboard.addCallbackButton("Random order", $"{CallbackName}:{true}", 1);
            return keyboard;
        }
    }

    internal class CreateSessionOutputTypeMenu : Menu
    {
        private readonly List<DataOutput.DataOutputTypes> _validOutputTypes;
        public const string CallbackSubDirectory = "session_s_output:subdir";

        public CreateSessionOutputTypeMenu(List<DataOutput.DataOutputTypes> validOutputTypes)
        {
            _validOutputTypes = validOutputTypes;
        }

        protected override string Text()
        {
            return _validOutputTypes.Count == 0 
                ? "There are no data output types available for this data source type." 
                : "How should this enrichment session output the results.";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            if (_validOutputTypes.Count == 0)
            {
                keyboard.addCallbackButton("Go back to menu 🔙", RootMenu.CallbackName, 0);
                return keyboard;
            }

            foreach (var outputType in _validOutputTypes)
            {
                switch (outputType)
                {
                    case DataOutput.DataOutputTypes.SubDirectory:
                        keyboard.addCallbackButton("Move to subdirectories", CallbackSubDirectory, 0);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return keyboard;
        }
    }

    internal class CreateSessionOptions : Menu
    {
        protected override string Text()
        {
            return "What options are available? Please enter a comma-separated list";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    internal class CreateSessionOptionsExpandable : Menu
    {
        public const string CallbackName = "session_c_options_expand";

        protected override string Text()
        {
            return "Are you allowed to add new options throughout the session, or are they hard-coded?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Allow adding new options", $"{CallbackName}:{true}", 0);
            keyboard.addCallbackButton("Just use hard-coded options", $"{CallbackName}:{false}", 1);
            return keyboard;
        }
    }

    internal class CreateSessionOptionsMulti : Menu
    {
        public const string CallbackName = "session_c_options_multi";

        protected override string Text()
        {
            return "Are you allowed to select multiple options?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Allow multiple options", $"{CallbackName}:{false}", 0);
            keyboard.addCallbackButton("One option only", $"{CallbackName}:{true}", 1);
            return keyboard;
        }
    }

    internal class SessionCreatedMenu : Menu
    {
        private readonly EnrichmentSession _newSession;

        public SessionCreatedMenu(EnrichmentSession newSession)
        {
            _newSession = newSession;
        }

        protected override string Text()
        {
            return $"New session has been created: {_newSession.Name}";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }
}