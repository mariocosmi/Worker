using System;
using System.Text;
using Solari.Core;
using NanoScript;

namespace Worker.Units {
	[Plugin("Select")]
	class Select : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = false;
			if (!this.ContainsKey("__DATA__")) {
				var text = this.GetString("content");
				this["__DATA__"] = Utils.List(sf, text, input);
			}
			var vett = this.GetArray("__DATA__");
			var ret = new Map();
			var idx = -1;
			if (vett != null) {
				idx = this.GetInt("__IDX__", 0);
				this["__IDX__"] = idx + 1;
				if (idx < vett.Count)
					ret = vett[idx] as Map;
			}
			hasMore = vett != null && idx < vett.Count - 1;
			cut = false;
			return ret;
		}
	}
}
