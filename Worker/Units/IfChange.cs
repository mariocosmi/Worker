using System;
using System.Collections;
using System.Text;
using Solari.Core;

namespace Worker.Units {
	[Plugin("IfChange")]
	class IfChange : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = false;
			var fields = this.GetString("fields").Split(',');
			var saved = this.GetMap("__SAVED__");
			var changed = false;
			cut = !changed;
			// TODO: dove salvo???
			return input;
		}

		private Map SelectValues(Map input, string values) {
			var ret = new Map();
			foreach (var k in values.Split(','))
				ret.Add(k, input[k]);
			return ret;
		}
	}
}
