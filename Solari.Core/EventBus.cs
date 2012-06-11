using System;
using System.Data;
using System.Collections;
using System.Reflection;

namespace Solari.Core {

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
	public class ObserverAttribute: Attribute {
		protected System.Type _type;

		public ObserverAttribute(System.Type type) {
			_type = type;
		}

		public System.Type ObservedType() {
			return _type;
		}
	}

	public interface IObserver {
		void Notify(ModelBase triggered, string eventType, object[] data);
	}

	public class EventBus {
		private Hashtable _observers = new Hashtable();
		private static EventBus _instance = null;
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(EventBus));
		private EventBus() {}

		public static EventBus Instance {
			get {
				lock (typeof(EventBus)) {
					if (_instance == null) {
						_instance = new EventBus();
						_instance.Init();
					}
				}
				return _instance;
			}
		}

		private void Init() {
			Logger.Info("Inizializzazione Event Bus");
			RegisterObservers();
		}

		private void RegisterObservers() {
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				// TODO: non tutte le dll ma solo quelle in una certa cartella?
				foreach (Type type in assembly.GetTypes()) {
					foreach (Attribute a in type.GetCustomAttributes(typeof(ObserverAttribute), true)) {
						ObserverAttribute la = (ObserverAttribute)a;
						// TODO: verificare che la classe implementi IObserver
						AddToObservers(la.ObservedType(), type);
					}
				}
			}
		}

		private void AddToObservers(System.Type observed, System.Type observer) {
			Logger.Debug(string.Format("Classe {0} registrata come osservatore per la classe {1}",
				observer.FullName, observed.FullName));
			ArrayList observers = (ArrayList)_observers[observed];
			if (observers == null)
				_observers[observed] = observers = new ArrayList();
			observers.Add(Activator.CreateInstance(observer));
		}

		public void Raise(ModelBase triggered, string eventType, params object[] data) {
			System.Type t = ModelHelper.Instance.GetBase(triggered.GetType());
			ArrayList observers = (ArrayList)_observers[t];
			if (observers != null)
				foreach (IObserver observer in observers)
					observer.Notify(triggered, eventType, data);
		}
	}
}
