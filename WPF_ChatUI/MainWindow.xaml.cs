
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

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void ButtonMaximaze_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow.WindowState != WindowState.Maximized)
                Application.Current.MainWindow.WindowState = WindowState.Maximized;
            else
                Application.Current.MainWindow.WindowState = WindowState.Normal;

        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private async void BotInitialize_Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            await mainViewModel.BotInitialize();
            BotInitialize_Label.Content = "BOT IS ON";
            BotInitialize_Img.IsHitTestVisible = false;
        }

        private void ButtonSaveMessages_Click(object sender, RoutedEventArgs e)
        {

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;


            using (StreamWriter sw = new StreamWriter(mainViewModel.bot.FullPath + "Contacts.json"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Formatting.Indented;
                serializer.Serialize(writer, mainViewModel.Contacts);

            }

            //openFileDialog.Filter = "Json files (*.json)|*.json";

        }

        private void ButtonUploadMessages_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = mainViewModel.bot.FullPath;
            ofd.Filter = "Json files (*.json)|*.json";

            DialogResult result = ofd.ShowDialog();


            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Task.Run(() =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ObservableCollection<ContactModel> tempContacts = JsonConvert.DeserializeObject<ObservableCollection<ContactModel>>(File.ReadAllText(ofd.FileName));
                        mainViewModel.Contacts.Clear();
                        foreach (var item in tempContacts)
                        {
                            mainViewModel.Contacts.Add(item);
                        }
                    });                
                });
            }

        }
    }
}
