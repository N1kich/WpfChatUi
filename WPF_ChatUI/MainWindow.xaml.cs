
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPF_ChatUI.MVVM.Model;
using WPF_ChatUI.MVVM.VIewModel;
using System.IO;
using Application = System.Windows.Application;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.ObjectModel;

namespace WPF_ChatUI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
            mainViewModel = new MainViewModel(this);
            this.DataContext = mainViewModel;

        }

        /// <summary>
        /// move chat window by clicking on window border
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// method to minimize chat window by clicking on button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// method to maximaze chat window by clicking on button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMaximaze_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            else
                Application.Current.MainWindow.WindowState = WindowState.Normal;

        }

        /// <summary>
        /// method to kill chat window by clicking on button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        /// <summary>
        /// method to initialize bot 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BotInitialize_Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await mainViewModel.BotInitialize();
            BotInitialize_Label.Content = "BOT IS ON";
            BotInitialize_Img.IsHitTestVisible = false;
        }


        /// <summary>
        /// Save chat history into json file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonSaveMessages_Click(object sender, RoutedEventArgs e)
        {
            if ((string)BotInitialize_Label.Content == "BOT IS ON")
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Json files (*.json)|*.json";
                saveFileDialog.Title = "Save a ChatHistory File";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {
                    using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Formatting = Formatting.Indented;
                        serializer.Serialize(writer, mainViewModel.Contacts);

                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Please, enter a fileName to save chat history in it", "Caption", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
            }
            else
            {
                System.Windows.MessageBox.Show("Please, initialize Bot by clicking on Bot's image", "Caption", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Upload chat history
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonUploadMessages_Click(object sender, RoutedEventArgs e)
        {
            if ((string)BotInitialize_Label.Content == "BOT IS ON")
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.InitialDirectory = mainViewModel.bot.FullPath;
                ofd.Title = "Upload Chat History FileDialog";
                ofd.Filter = "Json files (*.json)|*.json";

                DialogResult result = ofd.ShowDialog();


                if (result == System.Windows.Forms.DialogResult.OK)
                {

                    ObservableCollection<ContactModel> tempContacts = JsonConvert.DeserializeObject<ObservableCollection<ContactModel>>(File.ReadAllText(ofd.FileName));
                    mainViewModel.Contacts.Clear();
                    foreach (var item in tempContacts)
                    {
                        mainViewModel.Contacts.Add(item);
                    }

                }
            } 
            else
            {
                System.Windows.MessageBox.Show("Please, initialize Bot by clicking on Bot's image", "Caption", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            

        }
    }
}
