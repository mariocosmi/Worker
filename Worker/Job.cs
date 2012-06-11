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
		private List<Job> _jobs = null;

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
						var job = new Job(_logger, jobId);
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
						job.Execute(sf, cfg);
					sf.Commit();
				} catch (Exception e) {
					if (sf.Transaction != null)
						sf.Rollback();
					_logger.Log("Errore in onTimer", e);
				}
			}
		}
	}

	public class Job {
		private readonly MyLogger _logger;
		private readonly string _id;
		public Job(MyLogger logger, string id) {
			_logger = logger;
			_id = id;
		}
		private ArrayList _units = null;

		public void Load(XmlElement el) {
			_units = new ArrayList();
			var list = el.SelectNodes("unit");
			if (list != null)
				foreach (XmlNode n in list) {
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
