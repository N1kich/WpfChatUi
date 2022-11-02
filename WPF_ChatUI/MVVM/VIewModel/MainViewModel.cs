using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using WPF_ChatUI.Core;
using WPF_ChatUI.MVVM.Model;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Windows;
using System.Windows.Threading;
using Telegram.Bot.Types.InputFiles;

namespace WPF_ChatUI.MVVM.VIewModel
{
    class MainViewModel: ObservableObject
    {
        MainWindow mainWindow;
        
        #region globalVariables for Telegram bot
        //collection for messages
        public ObservableCollection<MessageModel> Messages { get; set; }

        //collection of users
        public ObservableCollection<ContactModel> Contacts { get; set; }

        public TelegramBot bot;

        //dictionary for emoji's code
        Dictionary<Emoji, string> emojis;

        Dictionary<string, ReplyKeyboardMarkup> keyboards;

        
        #endregion

        //command to add messages on GUI
        public RelayCommand SendCommand { get; set; }

        private ContactModel _selectedContact;

        public ContactModel SelectedContact
        {
            get { return _selectedContact; }
            set { _selectedContact = value; OnPropertyChanged(); }
        }


        private string _message;

        public string Message { get { return _message; } set { _message = value; OnPropertyChanged(); } }



        public MainViewModel(MainWindow mainWindow)
        {
            //get a link on current mainWindow object
            this.mainWindow =mainWindow;

            bot = new TelegramBot();

            
            Contacts = new ObservableCollection<ContactModel>();
            SelectedContact = new ContactModel();
            SendCommand = new RelayCommand( o =>
            {

                var selectedContact = SelectedContact;
                selectedContact.Messages.Add(new MessageModel
                {
                    Username = bot.botModel.Username + " @" + bot.botModel.UserId.ToString(),
                    UserNameColor = "Black",
                    Time = DateTime.Now,
                    ImageSource = bot.botModel.ImageSource,
                    Message = Message,
                    IsFirstMessage = true
                }) ;
                Message = "";
                Console.WriteLine(selectedContact.Username.ToString());
            });

            //set up dictionaries once. emojis and markUpKeyboard
            emojis = SetEmojis();
            //keyboards = SetBotKeyboardButtons();

        }


        public async Task BotInitialize()
        {                       

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            bot.telegramBotUser.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions);
            
            //get response from bot
            var botUser = await bot.telegramBotUser.GetMeAsync();

            //get string path to botAvatar
            string imageBotAvatar = await GetBotProfilePhoto(botUser, bot.telegramBotUser);

            bot.botModel.Username = botUser.Username + botUser.Id;
            bot.botModel.ImageSource = imageBotAvatar;
            bot.GetImageSource();
            Console.WriteLine(botUser.Username.ToString());
            Console.WriteLine(botUser.Id.ToString());
            Console.WriteLine(imageBotAvatar.ToString());

            bot.botModel.Messages.Add(new MessageModel
            {
                Username = botUser.Username + botUser.Id.ToString(),
                UserNameColor = "Black",
                ImageSource = bot.botModel.ImageSource,
                IsFirstMessage = true,
                Message = "Welcome to the SavedMessages",
            });

            
            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts.Add(bot.botModel);
            });
            
            Console.WriteLine($"Start listening for @{botUser.Username}");
            
          
        }
        

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            
            //if update type is message, then work with message data
            if (update.Type == UpdateType.Message)
            {
                int indexOfIncomingUser = default(int);
                // if message is a text, handle it
                if (update.Message.Type == MessageType.Text)
                {
                    if (isUserNew(update.Message.From.Id.ToString()))
                    {
                        await CreateNewContact(update, botClient, bot.FullPath);
                        indexOfIncomingUser = Contacts.Count - 1;
                    }
                    else
                    {
                        indexOfIncomingUser = GetIndexOfContactUser(update.Message.From.Id.ToString());
                    }
                    //just cosmetic output to console
                    var chatId = update.Message.Chat.Id;
                    var messageText = update.Message.Text;
                    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}. By {update.Message.From.Username}");

                    
                    //define if text message is a command
                   await TextOptionsAsync(botClient, update, cancellationToken,indexOfIncomingUser);
                }
                else
                {
                    //if message type != text, try to donwload it
                   // await FileDownloaderHandlerAsync(botClient, update, cancellationToken, relativePath, emojis, IsMenuButtonSelected);
                }

            }
            //if update type is CallbackQuery 
            if (update.Type == UpdateType.CallbackQuery)
            {
                //define CallBackData 
                string fileToUpload = update.CallbackQuery.Data;
                Console.WriteLine(fileToUpload);

                //upload files based on CallBack Data
                //await UploadChoosenFileAsync(fileToUpload, relativePath, (TelegramBotClient)botClient, update);
            }
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception.Message;

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        //Task to handle text command
        async Task TextOptionsAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken,int indexOfIncomingUser)
        {
            

            // rework this shit 
            var messageText = update.Message.Text;
            MessageModel messageModel = new MessageModel()
            {
                Message = messageText,
                ImageSource = Contacts[indexOfIncomingUser].ImageSource,
                IsFirstMessage = true,
                Time = DateTime.Now,
                Username = update.Message.From.Username,
                UserNameColor = "Black",
            };

            //mainWindow.Dispatcher.Invoke(() =>
            //{
            //    Contacts[indexOfIncomingUser].Messages.Add(messageModel);
            //});
            
            // handle text form message
            switch (messageText)
            {
                //case to send greetigs to user
                case "/start":
                    {
                        //await DescriprionStartAsync(botClient, update, cancellationToken, keyboards["startButtons"], emojis);
                        bot.isMenuEnabled = false;
                        break;
                    }
                //case to send descriptions of functions which bot can process
                case "/menu":
                    {
                        await MenuDescriptionAsync(botClient, update, cancellationToken, keyboards["menuButtons"], emojis, bot.FullPath);
                        bot.isMenuEnabled = true;
                        break;
                    }
                //case to send info about
                case "/info":
                    {
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "test file-trading bot", cancellationToken: cancellationToken);
                        bot.isMenuEnabled = false;
                        break;
                    }
                //case to send list of files, this command could be invoke in /menu from keyboard button
                case "I want to know, which files are already in the bot's storage":
                    {
                        await SendListOfFilesAsync(bot.FullPath, (TelegramBotClient)botClient, update, cancellationToken,indexOfIncomingUser);
                        break;
                    }
                default:
                    Message defaultMessage = await botClient.SendTextMessageAsync(update.Message.Chat.Id, "I dont understand your commands" + emojis[Emoji.HmmEmoji] + ".\n Use keyboard buttons to navigate! But its okay! Lets chat. Feel free to write me anything" +
                        emojis[Emoji.CoolMan]);
                    defaultMessage = await botClient.SendStickerAsync(update.Message.Chat.Id, sticker: "CAACAgIAAxkBAAEDy3Vh-w1UvK764n4JWmM-v2e9rndxLQAC6BUAAiMlyUtQqGgG1fAXAAEjBA");
                    break;
            }
        }


        async Task CreateNewContact(Update update, ITelegramBotClient botClient,string path)
        {

            string newUserPath = path + @"\" + update.Message.From.Username + @"\";
            Directory.CreateDirectory(newUserPath);

            MessageModel messageModel = new MessageModel()
            {
                UserNameColor = "Black",
                Username = update.Message.From.Username,
                Message = "Hi, i'm new here! Greetings!",
                Time = DateTime.Now,
                IsFirstMessage = true,
                ImageSource = await GetPhotoProfile(botClient, update, newUserPath),
            };
            ContactModel contact = new ContactModel()
            {
               Username = update.Message.From.Username,
               Messages = new ObservableCollection<MessageModel> {messageModel },
               UserId = update.Message.From.Id.ToString(),
               UserPath = newUserPath,
               ImageSource = messageModel.ImageSource,
            };
            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts.Add(contact);
            });
            
        }
        async Task<String> GetPhotoProfile(ITelegramBotClient botClient, Update update, string userPath)
        {
            UserProfilePhotos userProfilePhotos = await botClient.GetUserProfilePhotosAsync(update.Message.From.Id, 0, 1);

            PhotoSize[] photoSize = userProfilePhotos.Photos[0];
            var fileID = photoSize[0].FileId;
            Console.WriteLine(fileID);
           
            await DownloadAsync(fileID, userPath + update.Message.Chat.Username + "ProfilePic" + ".jpg", (TelegramBotClient)botClient);
            return userPath + update.Message.Chat.Username + "ProfilePic" + ".jpg";
        }

        async Task<String> GetBotProfilePhoto(Telegram.Bot.Types.User user, ITelegramBotClient botClient)
        {
            UserProfilePhotos userProfilePhotos = await botClient.GetUserProfilePhotosAsync(user.Id, 0, 1);

            PhotoSize[] photoSize = userProfilePhotos.Photos[0];
            var fileID = photoSize[0].FileId;
            

            await DownloadAsync(fileID, bot.FullPath + user.Username + "ProfilePic" + ".jpg", (TelegramBotClient)botClient);
            return bot.FullPath + user.Username + "ProfilePic" + ".jpg";
        }

        /// <summary>
        /// description abot functions which bot can process, runs when user send /menu
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="keyboard"></param>
        /// <param name="emojis"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        static async Task MenuDescriptionAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, ReplyKeyboardMarkup keyboard, Dictionary<Emoji, string> emojis, string Path)
        {
            string descriptionStr = $"\tOn this page you can choose following functions{emojis[Emoji.BicycleMan]}\n" +
                $"You can store the following file types:\n{emojis[Emoji.Photo]} Photos!\n" +
                    $"{emojis[Emoji.Document]} Documents!\n" +
                    $"{emojis[Emoji.Audio]} Audio\n " +
                    $"{emojis[Emoji.Video]} Video and VideoNotes\n" +
                    $"{emojis[Emoji.VoiceMessage]} VoiceMessage\n" +
                    $"{emojis[Emoji.BicycleMan]} Get the names of files from bot's storage{emojis[Emoji.BicycleMan]}\n" +
                $"{emojis[Emoji.BicycleMan]}Download the file from storage. To begin this operations check the existing files {emojis[Emoji.BicycleMan]}";

            Message MenuDescr = await botClient.SendTextMessageAsync(update.Message.Chat.Id, descriptionStr, replyMarkup: keyboard, cancellationToken: cancellationToken);

        }

        static async Task DownloadAsync(string FileID, string Path, TelegramBotClient bot)
        {
            var fileInfo = await bot.GetFileAsync(FileID);

            FileStream fs = new FileStream(Path, FileMode.Create);
            await bot.DownloadFileAsync(fileInfo.FilePath, fs);
            fs.Close();
        }

        Dictionary<Emoji, string> SetEmojis()
        {
            Dictionary<Emoji, string> emojis = new Dictionary<Emoji, string>()
            {
                {Emoji.Pen, char.ConvertFromUtf32(0x270F) },
                {Emoji.CheckMark,char.ConvertFromUtf32(0x2705)  },
                {Emoji.BicycleMan,char.ConvertFromUtf32(0x267F)  },
                {Emoji.HmmEmoji, char.ConvertFromUtf32(0x1F914)  },
                {Emoji.CoolMan,  char.ConvertFromUtf32(0x1F60E) },
                {Emoji.VoiceMessage, char.ConvertFromUtf32(0x1F3A4) },
                {Emoji.Video, char.ConvertFromUtf32(0x1F3A6)},
                {Emoji.Audio, char.ConvertFromUtf32(0x1F3B5) },
                {Emoji.Document, char.ConvertFromUtf32(0x1F4C3) },
                {Emoji.Photo, char.ConvertFromUtf32(0x1F4F7) }
            };

            return emojis;
        }

        async Task SendListOfFilesAsync(string path, TelegramBotClient bot, Update update, CancellationToken cancellationToken, int indexOfUSer)
        {
            //get files
            Dictionary<string, FileType> filesInPath = GetFileExtension(path);

            if (filesInPath.Count == 0)
            {
                _ = await bot.SendTextMessageAsync(update.Message.Chat.Id, $"I dont have any files in my storage {emojis[Emoji.CoolMan]}", cancellationToken: cancellationToken);
            }
            else
            {
                // build string with all files
                string listOfFiles = $"List of Files below{emojis[Emoji.HmmEmoji]}:\n";
                foreach (var files in filesInPath)
                {

                    listOfFiles += GetTheStrWithEmoji(files.Key, files.Value);

                }
                InlineKeyboardMarkup inlineKeyboard = SetInlineKeyboards(filesInPath);
                _ = await bot.SendTextMessageAsync(update.Message.Chat.Id, listOfFiles, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);

            }


        }

        /// <summary>
        /// Define fileExtension
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        Dictionary<string, FileType> GetFileExtension(string Path)
        {
            // get all files in special folder and define extension
            string[] fileNames = Directory.GetFiles(Path);

            Dictionary<string, FileType> files = new Dictionary<string, FileType>();

            if (fileNames != default(string[]))
            {
                for (int i = 0; i < fileNames.Length; i++)
                {
                    string fileExtension = fileNames[i].Substring(fileNames[i].LastIndexOf('.'));
                    files.Add(fileNames[i].Substring(fileNames[i].LastIndexOf(@"\") + 1), GetFileType(fileExtension));
                }

            }
            return files;
        }

        /// <summary>
        /// return enum type of file extension
        /// </summary>
        /// <param name="Extension"></param>
        /// <returns></returns>
        static FileType GetFileType(string Extension)
        {
            switch (Extension)
            {
                case ".jpg":
                    return FileType.Photo;
                case ".mp3":
                    return FileType.Audio;
                case ".mp4":
                    return FileType.Video;
                default:
                    return FileType.Document;
            }
        }

        /// <summary>
        /// creates string with file type emoji and file name
        /// </summary>
        /// <param name="KeyFromDictionary"></param>
        /// <param name="ValueFromDictionary"></param>
        /// <param name="emojis"></param>
        /// <returns></returns>
        string GetTheStrWithEmoji(string KeyFromDictionary, FileType ValueFromDictionary)
        {
            string strWithEmoji = "";

            switch (ValueFromDictionary)
            {
                case FileType.Document:
                    {
                        strWithEmoji += emojis[Emoji.Document] + KeyFromDictionary + "\n";
                        break;
                    }
                case FileType.Audio:
                    {
                        strWithEmoji += emojis[Emoji.Audio] + KeyFromDictionary + "\n";
                        break;
                    }
                case FileType.Photo:
                    {
                        strWithEmoji += emojis[Emoji.Photo] + KeyFromDictionary + "\n";
                        break;
                    }
                case FileType.Video:
                    {
                        strWithEmoji += emojis[Emoji.Video] + KeyFromDictionary + "\n";
                        break;
                    }
            }

            return strWithEmoji;
        }

        /// <summary>
        /// Task to upload choosen file 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="Path"></param>
        /// <param name="bot"></param>
        /// <param name="update"></param>
        /// <returns></returns>
         async Task UploadChoosenFileAsync(string fileName, string Path, TelegramBotClient bot, Update update)
        {
            string fileExtension = fileName.Substring(fileName.LastIndexOf('.'));
            FileType typeOfCallbackFile = GetFileType(fileExtension);
            using (FileStream stream = System.IO.File.OpenRead(Path + fileName))
            {
                InputOnlineFile inputOnlineFile = new InputOnlineFile(stream, fileName);
                switch (typeOfCallbackFile)
                {
                    case FileType.Document:
                        await bot.SendDocumentAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    case FileType.Audio:
                        await bot.SendAudioAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    case FileType.Video:
                        await bot.SendVideoAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile, supportsStreaming: true);
                        break;
                    case FileType.Photo:
                        await bot.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    default:
                        break;
                }
            }

        }
        ///// <summary>
        ///// method to initialize keyboards
        ///// </summary>
        ///// <returns></returns>
        //Dictionary<string, ReplyKeyboardMarkup> SetBotKeyboardButtons()
        //{
        //    Dictionary<string, ReplyKeyboardMarkup> keyboards = new Dictionary<string, ReplyKeyboardMarkup>();
        //    {
        //        { {  "startButtons"}, new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "/start" }, new KeyboardButton[] { "/menu" }, new KeyboardButton[] { "/info" } }) { ResizeKeyboard = true } };
        //        { "menuButtons", new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "I want to know, which files are already in the bot's storage" } }) { ResizeKeyboard = true } };
        //    };


        //    return keyboards;

        //}

        InlineKeyboardMarkup SetInlineKeyboards(Dictionary<string, FileType> listOfFiles)
        {
            //creates list of inline buttons. Each list contains triplet of buttons. Adding list to list of inlinekeyboards array
            int sizeOfDictionary = listOfFiles.Count;
            InlineKeyboardMarkup inlineMarkup;

            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            List<InlineKeyboardButton[]> arrayOfButtons = new List<InlineKeyboardButton[]>();

            int k = 1;
            int i = 0;
            foreach (var item in listOfFiles)
            {
                // algorithm to collect files in triplets, if amount_of_files % 3 != 0 add 1 or 2 files in last inline string
                //create string with specific emoji and name of file based on file's extension
                string StringWithEmoji = GetTheStrWithEmoji(item.Key, item.Value);
                if (sizeOfDictionary - 1 == i && k != 3)
                {
                    switch (k)
                    {
                        case 1:
                            {
                                arrayOfButtons.Add(new[] { new InlineKeyboardButton(StringWithEmoji) { CallbackData = item.Key } });
                                break;
                            }
                        case 2:
                            {
                                arrayOfButtons.Add(new[] { buttons[0], new InlineKeyboardButton(StringWithEmoji) { CallbackData = item.Key } });
                                break;
                            }
                        default:
                            break;
                    }
                }
                else
                {
                    if (k <= 3)
                    {
                        buttons.Add(new InlineKeyboardButton(StringWithEmoji)
                        {
                            CallbackData = item.Key
                        });
                        if (k == 3)
                        {
                            arrayOfButtons.Add(new[] { buttons[0], buttons[1], buttons[2] });
                            k = 0;
                            buttons.Clear();
                        }
                    }

                    k++;
                    i++;
                }

            }
            //return inline keyboard object
            inlineMarkup = new InlineKeyboardMarkup(arrayOfButtons.ToArray());
            return inlineMarkup;
        }

        bool isUserNew(string id)
        {
            foreach (var contact in Contacts)
            {
                if (contact.UserId == id)
                {
                    return false;
                }
            }

            return true;
        }

        int GetIndexOfContactUser(string id )
        {
            ContactModel tempContact = new ContactModel();
            foreach (var contact in Contacts)
            {
                if (id == contact.UserId)
                {
                    tempContact = contact;
                }
            }

            return Contacts.IndexOf(tempContact);
        }
    }
}
