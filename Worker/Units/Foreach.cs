using System;
using System.Collections;
using Solari.Core;

namespace Worker.Units {
	[Plugin("Foreach")]
	class Foreach: UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			var vett = Utils.SelectValues(input, this.GetString("path"));
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
