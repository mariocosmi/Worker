using System;
using System.IO;
using System.Net;

namespace Solari.Core {
	public class MiniClient {
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(MiniClient));
		string _baseUrl;
		int _timeout = 30000;
		public MiniClient(string baseUrl) { 
			if (!baseUrl.StartsWith("http://"))
				baseUrl = "http://" + baseUrl;
			_baseUrl = baseUrl;
		}

		public MiniClient(string baseUrl, int timeout): this(baseUrl) {
			_timeout = timeout;
		}

		#region Get e Post
		public string Get(string path) {
			Logger.DebugFormat("GET {0}", _baseUrl + path);
			HttpWebRequest webRequest = WebRequest.Create(_baseUrl + path) as HttpWebRequest;
			webRequest.Method = "GET";
			webRequest.Timeout = _timeout;
			webRequest.AllowAutoRedirect = true;
			CookieContainer cookieContainer = new CookieContainer();
			webRequest.CookieContainer = cookieContainer;
			webRequest.Accept = "text/json";
			using (TextReader rdr = new StreamReader(webRequest.GetResponse().GetResponseStream()))
				return rdr.ReadToEnd();
		}

		public byte[] GetBinary(string path) {
			Logger.DebugFormat("GET {0}", _baseUrl + path);
			HttpWebRequest webRequest = WebRequest.Create(_baseUrl + path) as HttpWebRequest;
			webRequest.Method = "GET";
			webRequest.Timeout = _timeout;
			webRequest.AllowAutoRedirect = true;
			CookieContainer cookieContainer = new CookieContainer();
			webRequest.CookieContainer = cookieContainer;
			webRequest.Accept = "text/json";
			Stream stm = webRequest.GetResponse().GetResponseStream();
			int len = (int)webRequest.GetResponse().ContentLength;
			byte[] buff = new byte[len];
			for (int i = 0; i < len; i++)
				buff[i] = (byte)stm.ReadByte();
			stm.Close();
			return buff;
		}

		public string Post(string path, string body) {
			Logger.DebugFormat("POST {0} {1} chars", _baseUrl + path, body.Length);
			HttpWebRequest webRequest = WebRequest.Create(_baseUrl + path) as HttpWebRequest;
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.Timeout = _timeout;
			webRequest.Accept = "text/json";
			// invio dati
			StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
			requestWriter.Write(body);
			requestWriter.Close();
			// lettura risposta
			using (TextReader rdr = new StreamReader(webRequest.GetResponse().GetResponseStream()))
				return rdr.ReadToEnd();
		}

		public string Post(string path, byte[] body) {
			Logger.DebugFormat("POST {0} {1} bytes", _baseUrl + path, body.Length);
			HttpWebRequest webRequest = WebRequest.Create(_baseUrl + path) as HttpWebRequest;
			webRequest.Method = "POST";
			webRequest.ContentType = "multipart/form-data";
			webRequest.Timeout = _timeout;
			webRequest.Accept = "text/json";
			// invio dati
			Stream stm = webRequest.GetRequestStream();
			stm.Write(body, 0, body.Length);
			stm.Close();
			// lettura risposta
			using (TextReader rdr = new StreamReader(webRequest.GetResponse().GetResponseStream()))
				return rdr.ReadToEnd();
		}

		public void AsyncPost(string path, string body) {
			Logger.DebugFormat("ASYNC POST {0} {1} chars", _baseUrl + path, body.Length);
			HttpWebRequest webRequest = WebRequest.Create(_baseUrl + path) as HttpWebRequest;
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.Timeout = _timeout;
			webRequest.Accept = "text/json";
			// invio dati
			StreamWriter requestWriter = new StreamWriter(webRequest.GetRequestStream());
			requestWriter.Write(body);
			requestWriter.Close();
			// avvio su altro thread e lettura risposta 
			webRequest.BeginGetResponse(new AsyncCallback(this.PostCallback), webRequest);
		}

		void PostCallback(IAsyncResult ret) {
			WebRequest webRequest = (WebRequest)ret.AsyncState;
			Logger.DebugFormat("ASYNC POST {0} ritorna {1}", webRequest.RequestUri.ToString(), ret);
		}

		#endregion
	}
}
