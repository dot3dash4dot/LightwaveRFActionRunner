using DeviceDetector;
using LightwaveRFLinkPlusSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using Utilities;

namespace LightwaveDaemon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LightwaveAPI _api;
        private Device[] _devices;

        private Logger _logger;
        private StringBuilder _errorStringBuilder;

        private Timer _waitTimer;
        private DaemonAutomation _nextAutomation;
        private TimeSpan _todaysDuskTime;

        private int _automationRetryCount = 0;
        private int _automationRetryLimit = 3;

        Dictionary<Phone, TextBlock> _personTextBlocks = new Dictionary<Phone, TextBlock>();
        private const string _timeSpanFormat = @"hh\:mm";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _personTextBlocks.Add(Configuration.Phones[0], tbPerson1);

            Setup();
        }

        public async void Setup()
        {
            _logger = new Logger();
            _errorStringBuilder = new StringBuilder();

            if (!Debugger.IsAttached)
            {
                //Send an email at the start to check that we can
                if (!Helpers.SendEmail(
                        Configuration.EmailFromAddress,
                        Configuration.EmailToAddress,
                        "Lightwave Daemon starting up",
                        "",
                        null,
                        out Exception ex))
                {
                    MessageBox.Show("Failed to send start-up email. Daemon will exit.");
                    Application.Current.Shutdown();
                }
            }

            // In order to connect to the Lightwave API you must provide a bearer ID and an initial refresh token. You can get these
            // from https://my.lightwaverf.com > Settings > API. (The bearer ID is the long string labelled "Basic" for some reason.)
            // During use of the API, further refresh tokens will be provided which will be handled for you automatically. If you stop
            // being able to access the API at any point, however, you will have to request a new refresh token from the Lightwave site
            // and provide it in this constructor.
            _api = new LightwaveAPI("INSERT BEARER TOKEN HERE", "INSERT INITIAL REFRESH TOKEN HERE");

            try
            {
                _devices = await _api.GetDevicesInFirstStructureAsync();
            }
            catch
            {
                MessageBox.Show("Failed to connect to Lightwave API. Daemon will exit.");
                Application.Current.Shutdown();
            }

            //Check all device names in automations are known
            foreach (DeviceName device in Configuration.DaemonAutomations.SelectMany(x => x.StateChanges).Select(x => x.DeviceName))
            {
                try
                {
                    device.ToDevice(_devices);
                }
                catch
                {
                    throw new Exception($"Device '{device.DisplayName()}' not found in devices fetched from Lightwave");
                }
            }

            _ = WaitForNextAutomation(true);
        }

        public async Task WaitForNextAutomation(bool newDay)
        {
            if (_waitTimer != null)
            {
                try
                {
                    //Dispose of old timer
                    _waitTimer.Stop();
                    _waitTimer.Dispose();
                }
                catch
                {
                    Output("Failed to dispose of old timer", true);
                }
            }

            if (newDay)
            {
                Output($"Starting a new day");

                try
                {
                    Device linkPlus = _devices.First(x => x.Name == "LinkPlus");
                    _todaysDuskTime = await _api.GetDuskTimeTimeZoneAdjustedAsync(linkPlus);
                }
                catch (Exception ex)
                {
                    Output($"Failed to get dusk time, so using 7pm instead. Exception:\r\n{ex}", true);
                    _todaysDuskTime = TimeSpan.FromHours(19);
                }

                Output($"Automations for {DateTime.Today.ToShortDateString()}:");
                foreach (var automation in Configuration.DaemonAutomations)
                {
                    Output($"\t{automation.RealTime(_todaysDuskTime).ToString(_timeSpanFormat)} - {automation.Name}");
                }
            }

            TimeSpan waitTime;
            ElapsedEventHandler timerElapsedHandler = async (sender, e) => await RunNextAutomation();

            bool test = false; //Test mode will run through all your automations, waiting 5 seconds between each one
            if (test) 
            {
                Output("RUNNING IN TEST MODE!");
                if (_nextAutomation == null)
                {
                    _nextAutomation = Configuration.DaemonAutomations.First();
                }
                else
                {
                    var automationArray = Configuration.DaemonAutomations.ToArray();
                    int index = Array.IndexOf(automationArray, _nextAutomation);
                    if (automationArray.Length > index + 1)
                    {
                        _nextAutomation = automationArray[index + 1];
                    }
                    else
                    {
                        Output("Test Finished");
                        return;
                    }
                }
                waitTime = TimeSpan.FromSeconds(5);
                Output($"Waiting { waitTime.TotalSeconds } seconds");
            }
            else
            {
                TimeSpan currentTime = DateTime.Now.TimeOfDay;
                _nextAutomation = Configuration.DaemonAutomations.FirstOrDefault(x => x.RealTime(_todaysDuskTime) > currentTime);
                if (_nextAutomation != null)
                {
                    TimeSpan nextAutomationTime = _nextAutomation.RealTime(_todaysDuskTime);
                    waitTime = nextAutomationTime - currentTime;

                    //Catch an error where waitTime < 0 for some unknown reason. I've used currentTime as a tentative fix but in case
                    //that doesn't work, handle this gracefully
                    if (waitTime < TimeSpan.Zero)
                    {
                        Output("Wait time is < 0 which is just wrong. Restarting", true);
                        _ = WaitForNextAutomation(true);
                        return;
                    }

                    Output($"Waiting until {nextAutomationTime.ToString(_timeSpanFormat)} for {_nextAutomation.Name}");
                }
                else
                {
                    //No more automations for the day. Wait till just after midnight
                    waitTime = TimeSpan.FromDays(1) - currentTime + TimeSpan.FromMinutes(15);
                    timerElapsedHandler = async (sender, e) => await WaitForNextAutomation(true);
                    Output($"No more automations today. Waiting until 00:15 to restart for tomorrow.");

                    SendErrorOrCheckInEmail();
                }
            }

            _waitTimer = new Timer(waitTime.TotalMilliseconds);
            _waitTimer.Elapsed += timerElapsedHandler;
            _waitTimer.AutoReset = false;
            _waitTimer.Start();
        }

        private async Task RunNextAutomation()
        {
            Output($"Running automation {_nextAutomation.Name}");

            bool anyPhoneHome = false;
            if (_nextAutomation.StateChanges.Any(x => !x.EvenIfHome))
            {
                Output("\tGetting phone locations");

                anyPhoneHome = await IsAnyPhoneHome();
            }

            foreach (var stateChange in _nextAutomation.StateChanges)
            {
                if (!stateChange.EvenIfHome && anyPhoneHome)
                {
                    Output($"\tskipped: {stateChange}");
                    continue;
                }

                try
                {
                    await stateChange.Run(_devices, _api);
                }
                catch (Exception ex)
                {
                    Output($"\tFailed to run {stateChange}. Exception:\r\n{ex}", true);
                    
                    if (_automationRetryCount < _automationRetryLimit)
                    {
                        _automationRetryCount++;

                        Output($"\tRetrying in 5 seconds (attempt {_automationRetryCount} of {_automationRetryLimit})", true);

                        //Wait briefly
                        await Task.Delay(5000);

                        //Retry entire automation
                        await RunNextAutomation();
                    }
                    else
                    {
                        //Restart
                        Output("\tRetry limit reached. Restarting...", true);
                        _ = WaitForNextAutomation(true);
                    }

                    return;
                }

                _automationRetryCount = 0;

                Output($"\tRAN: {stateChange}");
            }

            Output("\tAutomation Complete");

            _ = WaitForNextAutomation(false);
        }

        private void SendErrorOrCheckInEmail()
        {
            bool sendEmail = false;
            string subject = null;
            string body = null;
            if (_errorStringBuilder.Length > 0)
            {
                //Send daily email containing errors
                sendEmail = true;
                subject = "Lightwave Daemon Errors";
                body = _errorStringBuilder.ToString();
            }
            else if (DateTime.Now.DayOfWeek == DayOfWeek.Friday) //Send weekly check-in email if not sending an error email
            {
                sendEmail = true;
                subject = "Lightwave Daemon Check-in";
                body = "Just a weekly check-in to let you know that the Daemon is happy :)";
            }

            if (sendEmail)
            {
                if (!Helpers.SendEmail(
                        Configuration.EmailFromAddress,
                        Configuration.EmailToAddress,
                        subject,
                        body,
                        null,
                        out Exception ex))
                {
                    Output($"Failed to send email. Exception:\r\n{ex}", true);
                }
                else
                {
                    _errorStringBuilder.Clear();
                }
            }
        }

        private async Task<bool> IsAnyPhoneHome(bool manuallyTriggered = false)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (var personTextBlock in _personTextBlocks)
                {
                    personTextBlock.Value.Text = $"{personTextBlock.Key.PersonName}: Refreshing";
                }
            });

            IEnumerable<(string Name, string IP)> connectedDevices = null;
            try
            {
                await Task.Run(async () => //Don't lock the UI
                {
                    connectedDevices = await VirginRouter.GetWifiConnectedDevices(Configuration.RouterIP, Configuration.RouterPassword);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    foreach (var personTextBlock in _personTextBlocks)
                    {
                        personTextBlock.Value.Text = $"{personTextBlock.Key.PersonName}: Failed";
                    }
                });

                string errorMessage = $"\tFailed to get phone locations, so daemon will assume phones are away. Exception:\r\n{ex}";
                if (manuallyTriggered)
                {
                    //Also show message box so exception details can be seen
                    MessageBox.Show(errorMessage, null);
                }
                else
                {
                    Output(errorMessage, true);
                }

                return false;
            }

            bool anyPhoneHome = false;
            foreach (var phone in Configuration.Phones)
            {
                bool home;
                if (phone.IP != null)
                {
                    home = connectedDevices.Any(x => x.IP == phone.IP);
                }
                else
                {
                    home = connectedDevices.Any(x => x.Name == phone.PhoneName);
                }

                string location = home ? "Home" : "Away";
                string message = $"{phone.PersonName}: {location}";

                Dispatcher.Invoke(() =>
                {
                    _personTextBlocks[phone].Text = message;
                });

                if (!manuallyTriggered)
                {
                    Output($"\t\t{message}");
                }

                anyPhoneHome |= home;
            }
            return anyPhoneHome;
        }

        private async void BtnRefreshPhoneLocations_Click(object sender, RoutedEventArgs e)
        {
            await IsAnyPhoneHome(true);
        }

        private void Output(string text, bool isError = false)
        {
            text = $"{ DateTime.Now.ToString("G") }: {text}";

            Dispatcher.Invoke(() =>
            {
                tbOutput.Text += text + "\r\n";
                tbOutput.ScrollToEnd();
            });

            _logger.Log(text, false);

            if (isError)
            {
                _errorStringBuilder.AppendLine(text + "\r\n");
            }
        }
    }
}
