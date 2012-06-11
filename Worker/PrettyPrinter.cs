using System;
using System.Collections;
using Solari.Core;
using System.Text;
using Nii.JSON;

namespace Worker {
	class PrettyPrinter {
		public static string PrettyPrint(Map map) {
			return map == null ? "NULL-MAP" : new InternalPrettyPrinter().DoPrettyPrint(map);
		}

		public static string PrettyPrint(ArrayList vett) {
			return vett == null ? "NULL-ARRAY" : new InternalPrettyPrinter().DoPrettyPrint(vett);
		}

		private class InternalPrettyPrinter {
			public string DoPrettyPrint(Map map) {
				StringBuilder sb = new StringBuilder();
				_newLine = true;
				sb.AppendLine("{");
				DoPrettyPrint(map, sb, 1);
				sb.AppendLine("}");
				return sb.ToString();
			}

			public string DoPrettyPrint(ArrayList vett) {
				StringBuilder sb = new StringBuilder();
				_newLine = true;
				sb.AppendLine("[");
				DoPrettyPrint(vett, sb, 1);
				sb.AppendLine("]");
				return sb.ToString();
			}

			private bool _newLine;

			private void DoPrettyPrint(Map map, StringBuilder sb, int indent) {
				ArrayList keyArray = new ArrayList(map.Keys);
				keyArray.Sort();
				for (int i = 0; i < keyArray.Count; i++) {
					string key = keyArray[i] as string;
					object val = map[key];
					if (i > 0) {
						sb.AppendLine(",");
						_newLine = true;
					}
					sb.Append(' ', indent * 4).Append('\"').Append(key).Append('\"').Append(':');
					_newLine = false;
					PrettyPrintValue(val, sb, indent);
				}
			}

			public void DoPrettyPrint(ArrayList vett, StringBuilder sb, int indent) {
				for (int i = 0; i < vett.Count; i++) {
					if (i > 0) {
						sb.AppendLine(",");
						_newLine = true;
					}
					PrettyPrintValue(vett[i], sb, indent);
				}
			}

			private void PrettyPrintValue(object val, StringBuilder sb, int indent) {
				if (_newLine)
					sb.Append(' ', indent * 4);
				if (val is Map) {
					sb.AppendLine("{");
					_newLine = true;
					DoPrettyPrint(val as Map, sb, indent + 1);
					sb.Append("}");
				} else if (val is ArrayList) {
					sb.AppendLine("[");
					_newLine = true;
					DoPrettyPrint(val as ArrayList, sb, indent + 1);
					sb.Append("]");
				} else if (val is DateTime)
					sb.Append('\"').Append(((DateTime)val).ToString("dd.MM.yyyy HH:mm:ss")).Append('\"');
				else
					sb.Append(JSONUtils.Enquote(val.ToString()));
				_newLine = false;
			}
		}
	}
}
