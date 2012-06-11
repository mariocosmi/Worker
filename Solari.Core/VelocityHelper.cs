using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Web;
using Nii.JSON;

namespace Solari.Core {
	public class VelocityHelper {
		#region Metodi static
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(VelocityHelper));
		private static string MASTER_TEMPLATE = "master.vm";
		private static string _defaultPath = "";

		public static void SetDefaultPath(string path) {
			_defaultPath = path;
		}

		public static void RenderTemplateToWriter(string templateFile, TextWriter writer, NVelocity.VelocityContext vc) {
			NVelocity.App.VelocityEngine engine = null;
			IPersonalSettings provider = PersonalSettings.Provider;
			if (provider == null) {
				Commons.Collections.ExtendedProperties props = new Commons.Collections.ExtendedProperties();
				props.SetProperty(NVelocity.Runtime.RuntimeConstants.RESOURCE_LOADER, "file");
				props.SetProperty(NVelocity.Runtime.RuntimeConstants.FILE_RESOURCE_LOADER_PATH, _defaultPath);
				props.SetProperty(NVelocity.Runtime.RuntimeConstants.FILE_RESOURCE_LOADER_CACHE, "true");
				engine = new NVelocity.App.VelocityEngine(props);
			}
			else
				engine = (NVelocity.App.VelocityEngine)PersonalSettings.Provider.ViewEngine;
			NVelocity.Template template = engine.GetTemplate(templateFile);
			template.Merge(vc, writer);
		}

		public static string RenderTemplateToString(string templateFile, Map map) {
			NVelocity.VelocityContext vc = new NVelocity.VelocityContext(map);
			using (StringWriter contentWriter = new StringWriter()) {
				VelocityHelper.RenderTemplateToWriter(templateFile, contentWriter, vc);
				return contentWriter.ToString();
			}
		}

		public static void DisableBrowserCache() {
			HttpContext.Current.Response.Cache.SetExpires(System.DateTime.Now.AddMinutes(-1));
			HttpContext.Current.Response.Cache.SetCacheability(HttpCacheability.NoCache);
			// TODO: così disabilito la cache al BACK in explorer ma non in firefox
		}

		public static void RenderTemplateToResponse(string templateFile, Map map, bool useMaster) {
			DisableBrowserCache();
			string requestedFormat = HttpContext.Current.Request.QueryString["format"];
			if (requestedFormat != null && requestedFormat.ToUpper() == "JSON") {
				HttpContext.Current.Response.Write(map.ToJson());
				return;
			}
			if (requestedFormat != null && requestedFormat.ToUpper() == "XML") {
				HttpContext.Current.Response.ContentType = "text/xml";
				map.WriteToXml(HttpContext.Current.Response.Output);
				return;
			}
			NVelocity.VelocityContext vc = new NVelocity.VelocityContext(map);
			if (useMaster) {
				vc.Put("childContent", VelocityHelper.RenderTemplateToString(templateFile, map));
				vc.Put("templateFile", templateFile);
				templateFile = VelocityHelper.MASTER_TEMPLATE;
			}
			using (StreamWriter pageWriter = new StreamWriter(HttpContext.Current.Response.OutputStream))
				VelocityHelper.RenderTemplateToWriter(templateFile, pageWriter, vc);
		}

		public static bool RenderStringTemplateToString(string templateString, Map dati, out string ret) {
			NVelocity.App.VelocityEngine engine = new NVelocity.App.VelocityEngine();
			engine.Init();
			NVelocity.VelocityContext vc = new NVelocity.VelocityContext(dati);
			StringBuilder sb = new StringBuilder();
			string logMsg = null;
			bool bOk = false;
			try {
				using (StringWriter wr = new StringWriter(sb))
					using (StringReader rdr = new StringReader(templateString))
						bOk = engine.Evaluate(vc, wr, logMsg, rdr);
				ret = bOk ? sb.ToString() : logMsg;
			} catch (Exception e) {
				Logger.Warn("Errore in RenderStringTemplateToString", e);
				ret = e.Message;
			}
			return bOk;
		}
		#endregion

		#region Metodi di utilità

		public object GetItem(ArrayList vett, int idx) {
			try {
				return vett[idx];
			} catch (Exception) {
				return null;
			}
		}

		#endregion
	}
}
