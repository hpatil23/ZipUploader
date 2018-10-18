using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.IO;
using OpenQA.Selenium.Support.UI;
using System.Threading;


namespace zipUploader
{
    class Program
    {
        static string checkPointFile = "checkpoint.txt";
        static string inputDataFile = "input.csv";
        static string errorDataFile = "error_logs.csv";
        static int siteRefreshCounter = 10;
        static List<state> states = new List<state>();
        //change the no. of Tabs to 10 after testing
        static int noOfTabs = 4;
        static string login_URL = "https://www.shopify.com/login";
        static string finalUrl = "";
        class state
        {
            public String zip;
            public String name;
        }

        static void openTabs(IWebDriver driver)
        {
            for (int i = 0; i < noOfTabs; i++)
            {

                if (i == 0)
                {
                    driver.Manage().Window.Maximize();
                    driver.Navigate().GoToUrl(login_URL);
                    Console.WriteLine("Press any key and Enter when done with captcha");
                    Console.ReadLine();
                    String newUrl = driver.Url;
                    Console.WriteLine(newUrl);
                    finalUrl = newUrl + "/settings/taxes/US";

                    driver.FindElement(By.CssSelector("body")).SendKeys(Keys.Control + "t");
                    driver.SwitchTo().Window(driver.WindowHandles.Last());
                    //going directly to data entry page
                    driver.Navigate().GoToUrl(finalUrl);

                }
                else
                {
                    IJavaScriptExecutor temp = (IJavaScriptExecutor)driver;
                    string title3 = (string)temp.ExecuteScript("window.open();");
                    driver.SwitchTo().Window(driver.WindowHandles[i]);
                    driver.Navigate().GoToUrl(finalUrl);
                }
                WebDriverWait _wait = new WebDriverWait(driver, new TimeSpan(0, 1, 0));

                Boolean page_load_check = false;
                int j = 0;
                while (page_load_check == false)

                {
                    try
                    {
                        // checking if the page loaded or not
                        IWebElement state = driver.FindElement(By.Id("NewPresenceState"));
                        page_load_check = true;
                       
                    }
                    catch (Exception)
                    {
                        if (j > 60)
                        {
                           page_load_check=true;      // too much waited and tab didnot completed the refresh.
                        }
                        i++;
                        Thread.Sleep(2000);
                        page_load_check = false;
                    }
                }
                Thread.Sleep(5000);



            }
        }
        static int readCheckPoint()
        {
            int checkPoint = 0;

            if (!File.Exists(checkPointFile))
            {
                //create checkpoint.txt file to store the last zip code uploaded on site
                using (StreamWriter sw = File.CreateText(checkPointFile))
                {
                    sw.WriteLine(checkPoint);
                }

            }
            else
            {
                //read the file to get last checkpoint zip code
                using (StreamReader sr = File.OpenText(checkPointFile))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        checkPoint = Convert.ToInt32(s);
                    }
                }
            }

            return checkPoint;
        }

        static void statesToUpload(int startZip)
        {
            using (var data = new StreamReader(inputDataFile))
            {
                Console.WriteLine("Getting Data: ");
                int count = 0;
                while (!data.EndOfStream)
                {
                    var line = data.ReadLine();
                    String[] values = line.Split(',');
                    int check = Convert.ToInt32(values[0]);
                    if (check > startZip)
                    {
                        while (values[0].Length < 5)
                        {
                            values[0] = "0" + values[0];
                        }
                        state temp = new state();
                        temp.zip = values[0];
                        temp.name = values[1];
                        states.Add(temp);
                        count++;
                        Console.WriteLine(count);
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                    }
                }
                Console.WriteLine("Data Read Done");
                Console.WriteLine(states.Count);


            }

        }

        static void updateErrorlogs()
        {
            if (!File.Exists(errorDataFile))
            {
                //create error_logs.csv file to store the error inputs
                using (StreamWriter sw = File.CreateText(errorDataFile))
                {

                }

            }
        }

        static int refreshTab(IWebDriver driver)
        {
            driver.Navigate().GoToUrl(finalUrl);
            WebDriverWait _wait = new WebDriverWait(driver, new TimeSpan(0, 1, 0));

            Boolean page_load_check = false;
            int i = 0;
            while (page_load_check == false)
            {
                try
                {
                    // checking if the page loaded or not
                    IWebElement state = driver.FindElement(By.Id("NewPresenceState"));
                    page_load_check = true;
                   
                }
                catch (Exception)
                {
                    if (i > 60)
                    {
                        return -7;      // too much waited and tab didnot completed the refresh.
                    }
                    i++;
                    Thread.Sleep(2000);
                    page_load_check = false;
                }


            }
            return 0;
        }
        static int uploadZip(IWebDriver driver, state st)
        {
            try
            {


                Boolean page_load_check = false;    // need to provide some exit mechanism in case page load is not happening
                int i = 0;
                while (page_load_check == false && i < 5)
                {
                    try
                    {
                        // checking if the page loaded or not
                        driver.FindElement(By.Id("NewPresenceState"));
                        page_load_check = true;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(4000);
                        i++;
                        page_load_check = false;
                    }
                }

                if (i == 5)
                {
                    return -1;     // Error1: Page not loaded in given time. Need to refresh the page. and go to next tab to upload.
                }

                // if page is loaded
                // check if both state name text box and zip code text box are empty
                IWebElement state;
                IWebElement zip;
                state = driver.FindElement(By.Id("NewPresenceState"));
                string already_state = state.GetAttribute("value");
                zip = driver.FindElement(By.XPath("//input[@placeholder='Your zip code']"));
                string already_zip = zip.GetAttribute("value");

                if (!(string.IsNullOrEmpty(already_state)))
                {
                    state = driver.FindElement(By.Id("NewPresenceState"));
                    state.Clear();
                    Console.WriteLine("Garbage Data in State Text Box: Cleared");

                }


                // find state colum using JavaScript and put the data
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                string first_state = st.name.Substring(0, st.name.Length - 1);
                string second_state = st.name.Substring(st.name.Length - 1, 1);

                // string query = "document.getElementById('NewPresenceState').setAttribute('value', '" + first_state + "');";
                string query = "document.getElementById('NewPresenceState').setAttribute('value', '" + first_state + "');";
                // Thread.Sleep(100);
                string title = (string)js.ExecuteScript(query);
                Console.WriteLine("State stag1 added:");
                // Thread.Sleep(100);

                //initial garbage data check for zip code
                if (!(string.IsNullOrEmpty(already_zip)))
                {
                    zip = driver.FindElement(By.XPath("//input[@placeholder='Your zip code']"));
                    zip.Clear();
                    Console.WriteLine("Garbage Data in Zip code Text box: Cleared");
                }

                string query2 = "$(\"input[placeholder ^= 'Your zip code']\").val(\"" + st.zip + "\");";
                string title2 = (string)js.ExecuteScript(query2);
                Console.WriteLine("Zip stag1 added:");

                // stage 1 check for added value
                // in case conditions not met, return with negative -2

                state = driver.FindElement(By.Id("NewPresenceState"));
                string stag1_state = state.GetAttribute("value");
                zip = driver.FindElement(By.XPath("//input[@placeholder='Your zip code']"));
                string stag1_zip = zip.GetAttribute("value");

                //if (!(stag1_state.Equals(first_state)))
                if (!(stag1_state.Equals(first_state)))
                {

                    Console.WriteLine("Stage 1 state not match");
                    return -2;

                }
                if (!(stag1_zip.Equals(st.zip)))
                {

                    Console.WriteLine("Stage 1 zip code not match");
                    return -2;
                }

                // if all correct till now. Execute Stage 2
                // otherwise stag2: to activate the add state button
                state = driver.FindElement(By.Id("NewPresenceState"));
                state.SendKeys(second_state);
                //  Thread.Sleep(100);
                zip = driver.FindElement(By.XPath("//input[@placeholder='Your zip code']"));
                zip.SendKeys(" ");

                Console.WriteLine("Stage2 complete");
                // stag3: checking the added data before hitting button
                state = driver.FindElement(By.Id("NewPresenceState"));
                string stag2_state = state.GetAttribute("value");
                zip = driver.FindElement(By.XPath("//input[@placeholder='Your zip code']"));
                string stag2_zip = zip.GetAttribute("value");

                if (!stag2_state.Equals(st.name))
                {
                    Console.WriteLine("Stage2: State Failed ");
                    return -3;
                }
                if (!stag2_zip.Contains(st.zip))
                {
                    Console.WriteLine("stag 2: zip Failed");
                    return -3;
                }


                driver.FindElement(By.XPath("//button[text()='Add state']")).Click();
                // updating checkpoint 
                using (StreamWriter outputFile = new StreamWriter(checkPointFile))
                {
                    outputFile.WriteLine(st.zip);
                }
                siteRefreshCounter--;
                Console.WriteLine("Done....");
                // add refresh page line here
                driver.Navigate().GoToUrl(finalUrl);
                return 1;
            }
            catch (Exception)
            {
                Console.WriteLine("Error Occured");
                return -5;
            }
        }
        static void Main(string[] args)
        {
            int startZipCode = readCheckPoint();

            Console.WriteLine("Execution Started:");
            Console.WriteLine("Last check point zip code: " + startZipCode);
            Console.WriteLine("Loading rest of the Input data.........");

            //list to store input state data
            statesToUpload(startZipCode);

            updateErrorlogs();
            using (var driver = new ChromeDriver())
            {
                openTabs(driver);
                Console.WriteLine("Page load done.. Waiting additional 5 Secs");
                Thread.Sleep(5000);
                int x = 0;
                int j = 0;
                while (x < states.Count)
                {
                    Console.WriteLine("Zip Code: " + states[x].zip + "  Index: " + x + "  Tab: " + j);

                    if (x < states.Count)
                    {
                        driver.SwitchTo().Window(driver.WindowHandles[j]);    //take the handle of the driver, execution will be from left to right
                        int res = uploadZip(driver, states[x]);
                        if (res == 1)    // sucess in adding the element
                        {
                            j++;   // going to next tab
                            x++;  // new entry upload in new tab
                        }
                        else if (res == -1 || res == -5)
                        {
                            // error in page load. Timeout after waiting for some time.
                            // refresh the page. 
                            // need to call the refresh function 
                            int check = refreshTab(driver);
                            if (check == -7)
                            {
                                j++;
                            }

                        }
                        else if (res == -2)
                        {
                            // refresh the site
                            // do not update x and j
                            int check = refreshTab(driver);
                            if (check == -7)
                            {
                                j++;
                            }

                        }
                        else if (res == -3)
                        {
                            // refresh the site
                            // stage 3 fail, do not update x and j
                            int check = refreshTab(driver);
                            if (check == -7)
                            {
                                j++;
                            }

                        }
                        j = j % noOfTabs;

                    }
                }
            }
        }





    }


}
