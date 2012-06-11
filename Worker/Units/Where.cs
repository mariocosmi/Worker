using System;
using System.Text;
using Solari.Core;
using NanoScript;

namespace Worker.Units {
	[Plugin("Where")]
	class Where : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = false;
			var text = this.GetString("content");
			var script = new ScriptEngine();
			script.References("Solari.Core.dll");
			script.Using("Solari.Core");
			script.DeclareGlobal("sf", typeof(ServerFacade), sf);
			script.DeclareGlobal("cfg", typeof(Map), cfg.Data);
			script.DeclareGlobal("context", typeof(Map), this);
			try {
				script.SetCode("public bool Filter(Map input) { " + text + " ; return false; }");
				script.Compile();
				cut = !(bool)script.Execute("Filter", input);
				return input;
			} catch (Exception ex) {
				throw new ScriptingException(ex);
			}
		}
	}
}
