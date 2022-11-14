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
            this.mainWindow = mainWindow;

            bot = new TelegramBot();

            Contacts = new ObservableCollection<ContactModel>();
            SelectedContact = new ContactModel();

            SendCommand = new RelayCommand( async o =>
            {
                //checking empty message from messageBox
                if (Message == "")
                {
                    MessageBox.Show("Your message is empty!", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var selectedContact = SelectedContact;
                    bool isFirstMessage = (selectedContact.Messages.Last().Username == bot.botModel.Username) ? false : true;

                    selectedContact.Messages.Add(new MessageModel
                    {
                        Username = bot.botModel.Username,
                        UserNameColor = "Black",
                        Time = DateTime.Now,
                        ImageSource = bot.botModel.ImageSource,
                        Message = Message,
                        IsFirstMessage = isFirstMessage
                    });

                    //get the last message in messageCollection: Didnt work!!!
                    selectedContact.GetLastMsg();
                                        
                    if (SelectedContact.Username != bot.botModel.Username)
                    {
                        await bot.telegramBotUser.SendTextMessageAsync(selectedContact.ChatId, Message);
                    }
                    Message = "";
                }
                                
            });

            //set up dictionaries: emojis and markUpKeyboard
            emojis = SetEmojis();
            keyboards = SetBotKeyboardButtons();
        }


        public async Task BotInitialize()
        {                       

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { } // receive all update types
            };

            //launch bot
            bot.telegramBotUser.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions);
            
            //get response from bot
            var botUser = await bot.telegramBotUser.GetMeAsync();
            
            //get string path to botAvatar
            string imageBotAvatar = await GetBotProfilePhoto(botUser, bot.telegramBotUser);

            //fill the bot info
            bot.botModel.Username = botUser.Username;
            bot.botModel.UserId = botUser.Id.ToString();
            bot.botModel.ImageSource = imageBotAvatar;
            bot.GetImageSource();

            //add first message
            bot.botModel.Messages.Add(new MessageModel
            {
                Username = botUser.Username,
                UserNameColor = "Black",
                ImageSource = bot.botModel.ImageSource,
                IsFirstMessage = true,
                Message = "Welcome to the SavedMessages",
            });

            
            //add information to form
            mainWindow.Dispatcher.Invoke(() =>
            {
                bot.botModel.GetLastMsg();
                Contacts.Add(bot.botModel);
            });
            
            Console.WriteLine($"Start listening for @{botUser.Username}");
        }
        

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            //index of contact
            int indexOfIncomingUser = default(int);
            //if update type is message, then work with message data
            if (update.Type == UpdateType.Message)
            {                
                // if message is a text, handle it
                if (update.Message.Type == MessageType.Text)
                {
                    //check the new user
                    if (isUserNew(update.Message.From.Id.ToString()))
                    {
                        await CreateNewContact(update, botClient, bot.FullPath);
                        indexOfIncomingUser = Contacts.Count - 1;
                    }
                    else //get the index of existing user
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
                    indexOfIncomingUser = GetIndexOfContactUser(update.Message.From.Id.ToString());
                    //if message type != text, try to donwload it
                    await FileDownloaderHandlerAsync(botClient, update, cancellationToken, Contacts[indexOfIncomingUser].UserPath, indexOfIncomingUser);
                }

            }
            //if update type is CallbackQuery 
            if (update.Type == UpdateType.CallbackQuery)
            {
                //define CallBackData 
                string fileToUpload = update.CallbackQuery.Data;
                Console.WriteLine(fileToUpload);

                indexOfIncomingUser = GetIndexOfContactUser(update.CallbackQuery.From.Id.ToString());

                //upload files based on CallBack Data
                await UploadChoosenFileAsync(fileToUpload, Contacts[indexOfIncomingUser].UserPath, (TelegramBotClient)botClient, update, indexOfIncomingUser);
            }
        }


        /// <summary>
        /// Error Handler
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception.Message;

            var obj = exception.InnerException;

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        

        
        /// <summary>
        /// Task to handle text messages and text commands
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="indexOfIncomingUser"></param>
        /// <returns></returns>
        async Task TextOptionsAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken,int indexOfIncomingUser)
        {
     
            var messageText = update.Message.Text;
            bool isUserSentNew = false;

            //checking if the messages from the user sent in a row or its the first message after our bot response
            if (Contacts[indexOfIncomingUser].Messages.Last().Username == bot.botModel.Username)
            {
                isUserSentNew = true;
            }

            MessageModel messageModel = new MessageModel()
            {
                Message = messageText,
                ImageSource = Contacts[indexOfIncomingUser].ImageSource,
                IsFirstMessage = isUserSentNew,
                Time = DateTime.Now,
                Username = update.Message.From.Username,
                UserNameColor = "Black",
            };

            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts[indexOfIncomingUser].Messages.Add(messageModel);
            });

            // handle text form message
            switch (messageText)
            {
                //case to send greetigs to user
                case "/start":
                    {
                        await DescriprionStartAsync(botClient, update, cancellationToken, keyboards["startButtons"], indexOfIncomingUser);
                        bot.isMenuEnabled = false;
                        break;
                    }
                //case to send descriptions of functions which bot can process
                case "/menu":
                    {
                        await MenuDescriptionAsync(botClient, update, cancellationToken, keyboards["menuButtons"], indexOfIncomingUser);
                        bot.isMenuEnabled = true;
                        break;
                    }
                //case to send info about
                case "/info":
                    {
                        mainWindow.Dispatcher.Invoke(() =>
                        {
                            Contacts[indexOfIncomingUser].Messages.Add(new MessageModel()
                            {
                                ImageSource = Contacts[indexOfIncomingUser].ImageSource,
                                IsFirstMessage= true,
                                Message = "test file-trading bot",
                                Time = DateTime.Now,
                                UserNameColor= "Black",
                                Username = bot.botModel.Username,
                            });
                        });
                        await botClient.SendTextMessageAsync(update.Message.Chat.Id, "test file-trading bot", cancellationToken: cancellationToken);
                        bot.isMenuEnabled = false;
                        break;
                    }
                //case to send list of files, this command could be invoke in /menu from keyboard button
                case "I want to know, which files are already in the bot's storage":
                    {
                        await SendListOfFilesAsync(Contacts[indexOfIncomingUser].UserPath, (TelegramBotClient)botClient, update, cancellationToken,indexOfIncomingUser);
                        break;
                    }
                default:
                   break;
            }
        }
        /// <summary>
        /// sends greetings to new user, runs when bot get /start command
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="keyboard"></param>
        /// <param name="emojis"></param>
        /// <returns></returns>
        async Task DescriprionStartAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, ReplyKeyboardMarkup keyboard, int indexOfIncomingUser)
        {
            string startDesription = "\tWelcome to the training bot!" + emojis[Emoji.CheckMark] + "\nDescription of Bot Commands below" + emojis[Emoji.Pen] +
                "\n/start - say hi to bot and view this message" + emojis[Emoji.Pen] +
                "\n/menu - show command keyboard" + emojis[Emoji.Pen] + "\n/info - show info about this bot" + emojis[Emoji.Pen];

            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts[indexOfIncomingUser].Messages.Add(new MessageModel
                {
                    ImageSource = bot.botModel.ImageSource,
                    Message = startDesription,
                    IsFirstMessage = true,
                    Time = DateTime.Now,
                    Username = bot.botModel.Username,
                    UserNameColor = "Black"
                });
            });

            await botClient.SendTextMessageAsync(update.Message.Chat.Id, startDesription, replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Task to create a new ContactModel
        /// </summary>
        /// <param name="update"></param>
        /// <param name="botClient"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        async Task CreateNewContact(Update update, ITelegramBotClient botClient,string path)
        {
            //crete a new user's folder
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
                Messages = new ObservableCollection<MessageModel> { messageModel },
                UserId = update.Message.From.Id.ToString(),
                UserPath = newUserPath,
                ImageSource = messageModel.ImageSource,
                ChatId = update.Message.Chat.Id,
            };

            
            mainWindow.Dispatcher.Invoke(() =>
            {
                contact.GetLastMsg();
                Contacts.Add(contact);
            });
            
        }

        /// <summary>
        /// Task to download the user's avatar picture and set it to the GUI of chat app
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="userPath"></param>
        /// <returns></returns>
        async Task<String> GetPhotoProfile(ITelegramBotClient botClient, Update update, string userPath)
        {
            
            UserProfilePhotos userProfilePhotos = await botClient.GetUserProfilePhotosAsync(update.Message.From.Id, 0, 1);
            PhotoSize[] photoSize;
            //user profile prhotos is array of the different proportions of height and weight. Choose the smallest one
            if (userProfilePhotos.TotalCount !=0)
            {
                photoSize = userProfilePhotos.Photos[0];
                var fileID = photoSize[1].FileId;
                Console.WriteLine(fileID);

                await DownloadAsync(fileID, userPath + update.Message.Chat.Username + "ProfilePic" + ".jpg", (TelegramBotClient)botClient);
                return userPath + update.Message.Chat.Username + "ProfilePic" + ".jpg";
            }
            else
            {
                return "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e0/Anonymous.svg/433px-Anonymous.svg.png";
            }
            
            
           
        }

        /// <summary>
        /// Task to download bot avatar, and return string path to picture
        /// </summary>
        /// <param name="user"></param>
        /// <param name="botClient"></param>
        /// <returns></returns>
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
        async Task MenuDescriptionAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, ReplyKeyboardMarkup keyboard, int indexOfUser)
        {
            string descriptionStr = $"\tOn this page you can choose following functions{emojis[Emoji.BicycleMan]}\n" +
                $"You can store the following file types:\n{emojis[Emoji.Photo]} Photos!\n" +
                    $"{emojis[Emoji.Document]} Documents!\n" +
                    $"{emojis[Emoji.Audio]} Audio\n " +
                    $"{emojis[Emoji.Video]} Video and VideoNotes\n" +
                    $"{emojis[Emoji.VoiceMessage]} VoiceMessage\n" +
                    $"{emojis[Emoji.BicycleMan]} Get the names of files from bot's storage{emojis[Emoji.BicycleMan]}\n" +
                $"{emojis[Emoji.BicycleMan]}Download the file from storage. To begin this operations check the existing files {emojis[Emoji.BicycleMan]}";

          

            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts[indexOfUser].Messages.Add(new MessageModel
                {
                    ImageSource = bot.botModel.ImageSource,
                    IsFirstMessage = true,
                    Time = DateTime.Now,
                    Username = bot.botModel.Username,
                    UserNameColor = "Black",
                    Message = descriptionStr,
                });
            });
            await botClient.SendTextMessageAsync(update.Message.Chat.Id, descriptionStr, replyMarkup: keyboard, cancellationToken: cancellationToken);
            
        }

        /// <summary>
        /// Task to download a object from Telegram
        /// </summary>
        /// <param name="FileID"></param>
        /// <param name="Path"></param>
        /// <param name="bot"></param>
        /// <returns></returns>
        static async Task DownloadAsync(string FileID, string Path, TelegramBotClient bot)
        {
            var fileInfo = await bot.GetFileAsync(FileID);

            FileStream fs = new FileStream(Path, FileMode.Create);
            await bot.DownloadFileAsync(fileInfo.FilePath, fs);
            fs.Close();
        }

        /// <summary>
        /// set the dictionary of emojis
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Task that send list of file in user's folder
        /// </summary>
        /// <param name="path"></param>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="indexOfUser"></param>
        /// <returns></returns>
        async Task SendListOfFilesAsync(string path, TelegramBotClient botClient, Update update, CancellationToken cancellationToken, int indexOfUser)
        {
            //get files
            Dictionary<string, FileType> filesInPath = GetFileExtension(path);

            if (filesInPath.Count == 0)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"I dont have any files in my storage {emojis[Emoji.CoolMan]}", cancellationToken: cancellationToken);
            }
            else
            {
                // build string with all files
                string listOfFiles = $"List of Files below{emojis[Emoji.HmmEmoji]}:\n";
                foreach (var files in filesInPath)
                {

                    listOfFiles += GetTheStrWithEmoji(files.Key, files.Value);

                }
                mainWindow.Dispatcher.Invoke(() =>
                {
                    Contacts[indexOfUser].Messages.Add(new MessageModel
                    {
                        ImageSource = bot.botModel.ImageSource,
                        IsFirstMessage = true,
                        Time = DateTime.Now,
                        Username = bot.botModel.Username,
                        UserNameColor = "Black",
                        Message = listOfFiles,
                    });
                });
                InlineKeyboardMarkup inlineKeyboard = SetInlineKeyboards(filesInPath);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, listOfFiles, replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);

            }


            }

        /// <summary>
        /// Define fileExtension get all files in special folder and define extension
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        Dictionary<string, FileType> GetFileExtension(string Path)
        {
            
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
        async Task UploadChoosenFileAsync(string fileName, string Path, TelegramBotClient botClient, Update update, int indexOfIncomingUser)
        {
            string fileExtension = fileName.Substring(fileName.LastIndexOf('.'));
            FileType typeOfCallbackFile = GetFileType(fileExtension);
            mainWindow.Dispatcher.Invoke(() =>
            {
                Contacts[indexOfIncomingUser].Messages.Add(new MessageModel()
                {
                    ImageSource = Contacts[indexOfIncomingUser].ImageSource,
                    IsFirstMessage = true,
                    Time = DateTime.Now,
                    Username = Contacts[indexOfIncomingUser].Username,
                    Message = $"I want to get {fileName} from bot's storage",
                    UserNameColor = "Black"
                });

                Contacts[indexOfIncomingUser].Messages.Add(new MessageModel()
                {
                    ImageSource = bot.botModel.ImageSource,
                    IsFirstMessage = true,
                    Message = "Bot is sending you the choosen file",
                    Time = DateTime.Now,
                    Username = bot.botModel.Username,
                    UserNameColor = "Black"
                });
            });

            using (FileStream stream = System.IO.File.OpenRead(Path + fileName))
            {
                InputOnlineFile inputOnlineFile = new InputOnlineFile(stream, fileName);
                switch (typeOfCallbackFile)
                {
                    case FileType.Document:
                        await botClient.SendDocumentAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    case FileType.Audio:
                        await botClient.SendAudioAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    case FileType.Video:
                        await botClient.SendVideoAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile, supportsStreaming: true);
                        break;
                    case FileType.Photo:
                        await botClient.SendPhotoAsync(update.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        break;
                    default:
                        break;
                }
            }

        }
        /// <summary>
        /// method to initialize keyboards
        /// </summary>
        /// <returns></returns>
        Dictionary<string, ReplyKeyboardMarkup> SetBotKeyboardButtons()
        {
            Dictionary<string, ReplyKeyboardMarkup> keyboards = new Dictionary<string, ReplyKeyboardMarkup>()
            {
                { "startButtons", new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "/start" }, new KeyboardButton[] { "/menu" }, new KeyboardButton[] { "/info" } }) { ResizeKeyboard = true } },
                { "menuButtons", new ReplyKeyboardMarkup(new[] { new KeyboardButton[] { "I want to know, which files are already in the bot's storage" } }) { ResizeKeyboard = true } },
            };


            return keyboards;
        }

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

        /// <summary>
        /// async Task to download specific files, define type of message, then define type of files get information about it then download file
        /// bool value uses in order to load files only after the /menu message
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        async Task FileDownloaderHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken, string Path, int indexOfIncomingUser)
        {
            //all supported message types
            if (update.Message.Type == MessageType.Photo || update.Message.Type == MessageType.Video || update.Message.Type == MessageType.VideoNote || update.Message.Type == MessageType.Voice || update.Message.Type == MessageType.Document || update.Message.Type == MessageType.Audio)
            {
                // if user don't click /menu button yet cancel download
                if (!bot.isMenuEnabled)
                {                
                    mainWindow.Dispatcher.Invoke(() =>
                    {
                        Contacts[indexOfIncomingUser].Messages.Add(new MessageModel
                        {
                            ImageSource = Contacts[indexOfIncomingUser].ImageSource,
                            Message = $"i've sent a {update.Message.Type}",
                            IsFirstMessage = true,
                            Time = DateTime.Now,
                            Username = update.Message.From.Username,
                            UserNameColor = "Black"

                        });

                        Contacts[indexOfIncomingUser].Messages.Add(new MessageModel
                        {
                            ImageSource = bot.botModel.ImageSource,
                            Message = $"Please choose the /menu to send and store your files in bot's files storage {emojis[Emoji.CoolMan]}",
                            IsFirstMessage = true,
                            Time = DateTime.Now,
                            Username = bot.botModel.Username,
                            UserNameColor = "Black"

                        });
                    });

                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Please choose the /menu to send and store your files in bot's files storage {emojis[Emoji.CoolMan]}");
                    return;
                }
                switch (update.Message.Type)
                {
                    case MessageType.Photo:
                        {
                            string fileID = update.Message.Photo[1].FileId;
                            //in case when we cant get the file name from telegram, we use 3 uniqe numbers from file ID
                            string UniqeID = fileID.Substring(fileID.Length / 2, 3);
                            await DownloadAsync(fileID, Path + update.Message.Chat.Username + UniqeID + ".jpg", (TelegramBotClient)botClient);

                            break;
                        }
                    case MessageType.Document:
                        {
                            var fileID = update.Message.Document.FileId;
                            await DownloadAsync(fileID, Path + update.Message.Document.FileName, (TelegramBotClient)botClient);
                            break;
                        }
                    case MessageType.Video:
                        {
                            var fileID = update.Message.Video.FileId;
                            var fileName = update.Message.Video.FileName;
                            await DownloadAsync(fileID, Path + fileName, (TelegramBotClient)botClient);
                            break;
                        }
                    case MessageType.Audio:
                        {
                            var fileID = update.Message.Audio.FileId;
                            var fileName = update.Message.Audio.FileName;
                            await DownloadAsync(fileID, Path + fileName, (TelegramBotClient)botClient);
                            break;
                        }
                    case MessageType.Voice:
                        {
                            var fileID = update.Message.Voice.FileId;
                            string UniqeID = fileID.Substring(fileID.Length - 5);
                            await DownloadAsync(fileID, Path + update.Message.Chat.Username + UniqeID + ".mp3", (TelegramBotClient)botClient);

                            break;
                        }
                    case MessageType.VideoNote:
                        {
                            var fileID = update.Message.VideoNote.FileId;
                            string UniqeID = fileID.Substring(fileID.Length - 5);
                            await DownloadAsync(fileID, Path + update.Message.Chat.Username + UniqeID + ".mp4", (TelegramBotClient)botClient);

                            break;
                        }
                }
                Console.WriteLine($"Download the file from  {update.Message.Chat.Username} {update.Message.Chat.Id} - {update.Message.Type}");

                mainWindow.Dispatcher.Invoke(() =>
                {
                    Contacts[indexOfIncomingUser].Messages.Add(new MessageModel()
                    {
                        ImageSource= Contacts[indexOfIncomingUser].ImageSource,
                        IsFirstMessage= true,
                        Time = DateTime.Now,
                        Username = update.Message.From.Username,
                        UserNameColor = "Black",
                        Message = $"i've sent {update.Message.Type} emojis[Emoji.Pen]"
                    });

                    Contacts[indexOfIncomingUser].Messages.Add(new MessageModel
                    {
                        ImageSource = bot.botModel.ImageSource,
                        Message = $"I download the {update.Message.Type}" + emojis[Emoji.Pen],
                        IsFirstMessage = true,
                        Time = DateTime.Now,
                        Username = bot.botModel.Username,
                        UserNameColor = "Black"

                    });
                });
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"I download the {update.Message.Type}" + emojis[Emoji.Pen]);
            }
            else
            {
                Console.WriteLine($"Unknown type of files");

                mainWindow.Dispatcher.Invoke(() =>
                {
                    Contacts[indexOfIncomingUser].Messages.Add(new MessageModel
                    {
                        ImageSource = bot.botModel.ImageSource,
                        Message = $"Unknown type of files {emojis[Emoji.HmmEmoji]}\n You can store the following file types:\n{emojis[Emoji.Photo]} Photos!\n" +
                            $"{emojis[Emoji.Document]} Documents!\n" +
                            $"{emojis[Emoji.Audio]} Audio\n " +
                            $"{emojis[Emoji.Video]} Video and VideoNotes\n" +
                            $"{emojis[Emoji.VoiceMessage]} VoiceMessage\n",
                        IsFirstMessage = true,
                        Time = DateTime.Now,
                        Username = bot.botModel.Username,
                        UserNameColor = "Black"
                    });
                });

                await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Unknown type of files {emojis[Emoji.HmmEmoji]}\n You can store the following file types:\n{emojis[Emoji.Photo]} Photos!\n" +
                    $"{emojis[Emoji.Document]} Documents!\n" +
                    $"{emojis[Emoji.Audio]} Audio\n " +
                    $"{emojis[Emoji.Video]} Video and VideoNotes\n" +
                    $"{emojis[Emoji.VoiceMessage]} VoiceMessage\n", cancellationToken: cancellationToken);
            }


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
