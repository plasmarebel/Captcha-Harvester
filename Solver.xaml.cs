using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CookieMonster.SpoofBrowser.CaptchaSolver
{
    /// <summary>
    /// Interaction logic for Solver.xaml
    /// </summary>
    public partial class Solver : Window
    {
        ChromiumWebBrowser chromeBrowser;
        internal static Solver main;
        DispatcherTimer timer = new DispatcherTimer();
        public string Site;
        private string CaptchaToken = string.Empty;

        public Solver(string Site, string SiteKey)
        {
            InitializeComponent();
            main = this;
            this.Site = Site;
            this.SiteKey = SiteKey;
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += timer_Tick;

            StartBrowser();
        }

        private void StartBrowser()
        {
            var settings = new CefSettings();
            settings.UserAgent = Properties.Settings.Default.userAgent;
            try
            {
                Cef.Initialize(settings);
            }
            catch { }


            var rc3 = new RequestContext();
            chromeBrowser = new ChromiumWebBrowser(Site)
            {
                RequestContext = rc3,
                LifeSpanHandler = new LifeSpanHandler()
            };

            browserContainer.Content = chromeBrowser;
            chromeBrowser.LoadingStateChanged += OnLoadingStateChanged;
        }

        int InitialLoad = 0;
        private void OnLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                InitialLoad++;
                if (InitialLoad == 1)
                {
                    LoadCaptcha();
                    timer.Start();                   
                }
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            IsSolved();
        }

        private void IsSolved()
        {
            chromeBrowser.GetMainFrame().EvaluateScriptAsync("get_action(this);").ContinueWith(x =>
            {
                var response = x.Result;
                if (response.Success && response.Result != null)
                {
                    var startDate = response.Result;
                    if (startDate.ToString() == "true" || startDate.ToString() == "True")
                    {
                        CaptchaResponse();
                    }
                }
            });
        }

        private void CaptchaResponse()
        {
            chromeBrowser.GetMainFrame().EvaluateScriptAsync("document.getElementById('g-recaptcha-response').value";).ContinueWith(x =>
            {
                var response = x.Result;
                if (response.Success && response.Result != null)
                {
                    var token = response.Result;
                    string TOKEN = token.ToString();
                    CaptchaToken = TOKEN;
                    timer.Stop();
                }
            });
        }

        public void LoadCaptcha()
        {
            Thread.Sleep(2000);
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync(@"document.querySelector('html').innerHTML = `
                <body bgcolor='#1f2125'>                
                        <form action='/submit' method='POST' style='margin: auto; margin-top: 100px; width: 300px;'>
                <div id='captchaFrame'>
                <div class='g-recaptcha' id='captchaFrame' data-sitekey='" + SiteKey + @"' data-callback='recaptchaCallback' style='height: 78px;'></div>                
                </div>
                </form></body>`");
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync("function get_token(form){return document.getElementById('recaptcha-token').getAttribute('value');}");
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync("function get_action(form){var v=grecaptcha.getResponse();if(v.length==0){return false;}else{return true;}}");
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync("var script = document.createElement('script');");
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync("script.setAttribute('src', 'https://www.google.com/recaptcha/api.js');");
            chromeBrowser.GetMainFrame().ExecuteJavaScriptAsync("document.head.appendChild(script)");
            MainBackground.Dispatcher.Invoke(new Action(() =>
            {
                MainBackground.Visibility = Visibility.Hidden;
            }));
            SpinIcon.Dispatcher.Invoke(new Action(() =>
            {
                SpinIcon.Visibility = Visibility.Hidden;
            }));
            PageLbl.Dispatcher.Invoke(new Action(() =>
            {
                PageLbl.Visibility = Visibility.Hidden;
            }));
        }
        public bool WaitingForSolve = false;
        private string SiteKey;
    }
}
