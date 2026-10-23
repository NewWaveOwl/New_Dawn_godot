using NewDawn.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
namespace NewDawn.Views
{
    /// <summary>
    /// Interaction logic for SavesWindow.xaml
    /// </summary>
    /// 
    public partial class SavesWindow : Window
    {
        private List<SaveItem> _items;
        private Game _loadedGame; //for everyone access new loaded game

        //properties

        public Game LoadedGame
        {
            get { return _loadedGame; }
            set { _loadedGame = value; }
        }
        public List<SaveItem> SaveWindowItems
        {
            get { return _items; }
            set { _items = value; }
        }

        //initializtion/constructor
        public SavesWindow()
        {
            InitializeComponent();
            SaveWindowItems = new List<SaveItem>();
            LoadSavesFromFolder();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        //struct
        public struct SaveItem
        {
            private string _displayName { get; set; }
            private string _fileName { get; set; }
            private string _fullPath { get; set; }
            private DateTime _modified { get; set; }

            public string DisplayName
            {
                get { return _displayName; }
                set { _displayName = value; }
            }
            public string FileName
            {
                get { return _fileName; }
                set { _fileName = value; }
            }
            public string FullPath
            {
                get { return _fullPath; }
                set { _fullPath = value; }
            }
            public DateTime Modified
            {
                get { return _modified; }
                set { _modified = value; }
            }
        }
        //buttons
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        public void LstSaves_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LoadSelected();
        }
        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadSelected();
        }

        //methods
        private void LoadSavesFromFolder()
        {
            SaveWindowItems.Clear();

            string dir = Path.Combine(Environment.CurrentDirectory, "Saves", "GameSaves");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string[] files = Directory.GetFiles(dir, "*" + SaveAndLoaderHandler.gameSaveExtention, SearchOption.TopDirectoryOnly);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo fi = new FileInfo(files[i]);
                SaveItem item = new SaveItem();
                item.FullPath = fi.FullName;
                item.FileName = fi.Name;
                item.DisplayName = Path.GetFileNameWithoutExtension(fi.Name);
                item.Modified = fi.LastWriteTime;
                SaveWindowItems.Add(item);
            }
            LstSaves.ItemsSource = null;
            LstSaves.ItemsSource = SaveWindowItems;
        }
        public void LoadSelected()
        {
            if (LstSaves.SelectedItem == null)
            {
                MessageBox.Show("Select a save first.", "Load", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveItem selected = (SaveItem)LstSaves.SelectedItem;

            try
            {
                string nameWithout = Path.GetFileNameWithoutExtension(selected.FileName);
                LoadedGame = SaveAndLoaderHandler.GameLoad(nameWithout);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load save.\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}