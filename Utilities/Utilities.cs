using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Windows;
using System.Windows.Media;

namespace Utilities
{
    public static class Helpers
    {
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static bool SendEmail(string from, string to, string subject, string body, SmtpClient smtpClient, out Exception exception)
        {
            exception = null;

            MailMessage message = new MailMessage();
            message.IsBodyHtml = true;
            message.From = new System.Net.Mail.MailAddress(from);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = body?.Replace("\r\n", "<br />");

            bool localSmtpClient = false;
            if (smtpClient == null)
            {
                localSmtpClient = true;

                //Use 587 instead of 465: https://stackoverflow.com/a/20252948/2385956
                smtpClient = new SmtpClient("ADDRESS", 587);
                smtpClient.Credentials = new System.Net.NetworkCredential("USERNAME", "PASSWORD");
                smtpClient.EnableSsl = true;
            }

            try
            {
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
            finally
            {
                if (localSmtpClient)
                {
                    smtpClient.Dispose();
                }
            }

            return true;
        }
    }
}
