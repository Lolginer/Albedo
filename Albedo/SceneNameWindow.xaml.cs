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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Albedo.Core;

namespace Albedo
{
	/// <summary>
	/// Interaction logic for SceneNameWindow.xaml
	/// </summary>
	public partial class SceneNameWindow : Window
	{
		public SceneNameWindow(string startName = "")
		{
			InitializeComponent();
			for (int i = 0; i < 10; i++) {
				Tile tileRef = (Tile)this.FindName(String.Format("Tile{0}", i));
				dynamic tileColor = Storage.tileColors(i);
				tileRef.Background = new BrushConverter().ConvertFromString(tileColor.background);
				tileRef.BorderBrush = new BrushConverter().ConvertFromString(tileColor.border);
			}
			this.NameText.Text = startName;
			this.NameText.Focus();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Tile_Click(object sender, RoutedEventArgs e)
		{
			if (this.NameText.Text.IndexOfAny(new char[] { '{', '}', '[', ']', '"', '\\' }) != -1) {
				System.Windows.MessageBox.Show("Forbidden characters: { } [ ] \" \\", "Name Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			} else if (this.NameText.Text.Length == 0) {
				System.Windows.MessageBox.Show("Please enter a scene name.", "Name Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			} else {
				Tile tileRef = (Tile)sender;
				EditorWindow.sceneName = this.NameText.Text;
				EditorWindow.sceneColor = Convert.ToInt32(tileRef.Name.Substring(4, tileRef.Name.Length - 4));
				this.Close();
			}
		}
	}
}
