using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FlowLog
{
    public static class MailUtil
    {
        public static string SendSimple(string host, int port, string fromName, string fromAddr,
            IEnumerable<string> toAddrs, string subject, string body)
        {
            try
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress(fromName, fromAddr));
                foreach (var to in toAddrs) msg.To.Add(MailboxAddress.Parse(to));
                msg.Subject = subject;
                msg.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                smtp.Connect(host, port, SecureSocketOptions.StartTlsWhenAvailable);
                smtp.Send(msg);
                smtp.Disconnect(true);
                return "";
            }
            catch (Exception ex) { return ex.Message; }
        }
    }
}