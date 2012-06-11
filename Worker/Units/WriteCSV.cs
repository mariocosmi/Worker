using System;
using System.Collections;
using System.IO;
using System.Text;
using Solari.Core;

namespace Worker.Units {
	[Plugin("WriteCSV")]
	class WriteCsv : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = cut = false;
			var fields = this.GetString("fields").Split(',');
			var data = Utils.SelectValues(input, this.GetString("path"));
			if (data != null)
				using (TextWriter tw = new StreamWriter(Utils.EvalExpression(this.GetString("outfile"), input))) {
					foreach (Map map in data)
						WriteCsvMap(tw, map, fields);
				}
			return input;
		}

		private void WriteCsvMap(TextWriter writer, Map map, string[] fields) {
			var sb = new StringBuilder();
			foreach (var field in fields) {
				if (sb.Length > 0)
					sb.Append(',');
				sb.Append(MakeValueCsvFriendly(map[field]));
			}
			writer.WriteLine(sb.ToString());
		}

		private string MakeValueCsvFriendly(object value) {
			if (value == null) return "";

			if (value is DateTime) {
				if (Math.Abs(((DateTime)value).TimeOfDay.TotalSeconds - 0) < 1)
					return ((DateTime)value).ToString("yyyy-MM-dd");
				return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
			}
			string output = value.ToString();

			if (output.Contains(",") || output.Contains("\""))
				output = '"' + output.Replace("\"", "\"\"") + '"';

			return output;
		}
	}
}
