using System;
using System.IO;
using System.Collections;

namespace Solari.Core {
	public class ApplicationError: Exception {
		public static string ModificheFallite = "ModificheFallite.vm";
		public static string AzioneNonAutorizzata = "AzioneNonAutorizzata.vm";
		public static string UtenteNonAutorizzato = "UtenteNonAutorizzato.vm";
		public static string RegistrazioneErrata = "RegistrazioneErrata.vm";

		public ApplicationError(string template): base() {
			_template = template;
			_hint = new Map();
		}

		public ApplicationError(string template, Map hint): base() {
			_template = template;
			_hint = hint;
		}

		protected Map _hint = null;
		public Map Hint {
			get { return _hint; }
		}

		protected string _template = null;
		public string Template {
			get { return _template; }
		}

		public static string FormatException(Exception ex) {
			while (ex.InnerException != null)
				ex = ex.InnerException;
			string template;
			Map hint;
			if (ex is ApplicationError) {
				ApplicationError ea = (ApplicationError)ex;
				template = Path.Combine("Errors", ea.Template);
				hint = ea.Hint;
			}
			else {
				template = Path.Combine("Errors", "Generic.vm");
				hint = new Map("msg", ex.Message);
			}
			return VelocityHelper.RenderTemplateToString(template, hint);
		}
	}
}
