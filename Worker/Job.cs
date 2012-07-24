using System;
using System.Xml;
using Solari.Core;
using System.Collections;
using System.Collections.Generic;

namespace Worker {
	public class Jobs {
		private readonly MyLogger _logger;
		public Jobs(MyLogger logger) {
			_logger = logger;
		}
		private List<Job> _jobs;

		public void Load(string path) {
			_jobs = new List<Job>();
			var doc = new XmlDocument();
			doc.Load(path);
			var list = doc.SelectNodes("/jobs/job");
			if (list != null)
				foreach (XmlNode n in list) {
					if (n is XmlElement) {
						var el = n as XmlElement;
						var jobId = el.GetAttribute("id");
						var fromTime = ParseTime(el.GetAttribute("fromtime"));
						var toTime = ParseTime(el.GetAttribute("totime"));
						var job = new Job(_logger, jobId, fromTime, toTime);
						job.Load(el);
						_jobs.Add(job);
					}
				}
		}

		public void Execute(ConfigHelper cfg) {
			using (var sf = ServerFacade.Create(cfg.ModelloDatabase, cfg.StringaDiConnessione)) {
				try {
					sf.BeginTransaction();
					foreach (var job in _jobs)
						if (job.CanExecute)
							job.Execute(sf, cfg);
					sf.Commit();
				} catch (Exception e) {
					if (sf.Transaction != null)
						sf.Rollback();
					_logger.Log("Errore in onTimer", e);
				}
			}
		}

		private TimeSpan ParseTime(string str) {
			TimeSpan ret;
			return TimeSpan.TryParse(str, out ret) ? ret : TimeSpan.Zero;
		}
	}

	public class Job {
		private readonly MyLogger _logger;
		private readonly string _id;
		private readonly TimeSpan _fromTime;
		private readonly TimeSpan _toTime;

		public Job(MyLogger logger, string id, TimeSpan fromTime, TimeSpan toTime) {
			_logger = logger;
			_id = id;
			_fromTime = fromTime == TimeSpan.Zero? new TimeSpan(0, 0, 0): fromTime;
			_toTime = toTime == TimeSpan.Zero ? new TimeSpan(23, 59, 59) : toTime;
		}
		private ArrayList _units;

		public bool CanExecute {
			get { return DateTime.Now.TimeOfDay > _fromTime && DateTime.Now.TimeOfDay < _toTime; }
		}

		public void Load(XmlElement el) {
			_units = new ArrayList();
			var list = el.SelectNodes("unit");
			if (list != null)
				foreach (XmlNode n in list)
					if (n.Attributes != null) {
						var map = new Map();
						foreach (XmlAttribute a in n.Attributes)
							map[a.Name] = a.Value;
						if (n is XmlElement)
							map["content"] = (n as XmlElement).InnerText;
						_units.Add(map);
					}
		}

		public void Execute(ServerFacade sf, ConfigHelper cfg) {
			_logger.Log(string.Format("Inizia l'esecuzione di {0} {1} units", _id, _units.Count));
			if (_units.Count == 0)
				return;
			_logger.Debug(PrettyPrinter.PrettyPrint(_units));
			try {
				InternalExecute(_units, new Map(), sf, cfg);
			} catch (Exception e) {
				_logger.Log("Esecuzione interrotta", e);
			}
			_logger.Log(string.Format("Terminata l'esecuzione di {0} {1} units", _id, _units.Count));
		}

		private void InternalExecute(ArrayList units, Map input, ServerFacade sf, ConfigHelper cfg) {
			if (units.Count == 0) return;
			var unit = PluginHelper.GetUnitOfWork(units[0] as Map);
			_logger.Log(string.Format("Inizia l'esecuzione della unit {0}", unit.Label));
			_logger.Debug(PrettyPrinter.PrettyPrint(input));
			bool more;
			do {
				bool cut;
				var tempData = Map.FromJsonObject(input.ToJson());
				tempData = ExecuteUnit(unit, tempData, sf, cfg, out more, out cut);
				if (!cut)
					InternalExecute(new ArrayList(units.GetRange(1, units.Count - 1)), tempData, sf, cfg);
				if (more)
					_logger.Log(string.Format("Continua l'esecuzione della unit {0}", unit.Label));
			} while (more);
		}

		private Map ExecuteUnit(UnitOfWork unit, Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			try {
				return unit.Execute(input, sf, cfg, out hasMore, out cut);
			} catch (Exception ex) {
				_logger.Log(string.Format("Errore nell'esecuzione della unit {0}", unit.Label), ex);
				throw;
			}
		}
	}
}
