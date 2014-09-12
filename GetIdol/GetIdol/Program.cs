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

namespace GetIdol
{
    class Program
    {
        static CookieCollection sankaku_cookies = null;
        static string BaseURL = "https://idol.sankakucomplex.com/post/index";
        static int LIMIT_ERRORS = 4;
        static string UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/34.0.1847.116 Safari/537.36";
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = ValidationCallback;
            Console.WriteLine("Импортируем тег " + args[0] + " с санкаки");
            List<int> post_ids = GetImageInfoFromSankaku(args[0]);
            Console.Write("\n\n\n\t\tНАЧИНАЕТСЯ ЗАГРУЗКА\n\n\n");
            int num6 = download(post_ids, ".\\" + args[0]);
        }
        static bool DownloadImageFromSankaku(int post_id, string dir, CookieCollection cookies)
        {
            string post = GetPostPage(post_id, cookies);
            if (post == null) { return false; }
            string url = GetOriginalUrlFromPostPage(post);
            if (url == null)
            {
                Console.WriteLine("URL Картинки не получен!");
                return false;
            }
            string filename = GetFileName(dir, url);

            Console.WriteLine("Начинаем закачку {0}.", url);
            FileInfo fi = new FileInfo(filename);
            HttpWebRequest httpWRQ = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            WebResponse wrp = null;
            Stream rStream = null;
            try
            {
                httpWRQ.Referer = "https://idol.sankakucomplex.com/post/show/" + post_id.ToString();
                httpWRQ.UserAgent = UserAgent;
                httpWRQ.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                httpWRQ.Headers.Add("Accept-Encoding: identity");
                httpWRQ.CookieContainer = new CookieContainer();
                httpWRQ.CookieContainer.Add(cookies);
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
                    //DateTime finish = DateTime.Now;
                    //Console.WriteLine("Средняя скорость загрузки составила {0} байт в секунду.", ((cnt / (finish - start).TotalSeconds) / 1024).ToString("0.00"));
                }
                if (cnt < wrp.ContentLength)
                {
                    Console.WriteLine("\nОбрыв! Закачка не завершена!");
                    //rStream.Close();
                    //wrp.Close();
                    return false;
                }
                else
                {
                    Console.WriteLine("\nЗакачка завершена.");
                    //rStream.Close();
                    //wrp.Close();
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
            string strURL = "https://idol.sankakucomplex.com/post/show/" + npost.ToString();
            Console.WriteLine("Загружаем и парсим пост: " + strURL);
            //WebClient Client = new WebClient();
            //Uri uri = new Uri(strURL);
            //Client.Headers.Add("user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
            while (true)
            {
                try
                {
                    return DownloadStringFromSankaku(strURL, null, cookies);
                    //return Client.DownloadString(uri);
                }
                catch (WebException we)
                {
                    Console.WriteLine(we.Message);
                    Thread.Sleep(10000);
                    return null;
                }
            }
        }
        static string GetOriginalUrlFromPostPage(string post)
        {
            string file_url = "<li>Original: <a href=\"";
            //Regex rx = new Regex(file_url + "(?:(?:ht|f)tps?://)?(?:[\\-\\w]+:[\\-\\w]+@)?(?:[0-9a-z][\\-0-9a-z]*[0-9a-z]\\.)+[a-z]{2,6}(?::\\d{1,5})?(?:[?/\\\\#][?!^$.(){}:|=[\\]+\\-/\\\\*;&~#@,%\\wА-Яа-я]*)?", RegexOptions.Compiled);
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
                    sankaku_cookies = GetSankakuCookies("https://idol.sankakucomplex.com/");
                    if (sankaku_cookies != null)
                    {
                        break;
                    }
                    else
                    {
                        if (temp < 4)
                        {
                            temp++;
                            Thread.Sleep(5000);
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
            int i = 1;
            while (true)
            {
                Console.Write("({0}/ХЗ) ", imgs.Count);
                DateTime start = DateTime.Now;
                string text = DownloadHTML(BaseURL, tag, i, sankaku_cookies);
                MyWait(start, 5000);
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
                loginRequest.UserAgent = UserAgent;
                loginRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                loginRequest.Headers.Add("Accept-Encoding: identity");
                loginRequest.CookieContainer = new CookieContainer();
                HttpWebResponse loginResponse = (HttpWebResponse)loginRequest.GetResponse();
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
            downloadRequest.UserAgent = UserAgent;
            downloadRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            downloadRequest.Headers.Add("Accept-Encoding: identity");
            downloadRequest.CookieContainer = new CookieContainer();
            downloadRequest.CookieContainer.Add(cookies);
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
                    Thread.Sleep(60000);
                    if (we.Response == null) { continue; }
                    if (((HttpWebResponse)we.Response).StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (count_503 < 4)
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
                //string json = match.Value.Substring(19, match.Value.Length - 20);
                //json = json.Remove(json.Length-2);
                //MatchCollection m = rx_digit.Matches(match.Value);
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
        static int download(List<int> list, string dir)
        {
            int count_complit = 0;
            int count_deleted = 0;
            int count_error = 0;
            int count_skip = 0;
            Directory.CreateDirectory(dir);
            for (int i = 0; i < list.Count; i++)
            {
                Console.WriteLine("\n###### {0}/{1} ######", (i + 1), list.Count);
                long r = DownloadImage(list[i], dir);
                if (r == 0)
                {
                    count_complit++;
                }
                else
                {
                    count_error++;
                }
            }
            Console.WriteLine("Успешно скачано: {0}\nСкачано ренее: {1}\nУдалено ранее: {2}\nОшибочно: {3}\nВсего: {4}", count_complit, count_skip, count_deleted, count_error, list.Count);
            return count_error;
        }
        static long DownloadImage(int post_id, string dir)
        {
            for (int index = 0; index < LIMIT_ERRORS; index++)
            {
                    DateTime start = DateTime.Now;
                    if (DownloadImageFromSankaku(post_id, dir, sankaku_cookies))
                    {
                        MyWait(start, 5000);
                        return 0;
                    }
                    MyWait(start, 5000);
            }
            return 1;
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
    }
}
