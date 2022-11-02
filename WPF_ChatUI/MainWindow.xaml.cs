using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPF_ChatUI.MVVM.Model;
using WPF_ChatUI.MVVM.VIewModel;

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
            //Contacts_lView.ItemsSource = mainViewModel.Contacts;
            ////Contacts_lView.SelectedItem = mainViewModel.SelectedContact;
            //MessageBox_lView.ItemsSource = mainViewModel.SelectedContact.Messages;

            //BotAvatar_ImgB.ImageSource = mainViewModel.bot.ImageSourceAvatar;

            //BotUserName_Label.DataContext = mainViewModel.Contacts;
            //BotUserID_Label.DataContext = mainViewModel.Contacts;
            //MessageWindow_TextBox.DataContext = mainViewModel.Message;
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
            if (Application.Current.MainWindow.WindowState != WindowState.Maximized )
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
            BotInitialize_Img.IsHitTestVisible = false;
        }

        private void Contacts_lView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mainViewModel.SelectedContact = (ContactModel)Contacts_lView.SelectedItem;
        }

        
    }
}
