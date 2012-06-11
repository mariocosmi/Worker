using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using System.Reflection;

namespace Solari.Core {
	public enum FilterResult { Accept, Discard, Stop };
	public interface IModelFilter {
		FilterResult Filter(ArrayList vett, Map model, Map fieldNames);
		Map Definition();
		bool IsNull();
		int Count();
		int Id();
		string Descr();
		void LazyLoad();
	}

	public class FinderBase: MarshalByRefObject {
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ModelBase));
		protected System.Type _baseType = typeof(ModelBase);
		protected ServerFacade _sf = null;
		public FinderBase(System.Type t) {
			_baseType = t;
		}
		public FinderBase(System.Type t, ServerFacade sf) {
			_baseType = t;
			_sf = sf;
		}

		public virtual IDataReader GetReader(string query, Map map) {
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), query, map):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
			return rdr;
		}

		public virtual int ExecuteSql(string query, Map map) {
			if (_sf == null)
				return ServerFacade.ExecuteNonQueryQs(ModelHelper.Instance.ShortName(_baseType),query, map);
			else
				return _sf.InstanceExecuteNonQuery(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
		}


		#region Read
		public virtual ModelBase Read(object key) {
			IDataReader rdr = _sf == null ?
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), "read", key) :
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), "read"), key);
			using (rdr)
				if (rdr.Read())
					return ModelHelper.Instance.New(_baseType, rdr);
			return null;
		}
		public virtual ModelBase Read(string query, Map map) {
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), query, map):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
			using (rdr)
				if (rdr.Read())
					return ModelHelper.Instance.New(_baseType, rdr);
			return null;
		}

		public virtual ModelBase Read(string query, System.Type t, Map map) {
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), query, map):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
			using (rdr)
				if (rdr.Read())
					return ModelHelper.Instance.New(t, rdr);
			return null;
		}
		#endregion

		#region List
		public virtual ArrayList List(string pattern) {
			ArrayList vett = new ArrayList();
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), "list", pattern + "%"):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), "list"), pattern + "%");
			using (rdr)
				FillArray(vett, rdr, _baseType);
			return vett;
		}

		public virtual ArrayList List(string query, Map map) {
			ArrayList vett = new ArrayList();
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), query, map):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
			using (rdr)
				FillArray(vett, rdr, _baseType);
			return vett;
		}

		public virtual ArrayList List(string query, System.Type t, Map map) {
			ArrayList vett = new ArrayList();
			IDataReader rdr = _sf == null ? 
				ServerFacade.ExecuteReaderQs(ModelHelper.Instance.ShortName(_baseType), query, map):
				_sf.InstanceExecuteReader(_sf.GetSQLFromQueryStore(ModelHelper.Instance.ShortName(_baseType), query), map);
			using (rdr)
				FillArray(vett, rdr, t);
			return vett;
		}
		#endregion

		#region FillArray
		public void FillArray(ArrayList vett, IDataReader rdr, System.Type type) {
			FillArray(vett, rdr, type, null, null);
		}

		private void FillArray(ArrayList vett, IDataReader rdr, System.Type type, IModelFilter modelFilter, Map fieldNames) {
			while (rdr != null && rdr.Read()) {
				ModelBase instance = ModelHelper.Instance.New(type, rdr);
				FilterResult stat = (modelFilter == null) ? FilterResult.Accept : modelFilter.Filter(vett, instance, fieldNames);
				if (stat == FilterResult.Accept)
					vett.Add(instance);
				else if (stat == FilterResult.Stop)
					break;
			}
			Logger.DebugFormat("{0} carica {1} records", this.GetType().Name, vett.Count);
		}
		#endregion

		public virtual ModelBase Init() {
			ModelBase m = ModelHelper.Instance.New(_baseType);
			m.Inizializza();
			return m;
		}
	}
}
