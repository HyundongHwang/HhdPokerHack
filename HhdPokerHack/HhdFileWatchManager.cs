using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HhdPokerHack
{
    public class HhdFileWatchManager
    {

        #region 싱글톤
        static HhdFileWatchManager _Current;
        static public HhdFileWatchManager Current
        {
            get
            {
                if (_Current == null)
                {
                    _Current = new HhdFileWatchManager();
                }
                return _Current;
            }
        }
        HhdFileWatchManager()
        {
            _RunLoop();
        }
        #endregion

        private Dictionary<string, object> _CurrentFilesDic = new Dictionary<string, object>();

        public event EventHandler<string> NewFileCreated;

        private const string _FTP_SERVER = "ftp://192.168.0.4:2221";

        private async Task _RunLoop()
        {
            while (true)
            {
                var listReq = WebRequest.Create(_FTP_SERVER);
                listReq.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                var listRes = (FtpWebResponse)(await listReq.GetResponseAsync());

                using (var resStream = listRes.GetResponseStream())
                using (var sr = new StreamReader(resStream))
                {
                    var resStr = await sr.ReadToEndAsync();
                    var resLines = resStr.Split("\n\r".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                    var isFirstRequest = _CurrentFilesDic.Count == 0;

                    foreach (var resLine in resLines)
                    {
                        var resTokens = resLine.Split(" \t\n\r".ToArray(), StringSplitOptions.RemoveEmptyEntries);

                        if (resTokens.Length < 9)
                            continue;



                        var fileName = resTokens[8];

                        if (isFirstRequest)
                        {
                            _CurrentFilesDic[fileName] = null;
                            continue;
                        }



                        if (!_CurrentFilesDic.ContainsKey(fileName))
                        {
                            var fileReq = WebRequest.Create($"{_FTP_SERVER}/{fileName}");
                            fileReq.Method = WebRequestMethods.Ftp.DownloadFile;
                            var fileRes = (FtpWebResponse)(await fileReq.GetResponseAsync());
                            var localPath = System.IO.Path.Combine(Environment.CurrentDirectory, fileName);

                            using (var fileResStream = fileRes.GetResponseStream())
                            using (var fs = new FileStream(localPath, FileMode.CreateNew))
                            {
                                await fileResStream.CopyToAsync(fs);
                            }

                            if (this.NewFileCreated != null)
                            {
                                this.NewFileCreated.Invoke(this, localPath);
                            }

                            _CurrentFilesDic[fileName] = null;
                        }
                    }

                    await Task.Delay(1000);
                }



                
            }
        }

    }
}
