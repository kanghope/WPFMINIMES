using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MiniMes.Client.Views
{
    /// <summary>
    /// ItemSelectView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ItemSelectView : Window
    {
        public ItemSelectView()
        {
            InitializeComponent();
        }
        /// <summary>
        /// XAML의 '닫기' 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // DialogResult를 false로 설정하면 호출한 곳(ShowDialog)에서 취소로 인식합니다.
            this.DialogResult = false;
            this.Close();
        }
    }
}
