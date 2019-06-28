using OpenQA.Selenium.Chrome;
using ScrapySharp.Network;
using System;
using System.Configuration;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace Dribble.Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("no-sandbox");
            chromeOptions.AddArguments("--start-maximized");
            chromeOptions.AddUserProfilePreference("intl.accept_languages", "nl");
            chromeOptions.AddUserProfilePreference("disable-popup-blocking", "true");

            GrabDribbleData(chromeOptions);
        }

        /// <summary>
        /// Grab Dribble data
        /// </summary>
        /// <param name="chromeOptions"></param>
        private static void GrabDribbleData(ChromeOptions chromeOptions)
        {
            try
            {
                // Initialize the Chrome Driver
                using (var driver = new ChromeDriver(chromeOptions))
                {
                    // Go to the home page
                    driver.Navigate().GoToUrl("https://dribbble.com/");

                    Thread.Sleep(5000);
                    var linkLogin = driver.FindElementById("t-signin");
                    linkLogin.Click();

                    Thread.Sleep(5000);
                    // Get the page elements
                    var txtUserName = driver.FindElementById("login");
                    var txtPassword = driver.FindElementById("password");
                    var btnLogin = driver.FindElementByClassName("button");

                    // get user name and password from App Setting
                    string strUserName = ConfigurationManager.AppSettings["username"];
                    txtUserName.SendKeys(strUserName);
                    //website has two password text boxes. So I have to click in first then put password in second to make it working.
                    string strPassword = ConfigurationManager.AppSettings["password"];
                    txtPassword.SendKeys(strPassword);

                    Thread.Sleep(5000);
                    // and click the login button
                    btnLogin.Click();
                    Thread.Sleep(5000);

                    var browser = new ScrapingBrowser();
                    string baseLink = "https://dribbble.com/designers";
                    WebPage pageResp = browser.NavigateToPage(new Uri(baseLink));

                    var li = pageResp.Html.SelectNodes(".//div[@class='results-pane']/ol/li");
                    foreach (var item in li)
                    {
                        if (!string.IsNullOrEmpty(item.InnerText))
                        {
                            Console.WriteLine("----------------------------------------------------------------------------------------------------------------------------------------");
                            //get all designers and terms step by step
                            string pattern = "<a\\s+(?:[^>]+\\s+)?class\\s*=\\s*(?:jobs-mark(?=\\s|>)|(['\"])(?:(?:(?!\\1).)*?\\s+)*jobs-mark(?:\\s+(?:(?!\\1).)*?)*\\1)[^>]*>(.*?)</a>";
                            var inner = Regex.Match(item.InnerHtml, pattern, RegexOptions.Singleline);
                            var getHrefValue = Regex.Match(inner.Value, "href\\s*=\\s*\"(?<url>.*?)\"").Groups["url"].Value;
                            var finalValue = getHrefValue.Split('/')[1];

                            string designerUrl = "https://dribbble.com/" + finalValue;
                            //get all profile details
                            WebPage getInformation = browser.NavigateToPage(new Uri(designerUrl));

                            string getNameInfo = string.Empty;
                            var nameDetails = getInformation.Html.SelectNodes(".//div[@class='profile-info-mod profile-essentials']/h1/a");
                            if (nameDetails != null)
                            {
                                getNameInfo = Regex.Replace(nameDetails[0].InnerText, @"\t|\n|\r", "").Trim();
                            }

                            string getAddressInfo = string.Empty;
                            var addressDetails = getInformation.Html.SelectNodes(".//div[@class='profile-info-mod profile-essentials']/h1/span[@class='location']/span");
                            if (addressDetails != null)
                            {
                                getAddressInfo = Regex.Replace(addressDetails[0].InnerText, @"\t|\n|\r", "").Trim();
                            }

                            string getPersonalInfo = string.Empty;
                            var personnelInformation = getInformation.Html.SelectNodes(".//div[@class='bio']");
                            if (personnelInformation != null)
                            {
                                getPersonalInfo = Regex.Replace(personnelInformation[0].InnerText, @"\t|\n|\r", "").Trim();
                            }
                            Console.WriteLine("*******  *Main Information*  **************");
                            Console.WriteLine(getNameInfo + "\n" + getAddressInfo + "\n" + getPersonalInfo);

                            var skillList = getInformation.Html.SelectNodes(".//div[@class='floating-sidebar-float']/div/ul/li");
                            if (skillList != null)
                            {
                                Console.WriteLine("**************  *Skills*  **************");
                                foreach (var skill in skillList)
                                {
                                    string data = Regex.Replace(skill.InnerText, @"\t|\n|\r", "");
                                    Console.WriteLine(data.Trim());
                                }
                            }

                            var onTeams = getInformation.Html.SelectNodes(".//div[@class='floating-sidebar-extra']/div[@class='profile-info-mod']/ul[@class='profile-details on-teams']/li");
                            if (onTeams != null)
                            {
                                Console.WriteLine("**************  *ON TEAMS*  **************");
                                foreach (var team in onTeams)
                                {
                                    string data = Regex.Replace(team.InnerText, @"\t|\n|\r", "");
                                    Console.WriteLine(data.Trim());
                                }
                            }

                            var otherDetails = getInformation.Html.SelectNodes(".//div[@class='floating-sidebar-extra']/div[@class='profile-info-mod']/ul[@class='profile-details']/li");
                            if (otherDetails != null)
                            {
                                Console.WriteLine("**************  *ELSEWHERE*  **************");
                                foreach (var elseWhere in otherDetails)
                                {
                                    string data = Regex.Replace(elseWhere.InnerText, @"\t|\n|\r", "");
                                    Console.WriteLine(data.Trim());
                                }
                            }

                            var projectList = getInformation.Html.SelectNodes(".//div[@class='floating-sidebar-extra']/div[@class='profile-info-mod']/ul[@class='profile-projects']/li");
                            if (projectList != null)
                            {
                                Console.WriteLine("**************  *PROJECTS*  **************");
                                foreach (var project in projectList)
                                {
                                    string data = Regex.Replace(project.InnerText, @"\t|\n|\r", "").Trim();
                                    Console.WriteLine(data.Replace(" ", String.Empty));
                                }
                            }

                            var featuredList = getInformation.Html.SelectNodes(".//div[@class='floating-sidebar-extra']/div[@class='profile-info-mod']/ul[@class='profile-details profile-featured']/li");
                            if (featuredList != null)
                            {
                                Console.WriteLine("**************  *FEATURED*  **************");
                                foreach (var feature in featuredList)
                                {
                                    string data = Regex.Replace(feature.InnerText, @"\t|\n|\r", "");
                                    Console.WriteLine(data.Trim());
                                }
                            }
                        }
                    }
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                Console.ReadLine();
            }
        }
    }
}
