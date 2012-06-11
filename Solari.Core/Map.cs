using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Web;
using System.Reflection;
using System.IO;
using System.Xml;
using Nii.JSON;

namespace Solari.Core {
	public class Map: Hashtable {
		public Map (params object[] data) {
			for (int i = 0; i < data.Length - 1; i = i + 2) {
				if (data[i + 1] != null) 
					this[data[i]] = data[i + 1];
				else
					this.Remove(data[i]);
			}
		}

		public Map Copy(Map m) {
			if (m != null)
				foreach (object k in m.Keys)
					this[k] = m[k];
			return this;
		}

		public Map AddMany(params object[] data) {
			for (int i = 0; i < data.Length - 1; i = i + 2) {
				if (data[i + 1] != null) 
					this[data[i]] = data[i + 1];
				else
					this.Remove(data[i]);
			}
			return this;
		}

		public override void Add(object key, object value) {
			base.Add(key.ToString().ToLower(), value);
		}

		public override object this[object key] {
			get { return base[key.ToString().ToLower()]; }
			set { base[key.ToString().ToLower()] = value; }
		}

		public override bool ContainsKey(object key) {
			return base.ContainsKey(key.ToString().ToLower());
		}

		public override void Remove(object key) {
			base.Remove(key.ToString().ToLower());
		}

		public object h(string key) {
			return this.h(key, "");
		}

		public object h(string key, string hint) {
			Object prop = this[key.ToLower()];
			if (prop == null)
				return "";
			if (prop is Map)
				return prop;
			if (hint == "jd" && prop is long)
				return DbHelper.FromJsonDate(Convert.ToInt64(prop)).ToString(PersonalSettings.Provider.DateFormat);
			if (hint == "jdt" && prop is long)
				return DbHelper.FromJsonDate(Convert.ToInt64(prop)).ToString(PersonalSettings.Provider.DateTimeFormat);
			if (hint == "dt?" && !(prop is DateTime)) {
				DateTime dt = DbHelper.FromJsonDate(Convert.ToInt64(prop));
				string ret = dt.ToString(dt.Date == DateTime.Today ? PersonalSettings.Provider.TimeFormat : PersonalSettings.Provider.DateTimeFormat);
				return ret;
			}
			if (prop is DateTime) {
				DateTime dt = (DateTime)prop;
				if (hint == "dt")  
					return dt.ToString(PersonalSettings.Provider.DateTimeFormat);
				else if (hint == "t")
					return dt.ToString(PersonalSettings.Provider.TimeFormat);
				else if (hint == "dt?")
					return dt.ToString(dt.Date == DateTime.Today ? PersonalSettings.Provider.TimeFormat: PersonalSettings.Provider.DateTimeFormat);
				else if (hint == "hm")
					return dt.ToString("HH:mm");
				else if (hint == "dte")
					return HttpUtility.UrlPathEncode(dt.ToString(PersonalSettings.Provider.DateTimeFormat));
				else if (hint == "dz")
					return DbHelper.ToZulianDate(dt);
				else if (hint == "tz")
					return DbHelper.ToZulianTime(dt);
				else if (hint == "json")
					return DbHelper.ToJsonDate(dt).ToString();
				else if (hint == "jd")
					return dt.ToString(PersonalSettings.Provider.DateFormat);
				else if (hint == "jdtt")
					return dt.ToString(PersonalSettings.Provider.DateTimeFormat);
				else if (hint == "")
					return dt.ToString(PersonalSettings.Provider.DateFormat);
				else {
					return dt.ToString(hint);
				}
			}
			if (hint == "enc")
				return HttpUtility.HtmlEncode(Convert.ToString(prop));
			if (hint == "esc")
				return Convert.ToString(prop).Replace("'", "\\'");
			if (hint == "json")
				return JSONUtils.Enquote(Convert.ToString(prop));
			return Convert.ToString(prop);
		}

		protected virtual bool CanSerialize(object key) {
			return true;
		}

		public string TranslateString(string name) {
			if (this.ContainsKey(name))
				return Convert.ToString(this[name]);
			else
				return name;
		}

		#region Interrogazioni

		private object GetObject(string path) {
			object tmp = this;
			foreach (string part in path.Split('.'))
				if (tmp is Map)
					tmp = ((Map)tmp)[part];
				else if (tmp is ArrayList)
					tmp = ((ArrayList)tmp)[Convert.ToInt32(part)];
				else
					return null;
			return tmp;
		}

		public string GetString(string path) {
			return Convert.ToString(GetObject(path));
		}

		public int GetInt(string path) {
			return this.GetInt(path, 0);
		}

		public int GetSafeInt(string path) {
			try {
				return this.GetInt(path, 0);
			} catch (Exception) {
				return 0;
			}
		}

		public int GetInt(string path, int defVal) {
			object val = GetObject(path);
			if (val == null || Convert.ToString(val) == "") return defVal;
			return Convert.ToInt32(val);
		}

		public long GetLong(string path) {
			object val = GetObject(path);
			if (val == null || Convert.ToString(val) == "") return 0;
			return Convert.ToInt64(val);
		}

		public float GetFloat(string path) {
			object val = GetObject(path);
			if (val == null || Convert.ToString(val) == "") return 0;
			return Convert.ToSingle(val);
		}

		public DateTime GetDateTime(string path) {
			object val = GetObject(path);
			if (Convert.ToString(val) == "NOW" || val == null || Convert.ToString(val) == "") return DateTime.Now;
			return Convert.ToDateTime(val);
		}

		public ArrayList GetArray(string path) {
			return GetObject(path) as ArrayList;
		}

		public Map GetMap(string path) {
			return GetObject(path) as Map;
		}

		#endregion

		#region ToJson
		public string ToJson() {
			return this.ToJson(false);
		}

		private string ToJson(bool flat) {
			StringBuilder sb = new StringBuilder();
			sb.Append('{');
			object el = null;
			foreach (object key in this.Keys) {
				if (!this.CanSerialize(key))
					continue;
				if (el != null)
					sb.Append(',');
				el = this[key];
				if (el == null)
					continue;
				string keyName = Convert.ToString(key);
				string keyValue = "null";
				if (el is string)
					keyValue = JSONUtils.Enquote((string)el);
				else if (el is int || el is long)
					keyValue = JSONUtils.Enquote(el.ToString());
				else if (el is float || el is double || el is Decimal)
					keyValue = JSONUtils.Enquote(el.ToString());
				else if (el is bool)
					keyValue = el.ToString().ToLower();
				else if (el is DateTime) {
					DateTime dtObj = (DateTime)el;
					long n = DbHelper.ToJsonDate(dtObj);
					keyValue = n.ToString();
				}
				else if (el is ArrayList) {
					ArrayList al = (ArrayList)el;
					StringBuilder sb2 = new StringBuilder();
					sb2.Append('[');
					object el2 = null;
					for (int i = 0; i < al.Count; i++) {
						if (el2 != null)
							sb2.Append(',');
						el2 = al[i];
						if (el2 != null && el2 is Map)
							sb2.Append(flat ? "{...}" : ((Map)el2).ToJson(flat));
						else if (el2 != null && el2 is string)
							sb2.Append(JSONUtils.Enquote((string)el2));
						else if (el2 != null && el2 is int)
							sb2.Append(JSONUtils.Enquote(el2.ToString()));
						else
							el2 = null;
					}
					sb2.Append(']');
					keyValue = sb2.ToString();
				}
				else if (el is Map)
					keyValue = flat? "{...}": ((Map)el).ToJson(flat);
				sb.Append(JSONUtils.Enquote(keyName))
					.Append(':')
					.Append(keyValue);
			}
			sb.Append('}');
			return sb.ToString();
		}

		public string ToShortJson() {
			try {
				string buff = this.ToJson(true);
				return buff.Length > 1000 ? buff.Substring(0, 1000) : buff;
			} catch (Exception) {
			}
			return "";
		}

		public static Map FromJsonObject(string str) {
			if (str == "")
				return new Map();
			JSONObject root = new JSONObject(str);
			return FromJson(root);
		}

		public static ArrayList FromJsonArray(string str) {
			if (str == "")
				return new ArrayList();
			JSONArray root = new JSONArray(str);
			return FromJson(root);
		}

		private static Map FromJson(JSONObject root) {
			Map ret = new Map();
			foreach (string k in root.getDictionary().Keys) {
				object val = root[k];
				if (val is JSONArray) {
					JSONArray aval = (JSONArray)val;
					for (int i = 0; i < aval.Count; i++)
						ret[k] = Map.FromJson(aval);
				}
				else if (val is JSONObject) {
					JSONObject oval = (JSONObject)val;
					ret[k] = Map.FromJson(oval);
				}
				else if (k.Length > 3 && k.Substring(0, 3) == "_dt") {
					ret[k.Substring(3)] = DbHelper.FromJsonDate(Convert.ToInt64(val));
				}
				else {
					ret[k] = val;
				}
			}
			return ret;
		}

		private static ArrayList FromJson(JSONArray vett) {
			ArrayList ret = new ArrayList();
			for (int i = 0; i < vett.Count; i++) {
				if (vett[i] is string) {
					string sval = vett.getString(i);
					ret.Add(sval);
				}
				else if (vett[i] is int) {
					int ival = vett.getInt(i);
					ret.Add(ival);
				}
				else if (vett[i] is JSONObject) {
					JSONObject oval = vett.getJSONObject(i);
					ret.Add(Map.FromJson(oval));
				}
				else if (vett[i] is JSONArray) {
					JSONArray aval = vett.getJSONArray(i);
					ret.Add(Map.FromJson(aval));
				}
			}
               
			return ret;
		}

/*		private static ArrayList FromJson(JSONArray ret) {
			ArrayList ret = new ArrayList();
			for (int i = 0; i < ret.Count; i++) {
				try {
					string sval = ret.getString(i);
					ret.Add(sval);
				} catch (Exception) {
					try {
						int ival = ret.getInt(i);
						ret.Add(ival);
					} catch (Exception) {
						try {
							JSONObject oval = ret.getJSONObject(i);
							ret.Add(Map.FromJson(oval));
						} catch (Exception) {
							JSONArray aval = ret.getJSONArray(i);
							ret.Add(Map.FromJson(aval));
						}
					}
				}
			}
			return ret;
		}
*/
		#endregion

		#region WriteToXml
		public void WriteToXml(TextWriter wr) {
			XmlTextWriter xwr = new XmlTextWriter(wr);
			xwr.Formatting = Formatting.Indented;
			xwr.Indentation = 4;
			xwr.WriteStartDocument(true);
			xwr.WriteStartElement("Result");
			this.WriteToXml(xwr);
			xwr.WriteEndElement();
			xwr.WriteEndDocument();
		}

		public void WriteToXml(XmlTextWriter xwr) {
			object el = null;
			foreach (string key in this.Keys) {
				if (!this.CanSerialize(key))
					continue;
				el = this[key];
				if (el == null)
					continue;
				if (el is string)
					xwr.WriteElementString(XmlConvert.EncodeName(key), (string)el);
				else if (el is Decimal)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((Decimal)el));
				else if (el is int)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((int)el));
				else if (el is long)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((long)el));
				else if (el is float)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((float)el));
				else if (el is double)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((double)el));
				else if (el is bool)
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString((bool)el));
				else if (el is DateTime) {
					DateTime dtObj = (DateTime)el;
					long n = DbHelper.ToJsonDate(dtObj);
					xwr.WriteElementString(XmlConvert.EncodeName(key), XmlConvert.ToString(n));
				}
				else if (el is ArrayList) {
					ArrayList al = (ArrayList)el;
					xwr.WriteStartElement(key.ToString());
					object el2 = null;
					for (int i = 0; i < al.Count; i++) {
						el2 = al[i];
						if (el2 != null && el2 is Map) {
							xwr.WriteStartElement(ModelHelper.Instance.GetBase(el2.GetType()).Name);
							((Map)el2).WriteToXml(xwr);
							xwr.WriteEndElement();
						}
					}
					xwr.WriteEndElement();
				}
				else if (el is Map) {
					xwr.WriteStartElement(key.ToString());
					((Map)el).WriteToXml(xwr);
					xwr.WriteEndElement();
				}
			}
		}
		#endregion

		#region FromXml
		public static Map FromXmlObject(string buff) {
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(buff);
			return Map.FromXmlObject(doc.DocumentElement);
		}

		private static Map FromXmlObject(XmlElement root) {
			Map ret = new Map();
			ret[root.Name] = Map.FromXmlObject(root.ChildNodes).Copy(Map.FromXmlObject(root.Attributes));
			return ret;
		}

		private static Map FromXmlObject(XmlNodeList nodes) {
			Map ret = new Map();
			foreach (XmlNode n in nodes) {
				if (n is XmlElement) {
					XmlElement el = (XmlElement)n;
					ret[el.Name] = Map.FromXmlObject(el.ChildNodes).Copy(Map.FromXmlObject(el.Attributes));
				}
			}
			return ret;
		}

		private static Map FromXmlObject(XmlAttributeCollection nodes) {
			Map ret = new Map();
			foreach (XmlNode n in nodes) {
				if (n is XmlAttribute) {
					XmlAttribute a = (XmlAttribute)n;
					ret[a.Name] = a.Value;
				}
			}
			return ret;
		}
		#endregion

		public override string ToString() {
			return this.ToJson();
		}

		public static Map FromHashtable(Hashtable h) {
			Map ret = new Map();
			foreach (object k in h.Keys)
				ret.Add(k, h[k]);
			return ret;
		}
	}

	public class Set: Map {
		public void Add(object element) {
			this[element] = element;
		}
	}

	public class MultiMap {
		private Map _map = new Map();
		public void Add(object key, object value) {
			ArrayList vett = (ArrayList)_map[key];
			if (vett == null) {
				vett = new ArrayList();
				_map[key] = vett;
			}
			vett.Add(value);
		}
		public ArrayList Get(object key) {
			return (ArrayList)_map[key];
		}
		public bool ContainsKey(object key) {
			return _map.ContainsKey(key);
		}
		public void Remove(object key, object value) {
			ArrayList vett = (ArrayList)_map[key];
			if (vett != null)
				vett.Remove(value);
		}
	}


	
}
