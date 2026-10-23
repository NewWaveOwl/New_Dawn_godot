using NewDawn.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewDawn.Models;

namespace NewDawn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnNewGame_Clicked(object sender, RoutedEventArgs e)
        {
            NewGameSetup newGameSetup = new NewGameSetup();
            this.Close();
            newGameSetup.Show();
        }
       private void BtnMapEditor_Clicked(object sender, RoutedEventArgs e)
       {
            MapEditorWindow mapEditorWindow = new MapEditorWindow();
            this.Close();
            mapEditorWindow.Show();
       }

        private void BtnUnitEditor_Clicked(object sender, RoutedEventArgs e)
        {
            UnitEditorWindow unitEditorWindow = new UnitEditorWindow();
            this.Close();
            unitEditorWindow.Show();
        }

        private void BtnExit_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();

        }

        //Full Credit to Chatpgt for generating this to use
        private void BtnHowToPlay_Clicked(object sender, RoutedEventArgs e)
        {
            string howToPlay =
        "═══════════════════════════════════\n" +
        "        NEW DAWN - HOW TO PLAY\n" +
        "═══════════════════════════════════\n\n" +

        "🎯 OBJECTIVE:\n" +
        "Destroy the enemy Commander (CMP) to win!\n" +
        "Protect your own CMP at all costs.\n\n" +

        "⚔️ GAMEPLAY:\n" +
        "• Deploy units from the shop (costs Alloy)\n" +
        "• Move units across the hex grid (costs Fuel)\n" +
        "• Attack enemies within range\n" +
        "• Use special abilities (cooldown)\n" +
        "• End turn when ready\n\n" +

        "💰 RESOURCES:\n" +
        "• Alloy: Used to purchase units\n" +
        "• Fuel: Refills automatically each turn\n" +
        "• Build extractors on resource tiles for income\n\n" +

        "🛡️ TERRAIN EFFECTS:\n" +
        "• Field/Forest: Bonus defense for infantry\n" +
        "• Mountain/Water: Blocks movement (armored only)\n" +
        "• Swamp: Movement penalty, damage boost\n" +
        "• City: Provides resources to adjacent tiles\n\n" +

        "⚡ TIPS:\n" +
        "• Armored units can cross water/mountains\n" +
        "• Protect your CMP - if it dies, you lose!\n" +
        "• Use terrain to your advantage\n" +
        "• Balance offense and economy\n\n" +

        "Good luck, Commander! 🚀";

            MessageBox.Show(howToPlay, "How to Play", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            //SavesWindow savesWindow = new SavesWindow();
            //MainGameWindow mainGameWindow = new MainGameWindow(newGame); 
            //this.Close();
            //mainGameWindow.Show();
            //savesWindow.Show();
        }
    }
}