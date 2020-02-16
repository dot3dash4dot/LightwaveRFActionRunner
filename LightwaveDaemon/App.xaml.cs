using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LightwaveDaemon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Utilities.Helpers.SendEmail(
                    Configuration.EmailFromAddress,
                    Configuration.EmailToAddress,
                    "Uh-oh! Lightwave Daemon has crashed!",
                    "",
                    null,
                    out _);
            }
            catch
            { }

            MessageBox.Show(e.Exception.ToString());
        }
    }
}
