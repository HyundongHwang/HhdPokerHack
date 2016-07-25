using EyeOpen.Imaging.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
using XnaFan.ImageComparison;

namespace HhdPokerHack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += _This_Loaded;
            this.Unloaded += _This_Unloaded;
        }

        private async void _This_Loaded(object sender, RoutedEventArgs e)
        {
            //var dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\Images\SampleScreens");
            //var ssFileList = Directory.GetFiles(dirPath, "*.png");

            //foreach (var ssPath in ssFileList)
            //{
            //    var preStr = await HhdPokerCalcManager.Current.Calc(ssPath, null);
            //}

            HhdFileWatchManager.Current.NewFileCreated += _HhdFileWatchManager_NewFileCreated;
        }

        private void _This_Unloaded(object sender, RoutedEventArgs e)
        {
            HhdFileWatchManager.Current.NewFileCreated -= _HhdFileWatchManager_NewFileCreated;
        }

        private async void _HhdFileWatchManager_NewFileCreated(object sender, string filePath)
        {
            Trace.TraceInformation($"_HhdFileWatchManager_NewFileCreated {filePath}");
            this.ImgObj.Source = new BitmapImage(new Uri(filePath));
            var preStr = await HhdPokerCalcManager.Current.Calc(filePath, null);
            this.TbPre.Text = preStr;
        }

    }
}
