using System;
using System.Collections;

namespace Solari.Core {
	public class ReportHelper {
		private class Comparer: IComparer {
			private string[] _columns;
			public Comparer(string[] columns) {
				_columns = columns;
			}
			public int Compare(object x, object y) {
				Map mx = (Map)x;
				Map my = (Map)y;
				foreach (string col in _columns) {
					string k = col;
					object vx = "";
					object vy = "";
					if (col.Length > 2 && col[0] == '_' && col[1] == 'd') {
						k = col.Substring(2);
						vx = mx.h(k, "dt");
						vx = (Convert.ToString(vx) == "") ? new DateTime(1900, 1, 1): Convert.ToDateTime(vx);
						vy = my.h(k, "dt");
						vy = (Convert.ToString(vy) == "") ? new DateTime(1900, 1, 1): Convert.ToDateTime(vy);
					}
					else {
						vx = (mx[k] == null) ? "": mx[k];
						vy = (my[k] == null) ? "": my[k];
					}
					if (vx is String && !(vy is String))
						vy = Convert.ToString(vy);
					else if (vy is String && !(vx is String))
						vx = Convert.ToString(vx);
					int n = ((IComparable)vx).CompareTo(vy);
					if (n != 0)
						return n;
				}
				return 0;
			}
		}

		public ReportHelper() {}

		public static void Sort(ArrayList vett, string column) {
			Sort(vett, new string[] { column });
		}

		public static void Sort(ArrayList vett, string[] columns) {
			vett.Sort(new Comparer(columns));
		}

		private static void SplitHints(string[] columns, string[] hints) {
			for (int i = 0; i < columns.Length; i++) {
				hints[i] = "";
				string col = columns[i];
				if (col.Length > 2 && col[0] == '_')
					if (col[1] == 'd') {
						columns[i] = col.Substring(2);
						hints[i] = "d";
					}
			}
		}

		public static void Group(ArrayList vett, string column) {
			Group(vett, new string[] { column });
		}

		public static void Group(ArrayList vett, string[] columns) {
			int n = columns.Length;
			string[] hints = new string[n];
			string[] old = new string[n];
			SplitHints(columns, hints);
			for (int i = 0; i < n; i++)
				old[i] = "$$valore impossibile$$";
			foreach (Map m in vett) 
				for (int i = 0; i < n; i++)
					if (Convert.ToString(m.h(columns[i], hints[i])) != old[i]) {
						m[string.Format("break_{0}", columns[i])] = true;
						m[string.Format("break_level{0}", i)] = true;
						m[string.Format("break_value{0}", i)] = Convert.ToString(m.h(columns[i], hints[i]));
						for (int j = i; j < n; j++)
							old[j] = Convert.ToString(m.h(columns[j], hints[j]));
						break;
					}
		}

		public static ArrayList Count(ArrayList vett, string column) {
			ArrayList ret = new ArrayList();
			int tot = 0;
			Map m = null;
			string old = "$$valore impossibile$$";
			for (int i = 0; i < vett.Count; i++) {
				m = (Map)vett[i];
				if (Convert.ToString(m.h(column)) != old) {
					if (i != 0) {
						ret.Add(new Map(column, old, "total", tot));
						tot = 0;
					}
					old = Convert.ToString(m.h(column));
				}
				tot++;
			}
			if (m != null)
				ret.Add(new Map(column, old, "total", tot));
			return ret;
		}
	}
}
