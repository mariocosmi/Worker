using System;
using Solari.Core;

namespace Worker {
	public abstract class UnitOfWork: Map {
		public abstract Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut);
		public string Label { get { return this.ContainsKey("label") ? this.GetString("Label") : this.GetString("type"); } }
	}
}
