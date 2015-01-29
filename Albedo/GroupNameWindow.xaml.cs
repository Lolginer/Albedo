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

namespace Albedo
{
	/// <summary>
	/// Interaction logic for GroupNameWindow.xaml
	/// </summary>
	public partial class GroupNameWindow : Window
	{
		public GroupNameWindow(string startName = "")
		{
			InitializeComponent();
			this.NameText.Text = startName;
			this.NameText.Focus();
		}

		private void OK_Click(object sender, RoutedEventArgs e)
		{
			if (this.NameText.Text.IndexOfAny(new char[] { '{', '}', '[', ']', '"', '\\' }) != -1) {
				System.Windows.MessageBox.Show("Forbidden characters: { } [ ] \" \\", "Name Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			} else if (this.NameText.Text.Length == 0) {
				System.Windows.MessageBox.Show("Please enter a group name.", "Name Error", MessageBoxButton.OK, MessageBoxImage.Warning);
			} else {
				EditorWindow.groupName = this.NameText.Text;
				this.Close();
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
