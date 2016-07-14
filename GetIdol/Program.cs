/* Copyright © Macsim Belous 2012 */
/* This file is part of Erza.

    Foobar is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Foobar is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Foobar.  If not, see <http://www.gnu.org/licenses/>.*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Threading;
using System.Globalization;
using System.Data.SQLite;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace GetIdol
{
    class Program
    {
        static CookieCollection sankaku_cookies = null;
        static string RawCookies = null;
        static int StartPage = 1;
        static int MaxPage = -1;
        static List<string> Tags = null;
        static GetidolConfig config = null;
        static void Main(string[] args)
        {
            LoadSettings();
            if (args.Length <= 0) 
            { 
                Console.WriteLine("Не заданы параметры!");
                return;
            }
            ParseArgs(args);
            if (Tags.Count <= 0)
            {
                Console.WriteLine("Не заданы теги!");
                return;
            }
            StringBuilder tags = new StringBuilder();
            for(int i=0;i<Tags.Count;i++)
            {
                if (i == 0)
                {
                    tags.Append(WebUtility.UrlEncode(Tags[i]));
                }
                else
                {
                    tags.Append("+");
                    tags.Append(WebUtility.UrlEncode(Tags[i]));
                }
            }
            ServicePointManager.ServerCertificateValidationCallback = ValidationCallback;
            Console.WriteLine("Импортируем тег " + tags.ToString() + " с санкаки");
            List<int> post_ids = GetImageInfoFromSankaku(tags.ToString());
            Console.Write("\n\n\n\t\tНАЧИНАЕТСЯ ЗАГРУЗКА\n\n\n");
            int count_complit = 0;
            int count_deleted = 0;
            int count_error = 0;
            int count_skip = 0;
            Directory.CreateDirectory(".\\" + tags.ToString());
            for (int i = 0; i < post_ids.Count; i++)
            {
                Console.WriteLine("\n###### {0}/{1} ######", (i + 1), post_ids.Count);
                for (int index = 0; index < Program.config.LimitErrors; index++)
                {
                    //DateTime start = DateTime.Now;
                    if (DownloadImageFromSankaku(post_ids[i], ".\\" + tags.ToString(), sankaku_cookies))
                    {
                        //MyWait(start, 5000);
                        count_complit++;
                        break;
                    }
                    //MyWait(start, 7000);
                    if (index == Program.config.LimitErrors - 1)
                    {
                        count_error++;
                    }
                }
            }
            Console.WriteLine("Успешно скачано: {0}\nСкачано ренее: {1}\nУдалено ранее: {2}\nОшибочно: {3}\nВсего: {4}", count_complit, count_skip, count_deleted, count_error, post_ids.Count);
            return;
        }
        static void LoadSettings()
        {
            Program.config = new GetidolConfig();
            //Параметры по умолчанию
            Program.config.BaseURL = "https://chan.sankakucomplex.com/";
            Program.config.TimeOut = 5 * 1000;
            Program.config.TimeOutError = (5 * 60) * 1000;
            Program.config.ConnectionString = @"data source=C:\utils\erza\erza.sqlite";
            Program.config.UseDB = false;
            Program.config.UserAgent = "Mozilla / 5.0(Windows NT 6.2; WOW64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 34.0.1847.116 Safari / 537.36";
            Program.config.LimitErrors = 2;
            Program.config.SankakuLogin = null;
            Program.config.SankakuPassword = null;
            Program.config.UseProxy = false;
            Program.config.ProxyAddress = null;
            Program.config.ProxyPort = 8888;
            Program.config.ProxyLogin = null;
            Program.config.ProxyPassword = null;
            Program.config.DownloadPath = ".";
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(GetidolConfig));
            //jsonFormatter.WriteObject(System.IO.File.Create(".\\test.json"), Program.config);
            if (File.Exists(".\\Getidol.json"))
            {
                using (FileStream fs = new FileStream(".\\Getidol.json", FileMode.Open))
                {
                    Program.config = (GetidolConfig)jsonFormatter.ReadObject(fs);
                }
                return;
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Getidol\\Getidol.json"))
            {
                using (FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Getidol\\Getidol.json", FileMode.Open))
                {
                    Program.config = (GetidolConfig)jsonFormatter.ReadObject(fs);
                }
                return;
            }
            if (File.Exists(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\Getidol.json"))
            {
                using (FileStream fs = new FileStream(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "\\Getidol.json", FileMode.Open))
                {
                    Program.config = (GetidolConfig)jsonFormatter.ReadObject(fs);
                }
                return;
            }
            Console.WriteLine("Конфигурационный файл не найден!\nЗагружены настройки по умолчанью.");
        }
        static void ParseArgs(string[] args)
        {
            string start_page_string = "--start_page=";
            string max_page_string = "--max_page=";
            string nosqlite_string = "--nosqlite";
            string sqlite_path_string = "--sqlite-path=";
            Program.Tags = new List<string>();
            foreach (string param in args)
            {
                if (param == nosqlite_string)
                {
                    //Program.config.UseDB = false;
                    continue;
                }
                if (param.Length >= sqlite_path_string.Length)
                {
                    if (param.Substring(0, sqlite_path_string.Length) == sqlite_path_string)
                    {
                        //Program.config.ConnectionString = "data source=" + param.Substring(sqlite_path_string.Length);
                        continue;
                    }
                }
                if (param.Length >= start_page_string.Length)
                {
                    if (param.Substring(0, start_page_string.Length) == start_page_string)
                    {
                        if (param.Length > start_page_string.Length)
                        {
                            Program.StartPage = int.Parse(param.Substring(start_page_string.Length));
                            if(Program.StartPage < 1)
                            {
                                Console.WriteLine("Параметр {0} не может быть меньше 1", param);
                                Environment.Exit(1);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Не правильно задан параметр {0}", param);
                            Environment.Exit(1);
                        }
                        continue;
                    }
                }
                if (param.Length >= max_page_string.Length)
                {
                    if (param.Substring(0, max_page_string.Length) == max_page_string)
                    {
                        if (param.Length > max_page_string.Length)
                        {
                            Program.MaxPage = int.Parse(param.Substring(max_page_string.Length));
                            if (Program.MaxPage < 0)
                            {
                                Console.WriteLine("Параметр {0} не может быть меньше 0", param);
                                Environment.Exit(1);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Не правильно задан параметр {0}", param);
                            Environment.Exit(1);
                        }
                        continue;
                    }
                }
                Program.Tags.Add(param);
            }
        }
        static bool DownloadImageFromSankaku(int post_id, string dir, CookieCollection cookies)
        {
            Thread.Sleep(Program.config.TimeOut);
            string post = GetPostPage(post_id, cookies);
            if (post == null) { return false; }
            string url = GetOriginalUrlFromPostPage(post);
            if (url == null)
            {
                Console.WriteLine("URL Картинки не получен!");
                return false;
            }
            string filename = GetFileName(dir, url);
            if (ExistImage(Path.GetFileNameWithoutExtension(url)))
            {
                Console.WriteLine("Уже скачан.");
                return true;
            }
            Console.WriteLine("Начинаем закачку {0}.", url);
            FileInfo fi = new FileInfo(filename);
            //ВРЕМЕННО!!!!!!!!
            if (fi.Exists)
            {
                Console.WriteLine("Уже скачан.");
                return true;
            }
            Thread.Sleep(Program.config.TimeOut - 2000);
            HttpWebRequest httpWRQ = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            if (Program.config.UseProxy)
            {
                WebProxy myProxy = new WebProxy(Program.config.ProxyAddress, Program.config.ProxyPort);
                myProxy.Credentials = new NetworkCredential(Program.config.ProxyLogin, Program.config.ProxyPassword);
                httpWRQ.Proxy = myProxy;
            }
            WebResponse wrp = null;
            Stream rStream = null;
            try
            {
                httpWRQ.Referer = Program.config.BaseURL + "post/show/" + post_id.ToString();
                httpWRQ.UserAgent = Program.config.UserAgent;
                httpWRQ.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                httpWRQ.Headers.Add("Accept-Encoding: identity");
                //httpWRQ.CookieContainer = new CookieContainer();
                //httpWRQ.CookieContainer.Add(cookies);
                httpWRQ.Headers.Add(HttpRequestHeader.Cookie, RawCookies);
                httpWRQ.Timeout = 60 * 1000;
                wrp = httpWRQ.GetResponse();
                if (fi.Exists)
                {
                    if (wrp.ContentLength == fi.Length)
                    {
                        Console.WriteLine("Уже скачан.");
                        //wrp.Close();
                        return true;
                    }
                    else
                    {
                        fi.Delete();
                    }
                }
                long cnt = 0;
                rStream = wrp.GetResponseStream();
                rStream.ReadTimeout = 60 * 1000;
                using (FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    DateTime start = DateTime.Now;
                    while ((bytesRead = rStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                        cnt += bytesRead;
                        DateTime pred = DateTime.Now;
                        Console.Write("\rСкачано " + cnt.ToString("#,###,###") + " из " + wrp.ContentLength.ToString("#,###,###") + " байт Скорость: " + ((cnt / (pred - start).TotalSeconds) / 1024).ToString("0.00") + " Килобайт в секунду.");
                    }
                }
                if (cnt < wrp.ContentLength)
                {
                    Console.WriteLine("\nОбрыв! Закачка не завершена!");
                    return false;
                }
                else
                {
                    Console.WriteLine("\nЗакачка завершена.");
                    return true;
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (wrp != null)
                {
                    wrp.Close();
                }
                if (rStream != null)
                {
                    rStream.Close();
                }
            }
        }
        static string GetPostPage(int npost, CookieCollection cookies)
        {
            string strURL = Program.config.BaseURL + "post/show/" + npost.ToString();
            Console.WriteLine("Загружаем и парсим пост: " + strURL);
            while (true)
            {
                try
                {
                    return DownloadStringFromSankaku(strURL, null, cookies);
                }
                catch (WebException we)
                {
                    Console.WriteLine(we.Message);
                    Thread.Sleep(Program.config.TimeOutError);
                    return null;
                }
            }
        }
        static string GetOriginalUrlFromPostPage(string post)
        {
            string file_url = "<li>Original: <a href=\"";
            Regex rx = new Regex(file_url + @"\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?", RegexOptions.Compiled);
            try
            {
                Match match = rx.Match(post);
                if (match.Success)
                {
                    string url = match.Value.Substring(file_url.Length);
                    return "https:" + url;
                }
                else
                {
                    Regex rx_swf = new Regex("<p><a href=\"" + @"(?<protocol>http(s)?)://(?<server>([A-Za-z0-9-]+\.)*(?<basedomain>[A-Za-z0-9-]+\.[A-Za-z0-9]+))+((:)?(?<port>[0-9]+)?(/?)(?<path>(?<dir>[A-Za-z0-9\._\-/]+)(/){0,1}[A-Za-z0-9.-/_]*)){0,1}" + "\" >Save this flash \\(right click and save\\)</a></p>", RegexOptions.Compiled);
                    Match match_swf = rx_swf.Match(post);
                    if (match_swf.Success)
                    {
                        string url = match_swf.Value.Substring(12).Replace("\" >Save this flash (right click and save)</a></p>", String.Empty);
                        return url;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (ArgumentNullException)
            {
                return null;
            }
        }
        static List<int> GetImageInfoFromSankaku(string tag)
        {
            if (sankaku_cookies == null)
            {
                int temp = 0;
                for (; ; )
                {
                    sankaku_cookies = GetSankakuCookies(Program.config.BaseURL + "user/authenticate");
                    if (sankaku_cookies != null)
                    {
                        break;
                    }
                    else
                    {
                        if (temp < Program.config.LimitErrors)
                        {
                            temp++;
                            Thread.Sleep(Program.config.TimeOut);
                            continue;
                        }
                        else
                        {
                            Console.Write("Не удалось получить куки!");
                            Environment.Exit(1);
                        }
                    }
                }
            }
            List<int> imgs = new List<int>();
            //int i = 1;
            int i = Program.StartPage;
            while (true)
            {
                if (Program.MaxPage >= 0)
                {
                    if (i >= Program.StartPage + Program.MaxPage) { break; }
                }
                Thread.Sleep(Program.config.TimeOut);
                Console.Write("({0}/ХЗ) ", imgs.Count);
                //DateTime start = DateTime.Now;
                string text = DownloadHTML(Program.config.BaseURL, tag, i, sankaku_cookies);
                //MyWait(start, 5000);
                if (text != null)
                {
                    List<int> posts = ParseHTML_sankaku(text);
                    if (posts.Count > 0)
                    {
                        imgs.AddRange(posts);
                        i++;
                        continue;
                    }
                    else break;
                }
                else break;
            }
            return imgs;
        }
        static CookieCollection GetSankakuCookies(string url)
        {
            try
            {
                HttpWebRequest loginRequest = (HttpWebRequest)WebRequest.Create(url);
                if (Program.config.UseProxy)
                {
                    WebProxy myProxy = new WebProxy(Program.config.ProxyAddress, Program.config.ProxyPort);
                    myProxy.Credentials = new NetworkCredential(Program.config.ProxyLogin, Program.config.ProxyPassword);
                    loginRequest.Proxy = myProxy;
                }
                loginRequest.UserAgent = Program.config.UserAgent;
                loginRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                loginRequest.ContentType = "application/x-www-form-urlencoded";
                loginRequest.Headers.Add("Accept-Encoding: identity");
                loginRequest.CookieContainer = new CookieContainer();
                loginRequest.Method = "POST";
                string PostData = String.Format("user%5Bname%5D={0}&user%5Bpassword%5D={1}", Program.config.SankakuLogin, Program.config.SankakuPassword);
                Encoding encoding = Encoding.UTF8;
                byte[] byte1 = encoding.GetBytes(PostData);
                loginRequest.ContentLength = byte1.Length;
                using (Stream st = loginRequest.GetRequestStream())
                {
                    st.Write(byte1, 0, byte1.Length);
                    st.Close();
                }
                loginRequest.AllowAutoRedirect = false;
                HttpWebResponse loginResponse = (HttpWebResponse)loginRequest.GetResponse();
                RawCookies = loginResponse.Headers["Set-Cookie"];
                return loginResponse.Cookies;
            }
            catch (WebException we)
            {
                Console.WriteLine(we.Message);
                return null;
            }
        }
        static string DownloadStringFromSankaku(string url, string referer, CookieCollection cookies)
        {
            HttpWebRequest downloadRequest = (HttpWebRequest)WebRequest.Create(url);
            if (Program.config.UseProxy)
            {
                WebProxy myProxy = new WebProxy(Program.config.ProxyAddress, Program.config.ProxyPort);
                myProxy.Credentials = new NetworkCredential(Program.config.ProxyLogin, Program.config.ProxyPassword);
                downloadRequest.Proxy = myProxy;
            }
            downloadRequest.UserAgent = Program.config.UserAgent;
            downloadRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            downloadRequest.Headers.Add("Accept-Encoding: identity");
            downloadRequest.Headers.Add(HttpRequestHeader.Cookie, RawCookies);
            //downloadRequest.CookieContainer = new CookieContainer();
            //downloadRequest.CookieContainer.Add(cookies);
            if (referer != null)
            {
                downloadRequest.Referer = referer;
            }
            string source;
            using (StreamReader reader = new StreamReader(downloadRequest.GetResponse().GetResponseStream()))
            {
                source = reader.ReadToEnd();
            }
            return source;
        }
        static string DownloadHTML(string m_strBaseURL, string m_strTags, int nPage, CookieCollection cookies)
        {
            int count_503 = 0;
            string strURL = String.Format("{0}?tags={1}&page={2}", m_strBaseURL, m_strTags, nPage);
            Console.WriteLine("Загружаем и парсим: " + strURL);
            while (true)
            {
                try
                {
                    return DownloadStringFromSankaku(strURL, null, cookies);
                }
                catch (WebException we)
                {
                    Console.WriteLine("Ошибка: " + we.Message);
                    Thread.Sleep(Program.config.TimeOutError);
                    if (we.Response == null) { continue; }
                    if (((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (count_503 < Program.config.LimitErrors)
                        {
                            count_503++;
                            continue;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        static List<int> ParseHTML_sankaku(string html)
        {
            List<int> temp = new List<int>();
            Regex rx_digit = new Regex("[0-9]+", RegexOptions.Compiled);
            Regex rx = new Regex(@"PostModeMenu\.click\([0-9]*\)", RegexOptions.Compiled);
            MatchCollection matches = rx.Matches(html);
            foreach (Match match in matches)
            {
                temp.Add(int.Parse(rx_digit.Match(match.Value).Value));
            }
            return temp;
        }
        static string GetFileName(string dir, string url)
        {
            int temp = url.IndexOf('?');
            if (temp >= 0)
            {
                url = url.Substring(0, temp);
            }
            string extension = Path.GetExtension(url);
            if (extension == ".jpeg")
            {
                return dir + "\\" + Path.GetFileNameWithoutExtension(url) + ".jpg";
            }
            else
            {
                return dir + "\\" + Path.GetFileName(url);
            }
        }
        static void MyWait(DateTime start, int delay)
        {
            int current = (int)((DateTime.Now - start).TotalMilliseconds);
            if (current < delay)
            {
                Thread.Sleep(delay - current);
                return;
            }
            else
            {
                return;
            }
        }
        static bool ValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        static bool ExistImage(string hash_string)
        {
            if (hash_string == null)
            {
                throw new ArgumentNullException("hexString");
            }
            if ((hash_string.Length & 1) != 0)
            {
                throw new ArgumentOutOfRangeException("hexString", hash_string, "hexString must contain an even number of characters.");
            }
            byte[] hash = new byte[hash_string.Length / 2];
            for (int i = 0; i < hash_string.Length; i += 2)
            {
                hash[i / 2] = byte.Parse(hash_string.Substring(i, 2), NumberStyles.HexNumber);
            }
            using (SQLiteConnection connection = new SQLiteConnection(Program.config.ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand())
                {
                    command.CommandText = "select * from hash_tags where hash = @hash";
                    command.Parameters.AddWithValue("hash", hash);
                    command.Connection = connection;
                    SQLiteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        if (System.Convert.ToBoolean(reader["is_deleted"])) { return true; }
                        if (Convert.IsDBNull(reader["file_name"]))
                        {
                            return false;
                        }
                        reader.Close();
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
    [DataContract]
    public class GetidolConfig
    {
        [DataMember(Name = "BaseURL", IsRequired = true)]
        public string BaseURL;
        [DataMember(Name = "ConnectionString", IsRequired = true)]
        public string ConnectionString;
        [DataMember(Name = "UseDB", IsRequired = true)]
        public bool UseDB;
        [DataMember(Name = "DownloadPath", IsRequired = true)]
        public string DownloadPath;
        [DataMember(Name = "LimitErrors", IsRequired = true)]
        public int LimitErrors;
        [DataMember(Name = "TimeOut", IsRequired = true)]
        public int TimeOut;
        [DataMember(Name = "TimeOutError", IsRequired = true)]
        public int TimeOutError;
        [DataMember(Name = "SankakuLogin", IsRequired = true)]
        public string SankakuLogin;
        [DataMember(Name = "SankakuPassword", IsRequired = true)]
        public string SankakuPassword;
        [DataMember(Name = "UseProxy", IsRequired = true)]
        public bool UseProxy;
        [DataMember(Name = "ProxyAddress", IsRequired = true)]
        public string ProxyAddress;
        [DataMember(Name = "ProxyPort", IsRequired = true)]
        public int ProxyPort;
        [DataMember(Name = "ProxyLogin", IsRequired = true)]
        public string ProxyLogin;
        [DataMember(Name = "ProxyPassword", IsRequired = true)]
        public string ProxyPassword;
        [DataMember(Name = "UserAgent", IsRequired = true)]
        public string UserAgent;
    }
}
