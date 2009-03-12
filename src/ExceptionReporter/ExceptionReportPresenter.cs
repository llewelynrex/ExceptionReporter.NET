using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ExceptionReporter.Config;
using ExceptionReporter.Core;
using ExceptionReporter.Mail;
using ExceptionReporter.SystemInfo;

namespace ExceptionReporter
{
	/// <summary>
	/// The Presenter in this MVP (Model-View-Presenter) implementation 
	/// </summary>
	public class ExceptionReportPresenter
    {
        private IClipboard _clipboard;
        private readonly IExceptionReportView _view;
        private readonly ExceptionReportGenerator _reportGenerator;

		/// <summary>
		/// 
		/// </summary>
        public ExceptionReportPresenter(IExceptionReportView view, ExceptionReportInfo info)
        {
            _view = view;
            ReportInfo = info;
            _reportGenerator = new ExceptionReportGenerator(ReportInfo);
        }

		/// <summary>
		/// The application assembly which is (the main application) using the exception reporter assembly
		/// </summary>
        public Assembly AppAssembly
        {
            get { return ReportInfo.AppAssembly; }
        }

        public ExceptionReportInfo ReportInfo { get; private set; }

		/// <summary>
		/// Clipboard, the setter is currently only used in testing/mocking
		/// </summary>
        public IClipboard Clipboard
        {
            set { _clipboard = value; }
        }

        private ExceptionReport CreateExceptionReport()
        {
            ReportInfo.UserExplanation = _view.UserExplanation;
            return _reportGenerator.CreateExceptionReport();
        }

		/// <summary>
		/// Save the exception report to file/disk
		/// </summary>
		/// <param name="fileName">the filename to save</param>
        public void SaveReportToFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            ExceptionReport exceptionReport = CreateExceptionReport();

            try
            {
                using (FileStream stream = File.OpenWrite(fileName))
                {
                    var writer = new StreamWriter(stream);
                    writer.Write(exceptionReport);
                    writer.Flush();
                }
            }
            catch (Exception exception)
            {
                _view.ShowErrorDialog(string.Format("Unable to save file '{0}'", fileName), exception);
            }
        }

		/// <summary>
		/// Send the exception report via email, using the configured email method/type
		/// </summary>
		/// <param name="handle">The handle of the window to use in sending the email</param>
        public void SendReportByEmail(IntPtr handle)
        {
            if (ReportInfo.MailMethod == ExceptionReportInfo.EmailMethod.SimpleMAPI)
            {
                SendMapiEmail(handle);
            }

            if (ReportInfo.MailMethod == ExceptionReportInfo.EmailMethod.SMTP)
            {
                SendSmtpMail();
            }
        }

		/// <summary>
		/// copy the report to the clipboard, using the clipboard implementation supplied
		/// </summary>
        public void CopyReportToClipboard()
        {
            ExceptionReport exceptionReport = CreateExceptionReport();
            _clipboard.CopyTo(exceptionReport.ToString());
            _view.ProgressMessage = string.Format("{0} copied to clipboard", ReportInfo.TitleText);
        }

		/// <summary>
		/// toggle the detail between 'simple' (just messsage) and showFullDetail (ie normal)
		/// </summary>
        public void ToggleDetail()
        {
            _view.ShowFullDetail = !_view.ShowFullDetail;
            _view.ToggleShowFullDetail();
        }

        private string BuildEmailExceptionString()
        {
            var stringBuilder = new StringBuilder()
                .AppendLine(
                @"The email is ready to be sent. 
					Information about your machine and the exception is included.
					Please feel free to add any relevant information or attach any files.")
                .AppendLine().AppendLine();

            if (ReportInfo.TakeScreenshot)
            {
                stringBuilder.AppendFormat("This email has attached a screenshot taken at the time of the exception.")
                    .AppendLine("If you do not want the screenshot to be sent, you may delete the attachment before sending.")
                    .AppendLine().AppendLine();
            }

            stringBuilder.Append(CreateExceptionReport());

            return stringBuilder.ToString();
        }


        private void SendSmtpMail()
        {
            string exceptionString = BuildEmailExceptionString();

            _view.ProgressMessage = "Sending email via SMTP...";
            _view.EnableEmailButton = false;
            _view.ShowProgressBar = true;

            try
            {
                var mailSender = new MailSender(ReportInfo);
                mailSender.SendSmtp(exceptionString, _view.SetEmailCompletedState);
            }
            catch (Exception exception)
            {
                _view.SetEmailCompletedState(false);
                _view.ShowErrorDialog("Unable to send email using SMTP", exception);
            }
        }

        private void SendMapiEmail(IntPtr windowHandle)
        {
            string exceptionString = BuildEmailExceptionString();

            _view.ProgressMessage = "Launching email program...";
            _view.EnableEmailButton = false;

            bool wasSuccessful = false;

            try
            {
                var mailSender = new MailSender(ReportInfo);
                mailSender.SendMapi(exceptionString, windowHandle);
                wasSuccessful = true;
            }
            catch (Exception exception)
            {
                wasSuccessful = false;
                _view.ShowErrorDialog("Unable to send Email using 'Simple MAPI'", exception);
            }
            finally
            {
                _view.SetEmailCompletedState_WithMessageIfSuccess(wasSuccessful, string.Empty);
            }
        }

        private static string GetConfigAsHtml()
        {
            var converter = new ConfigHtmlConverter(Assembly.GetExecutingAssembly());
            return converter.Convert();
        }

		/// <summary>
		/// Get the system information results
		/// </summary>
        public IEnumerable<SysInfoResult> GetSysInfoResults()
        {
            return _reportGenerator.GetOrFetchSysInfoResults();
        }

		/// <summary>
		/// Send email (using ShellExecute) to the configured contact email address
		/// </summary>
        public void SendContactEmail()
        {
            ShellExecute(string.Format("mailto:{0}", ReportInfo.ContactEmail));
        }

		/// <summary>
		/// Navigate to the website configured
		/// </summary>
        public void NavigateToWebsite()
        {
            ShellExecute(ReportInfo.WebUrl);
        }

        private void ShellExecute(string executeString)
        {
            try
            {
                var psi = new ProcessStartInfo(executeString) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception exception)
            {
                _view.ShowErrorDialog(string.Format("Unable to (Shell) Execute '{0}'", executeString), exception);
            }
        }

		/// <summary>
		/// The main entry point, populate the report with everything it needs
		/// </summary>
        public void PopulateReport()
        {
            try
            {
                _view.SetInProgressState();

                _view.PopulateExceptionTab(ReportInfo.Exceptions);
                _view.PopulateAssembliesTab();
                _view.PopulateConfigTab(GetConfigAsHtml());
                _view.PopulateSysInfoTab();
            }
            finally
            {
                _view.SetProgressCompleteState();
            }
        }

		/// <summary>
		/// Close/cleanup
		/// </summary>
        public void Close()
        {
            _reportGenerator.Dispose();
        }
    }
}