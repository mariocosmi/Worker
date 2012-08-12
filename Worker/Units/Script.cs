using System;
using System.Text;
using Solari.Core;
using NanoScript;

namespace Worker.Units {
	[Plugin("Script")]
	class Script : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = cut = false;
			var text = this.GetString("content");
			var cached = this["cached"] as ScriptEngine;
			if (cached == null) {
				System.Diagnostics.Debug.WriteLine("Compila script per " + this.Uid);
				var script = new ScriptEngine();
				script.References("Solari.Core.dll");
				script.Using("Solari.Core");
				script.DeclareGlobal("sf", typeof (ServerFacade), sf);
				script.DeclareGlobal("cfg", typeof (Map), cfg.Data);
				script.DeclareGlobal("context", typeof (Map), this);
				script.DeclareGlobal("hasMore", typeof (bool), false);
				try {
					script.SetCode("public Map DoExecute(Map input) { hasMore = false; " + text + " ; return input; }");
					script.Compile();
					this["cached"] = script;
					var ret = script.Execute("DoExecute", input) as Map;
					hasMore = (bool) script.GlobalGet("hasMore");
					return ret;
				}
				catch (Exception ex) {
					throw new ScriptingException(ex);
				}
			} else {
				System.Diagnostics.Debug.WriteLine("Trova in cache script per " + this.Uid);
				var script = cached;
				try {
					var ret = script.Execute("DoExecute", input) as Map;
					hasMore = (bool)script.GlobalGet("hasMore");
					return ret;
				} catch (Exception ex) {
					throw new ScriptingException(ex);
				}
			}
		}
	}

	public class ScriptingException : Exception {
		private Exception _e;
		public ScriptingException(Exception e) { _e = e; }

		public override string Message {
			get { return this.GetMessage(_e); }
		}

		private string GetMessage(Exception e) {
			if (e is ScriptEngine.ScriptingContextException) {
				var ex = e as ScriptEngine.ScriptingContextException;
				if (ex.errors != null) {
					var b = new StringBuilder();
					var cc = ex.errors;
					b.AppendLine("There were errors:");
					foreach (var err in cc) {
						b.AppendLine("Error: " + err.errNumber + " " + err.text);
					}
					return b.ToString();
				}
				return ex.Message + "\r\n";
			}
			while (e.InnerException != null)
				e = e.InnerException;
			return e.Message + "\r\n";
		}
	}
}
