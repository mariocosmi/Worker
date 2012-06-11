using System;
using System.Net;
using System.Net.Mail;
using Solari.Core;

namespace Worker.Units {
	[Plugin("Mail")]
	class Mail : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = false;
			cut = false;
			string testo;
			if (VelocityHelper.RenderStringTemplateToString(this.GetString("content"), input, out testo) && testo.Length > 0)
				SendMail(input, cfg.Data);

			return input;
		}

		private void SendMail(Map dati, Map config) {
			if (dati.GetString("smtpServer") == string.Empty)
				return;
			// tratto da http://blogs.ugidotnet.org/PietroLibroBlog/archive/2008/08/06/93635.aspx
			var message = new MailMessage();
			message.From = new MailAddress(dati.GetString("mittente"));
			message.To.Add(dati.GetString("destinatari"));
			message.Subject = dati.GetString("oggetto");
			message.IsBodyHtml = true;
			message.Body = dati.GetString("testo");
			var smtpClient = new SmtpClient(config.GetString("smtpServer"), config.GetInt("smtpPort", 25));
			if (config.ContainsKey("smtpUser")) {
				smtpClient.UseDefaultCredentials = false;
				smtpClient.Credentials = new NetworkCredential(config.GetString("smtpUser"), config.GetString("smtpPassword"));
			}
			smtpClient.Send(message);
		}

	}
}
