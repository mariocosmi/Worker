using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.IO;
using AMS.Profile;
using Solari.Core;
using System.Web;

namespace Worker {
	public interface MyLogger {
		void Log(string message);
		void Log(string message, bool important);
		void Log(string message, Exception e);
		void Debug(string message);
	}

	public partial class MainService : ServiceBase, MyLogger {
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(MainService));
		System.Threading.Timer _timer = null;
		ConfigHelper _cfg = null;
		MainForm _mainForm = null;
		int _timerInterval;

		public MainService() {
			this.ServiceName = "Worker";
		}

		public void OnTimer(object state) {
			this.Debug("OnTimer");
			_timer.Dispose();
			try {
				var jobs = new Jobs(this);
				jobs.Load(Path.Combine(Utils.GetApplicationFolder(), "jobs.xml"));
				jobs.Execute(_cfg);
			} catch (Exception ex) {
				this.Log("Errore in onTimer", ex);
			}
			_timer = new Timer(new TimerCallback(this.OnTimer), this, _timerInterval * 1000, Timeout.Infinite);
		}

		#region Logging
		public void InizializzaLogging() {
			var appender = new log4net.Appender.OutputDebugStringAppender {
			                Threshold = log4net.Core.Level.Debug,
			                Layout = new log4net.Layout.PatternLayout("%date{dd-MM-yyyy HH:mm:ss,fff} %5level  [%2thread] %message (%logger{1}:%line)%n")
			               };
			appender.ActivateOptions();
			log4net.Config.BasicConfigurator.Configure(appender);

			this.EventLog.Source = "Worker";
			this.EventLog.Log = "Application";

			if (!EventLog.SourceExists(this.ServiceName))
				EventLog.CreateEventSource(this.ServiceName, this.EventLog.Log);

			Utils.MyLogger = this;
		}

		public void Log(string message) {
			this.Log(message, false);
		}

		public void Log(string message, bool important) {
			if (important) {
				Logger.Warn(message);
				this.EventLog.WriteEntry(message);
			} else
				Logger.Info(message);
			if (_mainForm != null)
				_mainForm.Log(message);
		}

		public void Log(string message, Exception e) {
			this.Log(string.Format("{0}: {1}", message, e.Message), true);
		}

		public void Debug(string message) {
			Logger.Debug(message);
		}

		public MyLogger GetLogger() {
			return this;
		}
		#endregion

		#region Proprietà

		bool _dbAvviato = false;
		string _dbMessage = "Database non configurato";

		public bool DbAvviato {
			get { return _dbAvviato; }
		}

		public string DbMessage {
			get { return _dbMessage; }
		}

		#endregion

		#region Eventi

		protected override void OnStart(string[] args) {
			StartServer();
			base.OnStart(args);
		}

		protected override void OnStop() {
			StopServer();
			base.OnStop();
		}

		#endregion

		#region Operazioni

		public void StartServer() {
			StartServer(null);
		}

		public void StartServer(MainForm frm) {
			_mainForm = frm;
			try {
				log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(Path.Combine(Utils.GetMainFolder(), "log4net.config")));
			} catch (Exception e) {
				this.Log(string.Format("Impossibile accedere alla cartella {0}: {1}", Utils.GetMainFolder(), e.Message), true);
				this.Log("Verificare di avere i permessi di scrittura sulla cartella");
				this.Log("Oppure ripetere l'installazione indicando una cartella diversa");
				return;
			}
			try {
				_cfg = new ConfigHelper(Path.Combine(Utils.GetMainFolder(), "Web.xml"));
				if (_cfg.ModelloDatabase == "") {
					this.Log("Mancano le informazioni per accedere al db", true);
					return;
				}
				Log(_cfg.StringaDiConnessione);
				Utils.CheckDatabase(_cfg.ModelloDatabase, _cfg.StringaDiConnessione, out _dbAvviato, out _dbMessage);
				if (!_dbAvviato) {
					this.Log("Impossibile accedere al db: " + _dbMessage, true);
					return;
				}
				PluginHelper.Initialize();
				_timerInterval = _cfg.MainTimer;
				_timer = new Timer(new TimerCallback(this.OnTimer), this, 0, _timerInterval * 1000);
				this.Log(string.Format("Servizio avviato - connessione a database {0} ok; refresh ogni {1} secondi", _cfg.ModelloDatabase, _cfg.MainTimer), true);
			} catch (Exception e) {
				this.Log("Impossibile avviare il servizio", e);
			}
		}

		public void StopServer() {
			this.Log("Servizio fermato", true);
		}

		#endregion
	}

	public class ConfigHelper {
		private readonly Map _cachedValues = new Map();

		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ConfigHelper));
		private ConfigHelper() { }

		public ConfigHelper(string path) {
			Logger.InfoFormat("Inizializzazione ConfigHelper da {0}", path);
			_cachedValues = new Map();
			var cfg = new Xml(path);
			using (cfg.Buffer()) {
				foreach (string section in cfg.GetSectionNames()) {
					var map = new Map();
					_cachedValues[section] = map;
					foreach (string key in cfg.GetEntryNames(section))
						map[key] = cfg.GetValue(section, key);
				}
				_cachedValues["ModelloDatabase"] = cfg.GetValue("Settings", "ModelloDatabase", ServerFacade.SqlServer);
				_cachedValues["StringaDiConnessione"] = Crypto.FastDecrypt(cfg.GetValue("Settings", "StringaDiConnessione", Crypto.FastEncrypt("")));
				_cachedValues["MainTimer"] = cfg.GetValue("Settings", "MainTimer", 60);
			}
		}

		public string ModelloDatabase { get { return _cachedValues.GetString("ModelloDatabase"); } }
		public string StringaDiConnessione { get { return _cachedValues.GetString("StringaDiConnessione"); } }
		public int MainTimer { get { return _cachedValues.GetInt("MainTimer"); }}

		public Map Data { get { return _cachedValues; } }
	}
}
