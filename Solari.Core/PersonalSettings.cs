using System;
using System.Web;

namespace Solari.Core {
	public interface IPersonalSettings {
		string SessionKey { get; }
		string DateTimeFormat { get; }
		string DateFormat { get; }
		string TimeFormat { get; }
		object ViewEngine { get; }
	}

	public class PersonalSettings {
		private PersonalSettings() {}
		public static IPersonalSettings Provider {
			get {
				return HttpContext.Current == null? null: (IPersonalSettings)HttpContext.Current.Items["PersonalSettingsProvider"];
			}
			set {
				HttpContext.Current.Items["PersonalSettingsProvider"] = value;
			}
		}
	}
}
