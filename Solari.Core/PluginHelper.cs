using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using System.IO;
using System.Reflection;

public class PluginHelper {
	private static Hashtable _mapPlugins = new Hashtable();
	static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(PluginHelper));
	private static PluginHelper _instance = null;

	private PluginHelper() {}

	public static void Initialize(string path) {
		Logger.Info("Inizializzazione PluginHelper");
		LoadPlugins(path);
	}

	public static PluginHelper Instance {
		get {
			lock (typeof(PluginHelper)) {
				if (_instance == null) {
					_instance = new PluginHelper();
				}
			}
			return _instance;
		}
	}

	private static void LoadPlugins(string path) {
		if (Directory.Exists(path))
			foreach (string dllPath in Directory.GetFileSystemEntries(path, "*.dll"))
				try {
					Assembly assembly = Assembly.LoadFrom(dllPath);
					_mapPlugins[assembly.GetName().Name] = assembly;
					Logger.Info("Caricato plugin " + dllPath);
				}
				catch (Exception e) {
					Logger.Warn("Errore nel caricamento del plugin " + dllPath, e);
				}
	}

	private static Hashtable _cacheFinder = new Hashtable();
	private bool GetFromCache(string key, out System.Type val) {
		lock(this) {
			if (_cacheFinder.ContainsKey(key)) {
				val = (System.Type)_cacheFinder[key];
				return true;
			}
			val = null;
			return false;
		}
	}

	private void PutInCache(string key, System.Type val) {
		lock(this) {
			_cacheFinder[key] = val;
		}
	}

	public object CreateInstance(string assemblyName, string FullTypeName, object[] args) {
		System.Type tf;
		if (GetFromCache(FullTypeName, out tf))
			return Activator.CreateInstance(tf, args);

		object ret;
		if (_mapPlugins.ContainsKey(assemblyName))
			ret = ((Assembly)_mapPlugins[assemblyName]).CreateInstance(
				FullTypeName,
				false,
				BindingFlags.Default,
				null,
				args,
				null,
				null);
		else
			ret = Activator.CreateInstance(
				assemblyName,
				FullTypeName,
				false,
				BindingFlags.Default,
				null,
				args,
				null,
				null,
				null
				).Unwrap();
		PutInCache(FullTypeName, ret.GetType());
		return ret;
	}
}
