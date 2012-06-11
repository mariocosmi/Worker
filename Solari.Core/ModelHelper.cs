using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using System.Reflection;

namespace Solari.Core {

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class OverrideAttribute: Attribute {
		protected System.Type _type;

		public OverrideAttribute(System.Type type) {
			_type = type;
		}

		public System.Type OverridenType() {
			return _type;
		}
	}

	public class ModelHelper {
		private Hashtable _overrides = new Hashtable();
		private Hashtable _inverse = new Hashtable();
		private static ModelHelper _instance = null;
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ModelHelper));
		private ModelHelper() {}

		public static ModelHelper Instance {
			get {
				lock (typeof(ModelHelper)) {
					if (_instance == null) {
						_instance = new ModelHelper();
						_instance.Init();
					}
				}
				return _instance;
			}
		}

		private void Init() {
			Logger.Info("Inizializzazione ModelHelper");
			RegisterOverrides();
		}

		private void RegisterOverrides() {
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				// TODO: non tutte le dll ma solo quelle in una certa cartella?
				foreach (Type type in assembly.GetTypes()) {
					foreach (Attribute a in type.GetCustomAttributes(typeof(OverrideAttribute), true)) {
						OverrideAttribute la = (OverrideAttribute)a;
						// TODO: verificare che la classe implementi ModelBase
						AddToOverrides(la.OverridenType(), type);
					}
				}
			}
		}

		private void AddToOverrides(System.Type overriden, System.Type overrider) {
			Logger.Debug(string.Format("Classe {0} registrata come override per la classe {1}",
				overrider.FullName, overriden.FullName));
			_overrides[overriden] =  overrider;
			_inverse[overrider] = overriden;
		}

		public System.Type GetOverrider(System.Type t) {
			return _overrides.ContainsKey(t) ? (System.Type)_overrides[t]: t;
		}

		public System.Type GetBase(System.Type t) {
			return _inverse.ContainsKey(t) ? (System.Type)_inverse[t]: t;
		}

		public ModelBase New(System.Type t) {
			return (ModelBase)Activator.CreateInstance(GetOverrider(t));
		}

		public ModelBase New(System.Type t, IDataRecord rec) {
			return (ModelBase)Activator.CreateInstance(GetOverrider(t), new object[] { rec});
		}

		public ModelBase New(System.Type t, NameValueCollection coll) {
			return (ModelBase)Activator.CreateInstance(GetOverrider(t), new object[] { coll });
		}

		public string ShortName(System.Type t) {
			return GetBase(t).Name;
		}

		public FinderBase Finder(System.Type t) {
			t = GetOverrider(t);
			return (FinderBase)PluginHelper.Instance.CreateInstance(t.Assembly.GetName().Name, t.FullName + "Finder", new object[] {t});
		}

		public FinderBase Finder(System.Type t, ServerFacade sf) {
			t = GetOverrider(t);
			return (FinderBase)PluginHelper.Instance.CreateInstance(t.Assembly.GetName().Name, t.FullName + "Finder", new object[] { t, sf });
		}

	}
}
