using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Solari.Core;
using System.Collections;

namespace Worker {
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class PluginAttribute : Attribute {
		protected string _name;

		public PluginAttribute(string name) {
			_name = name;
		}

		public string Name { get { return _name; } }
	}

	class PluginHelper {
		#region Inizializzazione
		private static Map _mapPlugins = new Map();
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(PluginHelper));

		private PluginHelper() { }

		public static void Initialize() {
			Logger.Info("Inizializzazione PluginHelper");
			_mapPlugins.Clear();
			lock (typeof(PluginHelper)) {
				LoadPluginDll();
			}
		}

		private static void LoadPluginDll() {
			var core = Assembly.GetCallingAssembly();
			LoadAllPlugins(core);
		}

		private static void LoadAllPlugins(Assembly assembly) {
			try {
				foreach (var type in assembly.GetTypes())
					foreach (Attribute a in type.GetCustomAttributes(typeof(PluginAttribute), true)) {
						var pa = a as PluginAttribute;
						_mapPlugins[pa.Name] = new Map("name", pa.Name, "assembly", assembly, "type", type, "attribute", a);
						Logger.InfoFormat("Caricato plugin {0} = {1}", pa.Name, type.FullName);
					}
			} catch (Exception excp) {
				if (excp is System.Reflection.ReflectionTypeLoadException) {
					var typeLoadException = excp as ReflectionTypeLoadException;
					var loaderExceptions = typeLoadException.LoaderExceptions;
					foreach(var aexcp in loaderExceptions)
						Logger.Error("Errori in loadAllPlugins (multipli) ", aexcp);
				} else
					Logger.Error("Errore in loadAllPlugins", excp);
				throw;
			}
		}
		#endregion

		public static UnitOfWork GetUnitOfWork(Map map) {
			var cached = map["cached"] as UnitOfWork;
			if (cached != null) {
				Logger.InfoFormat("Trova in cache la UnitOfWork id {0}", cached.Uid);
				return cached;
			}
			var def = _mapPlugins.GetMap(map.GetString("type"));
			if (def == null) {
				Logger.WarnFormat("Tipo unit sconosciuto {0}", map.GetString("type"));
				return null;
			}
			cached = (UnitOfWork)(Activator.CreateInstance(def["type"] as Type) as UnitOfWork).Copy(map);
			map["cached"] = cached;
			Logger.InfoFormat("Mette in cache la UnitOfWork id {0} tipo {1}", cached.Uid, map.GetString("type"));
			return cached;
		}

		public static Map Plugins { get { return _mapPlugins; } }

	}
}
