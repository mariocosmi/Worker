using System;
using Solari.Core;

namespace Worker {
	public abstract class UnitOfWork: Map {
		private static int _id = 0;
		public UnitOfWork(): base() {
			_id++;
			this["uid"] = _id;
		}
		public abstract Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut);
		public string Label { get { return this.ContainsKey("label") ? this.GetString("Label") : this.GetString("type"); } }
		public string Uid { get { return this.GetString("uid"); } }
	}
}
