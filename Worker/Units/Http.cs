using System;
using Solari.Core;

namespace Worker.Units {
	[Plugin("Http")]
	class Http: UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			var mc = new MiniClient(Utils.EvalExpression(this.GetString("content"), input).Trim());
			var data = mc.Get("");
			hasMore = cut = false;
			return Map.FromJsonObject(data);
		}
	}
}
