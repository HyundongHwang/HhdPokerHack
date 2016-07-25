using EyeOpen.Imaging.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HhdPokerHack
{
    public class HhdPokerCalcManager
    {
        #region 싱글톤
        static HhdPokerCalcManager _Current;
        static public HhdPokerCalcManager Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new HhdPokerCalcManager();
                }
                return _Current;
            }
        }
        HhdPokerCalcManager()
        {
            var dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, @"Assets\Images\Patterns");
            _smallCatCcList = Directory.GetFiles(dirPath, "small-cat-*.png").Select(path => new ComparableImage(new FileInfo(path))).ToList();
            _smallNumCcList = Directory.GetFiles(dirPath, "small-*-num-*.png").Select(path => new ComparableImage(new FileInfo(path))).ToList();
            _largeCatCcList = Directory.GetFiles(dirPath, "large-cat-*.png").Select(path => new ComparableImage(new FileInfo(path))).ToList();
            _largeNumCcList = Directory.GetFiles(dirPath, "large-*-num-*.png").Select(path => new ComparableImage(new FileInfo(path))).ToList();
        }
        #endregion

        private Dictionary<string, Bitmap> _patternDic = new Dictionary<string, Bitmap>();

        private List<ComparableImage> _smallCatCcList = new List<ComparableImage>();
        private List<ComparableImage> _smallNumCcList = new List<ComparableImage>();
        private List<ComparableImage> _largeCatCcList = new List<ComparableImage>();
        private List<ComparableImage> _largeNumCcList = new List<ComparableImage>();

        public async Task<string> Calc(string ssPath, Dictionary<string, object> allCardDic)
        {
            var now = DateTime.Now;

            var orgBm = new Bitmap(ssPath);
            var uc = new CalcScreenPosition();
            var resultDic = new Dictionary<string, string>();
            var taskList = new List<Task>();

            foreach (FrameworkElement child in (uc.Content as Grid).Children)
            {
                if (!(child is Border))
                    continue;

                var fragRect = new System.Drawing.Rectangle((int)child.Margin.Left, (int)child.Margin.Top, (int)child.Width, (int)child.Height);
                var childName = child.Name;

                var task = Task.Run(() =>
                {
                    _FindPattern(orgBm, resultDic, fragRect, childName);
                });

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
            Trace.TraceInformation($"Task.WaitAll taskList end");



            var handStr = "";
            var playerHandStrList = new List<string>();

            foreach (var player in new[] { "p1", "p2", "me", "p4", "p5" })
            {
                var playerHandList = new List<string>();

                for (int i = 1; i <= 6; i++)
                {
                    if (resultDic.ContainsKey($"bd_{player}_cat_{i}") &&
                        resultDic.ContainsKey($"bd_{player}_num_{i}"))
                    {
                        playerHandList.Add($"{resultDic[$"bd_{player}_num_{i}"]}{resultDic[$"bd_{player}_cat_{i}"]}");
                    }
                }

                var playerHandStr = string.Join("+", playerHandList);
                playerHandStrList.Add(playerHandStr);
            }

            handStr = string.Join("%0D%0A", playerHandStrList);
            Trace.TraceInformation($"handStr : {handStr}");
            Trace.TraceInformation($"image sim time : {DateTime.Now - now}");
            now = DateTime.Now;



            try
            {
                using (var client = new HttpClient())
                {
                    //http://twodimes.net/poker/?g=7s&b=&d=ac&h=9s+ts+js+qs%0D%0A5d+5h+5c+2s
                    var url = $"http://twodimes.net/poker/?g=7s&b=&h={handStr}";

                    var resStr = await client.GetStringAsync(url);
                    var startIdx = resStr.IndexOf("7-card");
                    var endIdx = resStr.IndexOf("</pre>");
                    var preStr = resStr.Substring(startIdx, endIdx - startIdx);

                    Trace.TraceInformation($"poker calc time : {DateTime.Now - now}");
                    now = DateTime.Now;

//7 - card Stud Hi: 500000 sampled outcomes
//cards win   % win    lose % lose  tie % tie     EV
//4c 2c  2d           98405  19.68  401581  80.32   14  0.00  0.197
//Qc Jd  9d           65107  13.02  434834  86.97   59  0.01  0.130
//Ac Jh  9h  8h  3h  167200  33.44  332685  66.54  115  0.02  0.335
//9s 8s  2h           42703   8.54  457130  91.43  167  0.03  0.086
//Ts Td  8d          126376  25.28  373561  74.71   63  0.01  0.253

                    return preStr;
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private object _orgBm_lock = new object();

        private void _FindPattern(Bitmap orgBm, Dictionary<string, string> resultDic, Rectangle fragRect, string childName)
        {
            Trace.TraceInformation($"_FindPattern {childName} start");
            Bitmap fragBm = null;

            lock (_orgBm_lock)
            {
                fragBm = orgBm.Clone(fragRect, orgBm.PixelFormat);
            }
            
            var fragPath = System.IO.Path.GetTempFileName();
            fragBm.Save(fragPath);

            var fragCc = new ComparableImage(new FileInfo(fragPath));
            var maxSim = 0.0;
            var maxSimPath = "";
            List<ComparableImage> patternCcList = null;

            if (childName.Contains("_p") && childName.Contains("cat"))
            {
                patternCcList = _smallCatCcList;
            }
            else if (childName.Contains("_p") && childName.Contains("num"))
            {
                patternCcList = _smallNumCcList;
            }
            else if (childName.Contains("_me_") && childName.Contains("cat"))
            {
                patternCcList = _largeCatCcList;
            }
            else if (childName.Contains("_me_") && childName.Contains("num"))
            {
                patternCcList = _largeNumCcList;
            }

            foreach (var cc in patternCcList)
            {
                var sim = fragCc.CalculateSimilarity(cc);

                if (sim > maxSim)
                {
                    maxSim = sim;
                    maxSimPath = cc.File.Name;
                }
            }

            if (maxSim > 0.9)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(maxSimPath);
                resultDic.Add(childName, name.Last().ToString());
            }

            Trace.TraceInformation($"_FindPattern {childName} end");
        }
    }
}
