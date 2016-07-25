using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HhdPokerHack
{
    [TestClass]
    public class MyTests
    {

        [TestMethod]
        public async Task extract_pattern()
        {
            var dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\Images\SampleScreens");
            var ssFileList = Directory.GetFiles(dirPath, "*.png");

            foreach (var ssPath in ssFileList)
            {
                var orgBm = new Bitmap(ssPath);
                var uc = new CalcScreenPosition();

                foreach (FrameworkElement child in (uc.Content as Grid).Children)
                {
                    if (!(child is Border))
                        continue;

                    var fragRect = new System.Drawing.Rectangle((int)child.Margin.Left, (int)child.Margin.Top, (int)child.Width, (int)child.Height);
                    var fragBm = orgBm.Clone(fragRect, orgBm.PixelFormat);
                    var fragPath = System.IO.Path.Combine(Environment.CurrentDirectory, $"{child.Name}_{Guid.NewGuid().ToString()}.png");
                    fragBm.Save(fragPath);
                    Trace.TraceInformation($"fragPath : {fragPath}");
                }
            }
        }



        [TestMethod]
        public async Task calc()
        {
            var dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\Images\SampleScreens");
            var ssFileList = Directory.GetFiles(dirPath, "*.png");

            foreach (var ssPath in ssFileList)
            {
                var preStr = await HhdPokerCalcManager.Current.Calc(ssPath, null);
            }
        }




    }
}
