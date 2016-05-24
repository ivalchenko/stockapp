using System;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Web.Services;
using Newtonsoft.Json;
using System.Linq;

namespace sample
{
    public partial class _Default : System.Web.UI.Page
    {
        // Stock symbols seperated by space or comma.
        protected string m_symbol = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                if (Request.QueryString["s"] == null)
  
                    m_symbol = "GOOG"; // e.g. MSFT, TSLA, AAPL
                else
                m_symbol = Request.QueryString["s"].ToString().ToUpper();
                txtSymbol.Value = m_symbol;
                divService.InnerHtml = "<br />";
                if (m_symbol.Trim() != "")
                {
                    try
                    {
                        // Return the stock quote data in XML format.
                        String arg = GetQuote(m_symbol.Trim());                        
                        if (arg == null)
                            return;

                        XmlDocument xd = new XmlDocument();
                        xd.LoadXml(arg);
                        
                        XslCompiledTransform xslt = new XslCompiledTransform();
                        xslt.Load(Server.MapPath("stock.xsl"));
                                                
                        StringWriter fs = new StringWriter();
                        xslt.Transform(xd.CreateNavigator(), null, fs);
                        string result = fs.ToString();

                        divService.InnerHtml = "<br />" + result.Replace("&lt;", "<").Replace("&gt;", ">") + "<br />";

                        String[] symbols = m_symbol.Replace(",", " ").Split(' ');

                        for (int i = 0; i < symbols.Length; ++i)
                        {
                            if (symbols[i].Trim() == "")
                                continue;
                            int index = divService.InnerHtml.ToLower().IndexOf(symbols[i].Trim().ToLower() + " is invalid.");

                            if (index == -1)
                            {                                
                                Random random = new Random();
                                divService.InnerHtml += "<img id='imgChart_" + i.ToString() + "' src='http://ichart.finance.yahoo.com/b?s=" + symbols[i].Trim().ToUpper() + "& " + random.Next() + "' border=0><br />";
                                // 1 days
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(0," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div1d_" + i.ToString() + "'><b>1d</b></span></a>&nbsp;&nbsp;";
                                // 5 days
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(1," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div5d_" + i.ToString() + "'>5d</span></a>&nbsp;&nbsp;";
                                // 3 months
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(2," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div3m_" + i.ToString() + "'>3m</span></a>&nbsp;&nbsp;";
                                // 6 months
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(3," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div6m_" + i.ToString() + "'>6m</span></a>&nbsp;&nbsp;";
                                // 1 yeas
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(4," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div1y_" + i.ToString() + "'>1y</span></a>&nbsp;&nbsp;";
                                // 2 years
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(5," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div2y_" + i.ToString() + "'>2y</span></a>&nbsp;&nbsp;";
                                // 5 years
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(6," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='div5y_" + i.ToString() + "'>5y</span></a>&nbsp;&nbsp;";
                                // Max
                                divService.InnerHtml += "<a style='font-family: Arial, Helvetica, sans-serif; font-size: 14px; color: Blue;' href='javascript:changeChart(7," + i.ToString() + ", \"" + symbols[i].ToLower() + "\");'><span id='divMax_" + i.ToString() + "'>Max</span></a><br><br /><br />&nbsp;&nbsp;";
                            }
                        }
                    }
                    catch(Exception exc)
                    {
                        Console.WriteLine("Got an error: " + exc.Message);
                    }
                }
            }
        }

        public string GetQuote(string symbol)
        {
            string result = null;            
            try
            {
                string yahooURL = @"http://download.finance.yahoo.com/d/quotes.csv?s=" + symbol + "&f=sl1d1t1c1hgvbap2";

                string[] symbols = symbol.Replace(",", " ").Split(' ');

                HttpWebRequest webreq = (HttpWebRequest)WebRequest.Create(yahooURL);
                HttpWebResponse webresp = (HttpWebResponse)webreq.GetResponse();
                StreamReader strm = new StreamReader(webresp.GetResponseStream(), Encoding.ASCII);               

                List<HistoricalStock> data = HistoricalStockDownloader.DownloadData(symbol, 1962);

                var buff = from p in data where p.Date.Year == 2016
                          select new
                           {
                               p.Date,
                               p.High,
                               p.Low
                           };

                DateTime dateOfHighestPrice = buff.Last().Date;
                NewsModel._fromDate = dateOfHighestPrice.AddDays(-1);
                NewsModel._toDate = dateOfHighestPrice.AddDays(+1);

                NewsModel._phrase = symbol;

                string tmp = "<StockQuotes>";
                string content = "";
                for (int i = 0; i < symbols.Length; i++)
                {
                    if (symbols[i].Trim() == "")
                        continue;

                    content = strm.ReadLine().Replace("\"", "");
                    string[] contents = content.ToString().Split(',');
                    // If contents[2] = "N/A". the stock symbol is invalid.
                    if (contents[2] == "N/A")
                    {
                        tmp += "<Stock>";
                        tmp += "<Symbol>&lt;span style='color:red'&gt;" + symbols[i].ToUpper() + " is invalid.&lt;/span&gt;</Symbol>";
                        tmp += "<Last></Last>";
                        tmp += "<Date></Date>";
                        tmp += "<Time></Time>";
                        tmp += "<Change></Change>";
                        tmp += "<High></High>";
                        tmp += "<Low></Low>";
                        tmp += "<Volume></Volume>";
                        tmp += "<Bid></Bid>";
                        tmp += "<Ask></Ask>";
                        tmp += "<Ask></Ask>";
                        tmp += "</Stock>";
                    }
                    else
                    {
                        tmp += "<Stock>";
                        tmp += "<Symbol>" + contents[0] + "</Symbol>";
                        try
                        {
                            tmp += "<Last>" + String.Format("{0:c}", Convert.ToDouble(contents[1])) + "</Last>";
                        }
                        catch
                        {
                            tmp += "<Last>" + contents[1] + "</Last>";
                        }
                        tmp += "<Date>" + contents[2] + "</Date>";
                        tmp += "<Time>" + contents[3] + "</Time>";
                        if (contents[4].Trim().Substring(0, 1) == "-")
                            tmp += "<Change>&lt;span style='color:red'&gt;" + contents[4] + "(" + contents[10] + ")" + "&lt;span&gt;</Change>";
                        else if (contents[4].Trim().Substring(0, 1) == "+")
                            tmp += "<Change>&lt;span style='color:green'&gt;" + contents[4] + "(" + contents[10] + ")" + "&lt;span&gt;</Change>";
                        else
                            tmp += "<Change>" + contents[4] + "(" + contents[10] + ")" + "</Change>";
                        tmp += "<High>" + contents[5] + "</High>";
                        tmp += "<Low>" + contents[6] + "</Low>";
                        try
                        {
                            tmp += "<Volume>" + String.Format("{0:0,0}", Convert.ToInt64(contents[7])) + "</Volume>";
                        }
                        catch
                        {
                            tmp += "<Volume>" + contents[7] + "</Volume>";
                        }
                        tmp += "<Bid>" + contents[8] + "</Bid>";
                        tmp += "<Ask>" + contents[9] + "</Ask>";
                        tmp += "</Stock>";
                    }

                    result += tmp;
                    tmp = "";
                }

                result += "</StockQuotes>";
                strm.Close();
            }
            catch(Exception exc)
            {
                Console.WriteLine("Got an error: " + exc.Message);
            }

            return result;
        }

        [WebMethod]
        public static ItemNews[] GetNewsContent(string searchPhrase, string dateRange, string direction)
        {
            // Put dates into static fields of NewsModel
            SetDateRange(Convert.ToInt32(dateRange));
           
            List<ItemNews> Details = new List<ItemNews>();

            string guardianURL = @"http://content.guardianapis.com/search?q=" + searchPhrase + "&section=technology&show-references=all&from-date=" + NewsModel._fromDate.ToString("yyyy-MM-dd") + "&to-date=" + NewsModel._toDate.ToString("yyyy-MM-dd") + "&order-by=oldest&page-size=50" + "&api-key=6392a258-3c53-4e76-87ec-e9092356fa74";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(guardianURL);
            request.Method = "GET";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {

                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == "")
                    readStream = new StreamReader(receiveStream);
                else
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));

                string data = readStream.ReadToEnd();

                DataSet ds = new DataSet();
                StringReader reader = new StringReader(data);

                try
                {
                    var model = JsonConvert.DeserializeObject<NewsModel.RootObject>(data);
                    foreach(NewsModel.Result res in model.response.results)
                    {
                        ItemNews news = new ItemNews();
                        news.title = res.webTitle;
                        news.link = res.webUrl;
                        news.item_id = res.id;
                        news.PubDate = Convert.ToDateTime(res.webPublicationDate).ToShortDateString();

                        Details.Add(news);
                    }
                        
                } catch(Exception exc)
                {
                    Console.WriteLine("Got an error: " + exc.Message);
                }
            }

            return Details.ToArray();
        }

        public static void SetDateRange(int date)
        {
            List<HistoricalStock> data = new List<HistoricalStock>();
            NewsModel._toDate = HistoricalStockDownloader.StockList.Last().Date;

            switch ((Dates)date)
            {
                case (Dates.day1):
                    NewsModel._fromDate = NewsModel._toDate;
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.days5):
                    NewsModel._fromDate = NewsModel._toDate.AddDays(-5);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.months3):
                    NewsModel._fromDate = NewsModel._toDate.AddMonths(-3);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.months6):
                    NewsModel._fromDate = NewsModel._toDate.AddMonths(-6);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.year1):
                    NewsModel._fromDate = NewsModel._toDate.AddYears(-1);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.years2):
                    NewsModel._fromDate = NewsModel._toDate.AddYears(-2);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.years5):
                    NewsModel._fromDate = NewsModel._toDate.AddYears(-5);
                    data = GetStockByExactDateRange();
                    break;

                case (Dates.max):
                    NewsModel._fromDate = HistoricalStockDownloader.StockList.First().Date;
                    data = GetStockByExactDateRange();
                    break;
            }

            data.Sort(delegate (HistoricalStock hs1, HistoricalStock hs2) { return hs1.High.CompareTo(hs2.High); });

            NewsModel._fromDate = data.Last().Date.Date.AddDays(-1);
            NewsModel._toDate = data.Last().Date.Date.AddDays(+1);
        }

        public static List<HistoricalStock> GetStockByExactDateRange()
        {
            return HistoricalStockDownloader.StockList.Where(s => s.Date >= NewsModel._fromDate && s.Date <= NewsModel._toDate).ToList();
        }

        public class ItemNews
        {
            public string title { get; set; }
            public string link { get; set; }
            public string item_id { get; set; }
            public string PubDate { get; set; }
            public string Description { get; set; }

            public override string ToString()
            {
                return string.Format("Title: {0}, Link: {1}, Pubdate: {2}.", title, link, PubDate);
            }
        }

        public enum Dates { day1 = 1, days5 = 2, months3 = 3, months6 = 4, year1 = 5, years2 = 6, years5 = 7, max = 8 };
        public enum Directions { up = 1, down = 2 };
    }
}

