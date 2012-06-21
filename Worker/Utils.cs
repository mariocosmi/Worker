using System;
using System.Collections;
using System.Drawing;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using Solari.Core;

namespace Worker {
	class Utils {
		public static MyLogger MyLogger;

		public static string GetApplicationFolder() {
			var appPath = Assembly.GetExecutingAssembly().Location;
			return Path.GetDirectoryName(appPath);
		}

		public static string GetMainFolder() {
			return Path.GetFullPath(Path.Combine(GetApplicationFolder(), ".."));
		}

		public static void CheckDatabase(string dbt, string dsn, out bool ok, out string message) {
			ServerFacade sf = null;
			ok = false;
			message = "Controllo database fallisce";
			try {
				sf = ServerFacade.Create(dbt, dsn);
				if (sf.Connection == null)
					return;
				message = "Controllo database OK";
				ok = true;
			} catch (Exception e) {
				MyLogger.Log(string.Format("Errore durante la verifica del db {0} {1}: {2}", dbt, dsn, e.Message), true);
				message = message + ":" + e.Message;
			} finally {
				if (sf != null)
					sf.Dispose();
			}
		}

		public static object ReadValue(IDataReader rdr, object valdef) {
			using (rdr)
				if (rdr != null && rdr.Read())
					return rdr[0];
			return valdef;
		}

		public static Map Read(ServerFacade sf, string query, Map parms) {
			using (var rdr = sf.InstanceExecuteReader(query, parms)) {
				if (rdr != null && rdr.Read()) {
					var ret = new Map();
					DbHelper.FillHashtable(rdr, ret);
					return ret;
				}
			}
			return null;
		}

		public static ArrayList List(ServerFacade sf, string query, Map parms) {
			var ret = new ArrayList();
			using (var rdr = sf.InstanceExecuteReader(query, parms)) {
				while (rdr != null && rdr.Read()) {
					var map = new Map();
					DbHelper.FillHashtable(rdr, map);
					ret.Add(map);
				}
			}
			return ret;
		}

		public static string GetFileContentAsBase64String(string path) {
			byte[] buff;
			using (var stm = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				stm.Position = 0;
				buff = new byte[stm.Length];
				using (var br = new BinaryReader(stm))
					br.Read(buff, 0, Convert.ToInt32(stm.Length));
			}
			return Convert.ToBase64String(buff);
		}

		public static void WriteString64ToFile(string path, string data64) {
			using (var bw = new BinaryWriter(File.Create(path, 0x1000, FileOptions.WriteThrough), Encoding.UTF8))
				bw.Write(Convert.FromBase64String(data64));
		}

		public static void WriteBytesToFile(string path, byte[] buff) {
			using (var bw = new BinaryWriter(File.Create(path, 0x1000, FileOptions.WriteThrough), Encoding.UTF8))
				bw.Write(buff);
		}

		public static string GetImageDataAsBase64String(Image img) {
			byte[] buff;
			using (var stm = new MemoryStream()) {
				img.Save(stm, System.Drawing.Imaging.ImageFormat.Jpeg);
				buff = stm.GetBuffer();
			}
			return Convert.ToBase64String(buff);
		}

		public static Image ReadImageFile(string path) {
			var image = Image.FromFile(path);
			var bmp = new Bitmap(image);
			image.Dispose();
			return bmp;
		}

		private static Image ResizeImage(Image img, double factor) {
			return new Bitmap(img, Convert.ToInt32(img.Width * factor), Convert.ToInt32(img.Height * factor));
		}

		public static Image ResizeImage(Image img, int maxW, int maxH) {
			var ratiox = Convert.ToDouble(img.Width) / Convert.ToDouble(maxW);
			var ratioy = Convert.ToDouble(img.Height) / Convert.ToDouble(maxH);
			return ResizeImage(img, Math.Min(ratiox, ratioy));
		}

		public static Map FillMap(ArrayList vett, string keyField) {
			var ret = new Map();
			foreach (Map m in vett)
				ret[m.GetString(keyField)] = m;
			return ret;
		}

		public static string Join(ArrayList vett, string field, char separator, bool unique) {
			var sb = new StringBuilder();
			var values = new Set();
			for (var i = 0; i < vett.Count; i++) {
				var k = ((Map) (vett[i])).GetString(field);
				if (unique && values.ContainsKey(k)) continue;
				values.Add(k);
				sb.Append(k);
				if (i < vett.Count - 1)
					sb.Append(separator);
			}
			return sb.ToString();
		}

		public static ArrayList SelectValues(Map input, string path) {
			var vett = input.GetArray(path);
			if (vett != null)
				return vett;
			var map = input.GetMap(path);
			return map != null ? new ArrayList(map.Values) : null;
		}

		private static bool IsIdentifier(char ch) {
			return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_';
		}

		public static string EvalExpression(string expression, Map vars) {
			int pos1, pos2;
			for (pos1 = expression.IndexOf('@'); pos1 != -1; pos1 = expression.IndexOf('@', pos1)) {
				for (pos2 = pos1 + 1; pos2 < expression.Length; pos2++)
					if (!IsIdentifier(expression[pos2]))
						break;
				string var = expression.Substring(pos1, pos2 - pos1);
				expression = expression.Replace(var, vars.GetString(var.Substring(1)));
			}
			return expression;
		}

	}
}
