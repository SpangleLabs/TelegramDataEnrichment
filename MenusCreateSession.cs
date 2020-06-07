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