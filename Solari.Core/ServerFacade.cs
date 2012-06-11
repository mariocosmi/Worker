using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Xml;
using System.Web;
using System.Web.Caching;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data.OracleClient;
using System.Data.OleDb;

namespace Solari.Core {
	#region class SfCommand
	public class SfCommand {
		private string _originalText;
		private ArrayList _modifiedText = new ArrayList();
		private ArrayList _commandType = new ArrayList();
		private ArrayList _params = new ArrayList();

		public SfCommand(ServerFacade sf, string sql) {
			_originalText = sql;
			if (_originalText != "")
				PreProcess(sf);
		}
		private void PreProcess(ServerFacade sf) {
			foreach (string singleCommand in _originalText.Split(';')) {
				string modified = singleCommand;
				CommandType ct = CommandType.Text;
				string[] parms= sf.PrepareParameters(ref modified, ref ct);
				_modifiedText.Add(modified);
				_commandType.Add(ct);
				_params.Add(parms);
			}
		}

		public int GetCommandCount() {
			return _modifiedText.Count;
		}
		public string GetText(int idx) {
			return (string)_modifiedText[idx];
		}
		public CommandType GetCommandType(int idx) {
			return (CommandType)_commandType[idx];
		}
		public string[] GetParams(int idx) {
			return (string[])_params[idx];
		}
		public bool IsEmpty() {
			return _originalText == "";
		}
	}
	#endregion

	public abstract class ServerFacade: IDisposable {
		public static string Msde = "MSDE";
		public static string SqlServer = "SQLSERVER";
		public static string Oracle = "ORACLE";
		public static string OleDb = "OLEDB";

		#region Creazione
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ServerFacade));
		static string _dsn;
		static string _dbt;
		static string _qsPath;
		static string _commonPath;
		static string _customPath;
		public static void Initialize(string dbt, string dsn, string path, string customPath) {
			_dbt = dbt.ToUpper();
			_dsn = dsn;
			_customPath = customPath;
			_commonPath = Path.Combine(path, "common");
			_qsPath = Path.Combine(path, dbt == ServerFacade.Msde ? ServerFacade.SqlServer : dbt);
			// TODO: la riga sopra può usare la funzione GetDBPath()?
			IDictionaryEnumerator CacheEnum = HttpContext.Current.Cache.GetEnumerator();
			while (CacheEnum.MoveNext()) // spiano l'eventuale cache
				try {
					string cacheItem = ((DictionaryEntry)CacheEnum.Current).Key.ToString(); 
				} catch(Exception) {}
			Logger.Info("Inizializzata ServerFacade tipo " + dbt);
			Logger.Info("DSN = " + dsn);
			Logger.Info("QueryStore = " + path);
		}

		public static ServerFacade Create() { return Create(_dbt, _dsn, null); }
		public static ServerFacade Create(string dbt, string dsn) { return Create(dbt, dsn, null); }
		public static ServerFacade Create(string dbt, string dsn, string rootFolder) {
			if (dbt == ServerFacade.Msde)
				return new MsdeServerFacade(dsn, rootFolder);
			else if (dbt == ServerFacade.SqlServer)
				return new SqlServerServerFacade(dsn, rootFolder);
			else if (dbt == ServerFacade.Oracle)
				return new OracleServerFacade(dsn, rootFolder);
			else if (dbt == ServerFacade.OleDb)
				return new OleDbServerFacade(dsn, rootFolder);
			else throw new Exception("Unknown dbtype: " + dbt);
		}

		public static void Test(string dbt, string dsn) {
			ServerFacade sf = ServerFacade.Create(dbt, dsn);
		}

		IDbConnection _conn = null;
		IDbTransaction _trans = null;
		string _rootFolder = null;

		public IDbConnection Connection {
			get {
				return _conn;
			}
		}

		public IDbTransaction Transaction {
			get {
				return _trans;
			}
		}

		public ServerFacade() {}

		public ServerFacade(string dsn, string rootFolder) {
			if (dsn != "") {
				_conn = CreateConnection(dsn);
				if (_conn.State == ConnectionState.Closed)
					_conn.Open();
				_conn.Close();
			}
			_rootFolder = rootFolder;
		}

		public static ServerFacade Current {
			get {
				return (ServerFacade)HttpContext.Current.Items["ServerFacade"] ;
			}
		}

		public static ServerFacade CreateAndSetCurrent() {
			ServerFacade sf = ServerFacade.Create();
			HttpContext.Current.Items["ServerFacade"] = sf;
			return sf;
		}

		#endregion

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	
		private bool _disposed = false;
		private void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
					if (_trans != null)
						this.Rollback();
					if (_conn != null) {
						if (_conn.State == ConnectionState.Open)
							_conn.Close();
						_conn.Dispose();
					}
					if (HttpContext.Current != null && HttpContext.Current.Items["ServerFacade"] == this)
						HttpContext.Current.Items["ServerFacade"] = null;
				}
			}
			_disposed = true;
		}

		~ServerFacade() {
			Dispose(false);
		}

		#endregion

		#region Transazioni
		public bool InTransaction {
			get { return _trans != null; }
		}

		public void BeginTransaction() {
			if (InTransaction)
				throw new Exception("Already in transaction");
			if (_conn.State != ConnectionState.Open)
				_conn.Open();
			_trans = _conn.BeginTransaction();
			Logger.Info("Transazione iniziata");
		}

		public void Commit() {
			if (!InTransaction)
				throw new Exception("Not in transaction");
			_trans.Commit();
			_trans.Dispose();
			_trans = null;
			Logger.Info("Commit");
		}

		public void Rollback() {
			if (!InTransaction)
				throw new Exception("Not in transaction");
			_trans.Rollback();
			_trans.Dispose();
			_trans = null;
			Logger.Info("Rollback");
		}
		#endregion

		#region Metodi astratti
		public abstract IDbConnection CreateConnection(string dsn);
		public abstract IDataParameter[] CreateParameters(int count);
		public abstract IDataParameter CreateParameter(string nam, Object val);
		public abstract IDataParameter CreateParameter(string nam);
		public abstract void SetParameterValue(IDataParameter param, Object val);
		public abstract string[] PrepareParameters(ref string sql, ref CommandType ct);

		public abstract string DbType { get; }
		public abstract bool UseOracle { get; }
		public abstract bool UseOleDb { get; }
		public abstract bool UseMsSql { get; }
		public abstract bool UseSqlCE { get; }
		public abstract bool UseMsde { get; }
		#endregion

		#region Funzioni di comodità

		public virtual string GetDBPath() {
			return this.DbType;
		}

		public virtual int InstanceGetContatore(string contatore) {
			return this.InstanceExecuteNonQuery("CalcolaContatore(@contatore)", contatore);
		}

		public virtual int InstanceCalcolaProgressivo(string tabella) {
			return this.InstanceExecuteNonQuery("CalcolaProgressivo(@tabella)", tabella);
		}

		private IDataParameter[] FillParameters(string[] parms, Hashtable map) {
			IDataParameter[] ret = CreateParameters(parms.Length);
			for (int i = 0; i < parms.Length; i++) {
				string nam = DbHelper.RemovePrefix(parms[i]);
				object val = map[nam];
				if (nam.ToUpper() == "ADESSO")
					val = DateTime.Now;
				if (nam.ToUpper() == "USERID") {
					if (PersonalSettings.Provider == null)
						val = DBNull.Value;
					else
						val = PersonalSettings.Provider.SessionKey;
				}
				else if (val == null)
					val = DBNull.Value;
				ret[i] = CreateParameter(parms[i]);
				SetParameterValue(ret[i], val);
			}
			return ret;
		}

		private IDataParameter[] FillParameters(string[] parms, object[] paramValues) {
			for (int i = 0; i < parms.Length; i++)
				if (DbHelper.RemovePrefix(parms[i]).ToUpper() == "ADESSO") {
					ArrayList tmp = new ArrayList(paramValues);
					tmp.Insert(i, DateTime.Now);
					paramValues = tmp.ToArray();
				}
				else if (DbHelper.RemovePrefix(parms[i]).ToUpper() == "USERID") {
					ArrayList tmp = new ArrayList(paramValues);
					tmp.Insert(i, PersonalSettings.Provider == null? null: PersonalSettings.Provider.SessionKey);
					paramValues = tmp.ToArray();
				}
			if (parms.Length != paramValues.Length)
				throw new Exception("Lunghezza parametri errata");
			IDataParameter[] ret = CreateParameters(parms.Length);
			for (int i = 0; i < parms.Length; i++) {
				ret[i] = CreateParameter(DbHelper.RemovePrefix(parms[i]), paramValues[i]);
			}
			return ret;
		}
		#endregion

		#region QueryStore

		private static bool GetFromCache(string key, out SfCommand cmd) {
			cmd = HttpContext.Current == null? null: (SfCommand)HttpContext.Current.Cache[key];
			return cmd != null;
		}

		public static void RemovedCallback(String k, Object v, CacheItemRemovedReason r){
			Logger.Info(string.Format("Rimossa chiave {0} dal QueryStore", k));
		}

		private static void PutInCache(string key, SfCommand cmd) {
			if (HttpContext.Current != null)
				HttpContext.Current.Cache.Add(key, cmd, new CacheDependency(new string[] { _customPath, _commonPath, _qsPath }), Cache.NoAbsoluteExpiration, TimeSpan.FromHours(1), CacheItemPriority.Normal, new CacheItemRemovedCallback(RemovedCallback));
		}

		private XmlElement ReadFile(string path, string obj, string query) {
			string fullPath = Path.Combine(path, obj + ".xml");
			if (!File.Exists(fullPath))
				return null;
			XmlDocument doc = new XmlDocument();
			doc.Load(fullPath);
			return (XmlElement)doc.SelectSingleNode("/query/" + query);
		}

		private bool GetFromQueryStore(string obj, string query, out SfCommand cmd) {
			obj = obj.ToLower();
			query = query.ToLower();
			string key = obj + "/" + query;
			string sql = "";

			if (GetFromCache(key, out cmd))
				return cmd != null && !cmd.IsEmpty();

			XmlElement el = ReadFile(_customPath, obj, query);
			bool bCustom = false;
			if (el == null) {
				el = ReadFile(_qsPath, obj, query);
				if (el == null)
					el = ReadFile(_commonPath, obj, query);
			}
			else
				bCustom = true;

			if (el != null)
				sql = el.InnerText;

			cmd = new SfCommand(this, sql);
			PutInCache(key, cmd);
			Logger.Info("Inserita in cache la query " + (bCustom ? "CUSTOM " : "") + "per " + key + " = " + sql); 
			return !cmd.IsEmpty();
		}

		private Map _instanceCache = new Map();
		private string GetSQLFromQueryStore(string root, string obj, string query) {
			string paths = string.Format(@"custom\queryStore,queryStore\{0},queryStore\Common", this.GetDBPath());
			obj = obj.ToLower();
			query = query.ToLower();
			string key = obj + "/" + query;
			string sql = _instanceCache.GetString(key);
			if (sql != "")
				return sql;

			foreach (string part in paths.Split(',')) {
				XmlElement el = ReadFile(Path.Combine(root, part), obj, query);
				if (el != null) {
					_instanceCache[key] = el.InnerText;
					return el.InnerText;
				}
			}

			return "";
		}

		public string GetSQLFromQueryStore(string obj, string query) {
			if (_rootFolder != null)
				return GetSQLFromQueryStore(_rootFolder, obj, query);
			obj = obj.ToLower();
			query = query.ToLower();
			string key = obj + "/" + query;
			string sql = _instanceCache.GetString(key);
			if (sql != "")
				return sql;

			XmlElement el = ReadFile(_customPath, obj, query);
			if (el == null) {
				el = ReadFile(_qsPath, obj, query);
				if (el == null)
					el = ReadFile(_commonPath, obj, query);
			}

			if (el != null) {
				_instanceCache[key] = el.InnerText;
				return el.InnerText;
			}

			return "";
		}
		#endregion

		#region Operazioni static sul database 
		public static IDataReader ExecuteReader(string sql, Map map) {
			return ServerFacade.Current.InstanceExecuteReader(sql, map);
		}

		public static IDataReader ExecuteReader(string sql, params object[] parms) {
			return ServerFacade.Current.InstanceExecuteReader(sql, parms);
		}

		public static int CalcolaProgressivo(string tabella) {
			return ServerFacade.Current.InstanceCalcolaProgressivo(tabella);
		}

		public static int CalcolaNextContatore(string contatore) {
			return ServerFacade.Current.InstanceGetContatore(contatore);
		}

		#endregion

		#region Operazioni sul database con la query cablata
		public IDataReader InstanceExecuteReader(string sql, Map map) {
			Logger.Info("InstanceExecuteReader " + sql); 
			Logger.Debug(map.ToShortJson());
			if (this.Connection == null)
				return null;
			string modifiedSql = sql;
			CommandType ct = CommandType.Text;
			string[] paramNames = this.PrepareParameters(ref modifiedSql, ref ct);
			IDataParameter[] parms = this.FillParameters(paramNames, map);
			if (this.Transaction == null)
				return DbHelper.ExecuteReader(this.Connection, ct, modifiedSql, parms);
			else
				return DbHelper.ExecuteReader(this.Transaction, ct, modifiedSql, parms);
		}

		public IDataReader InstanceExecuteReader(string sql, params object[] parms) {
			string tmp = sql;
			string[] paramNames = (string[])DbHelper.ExtractParametersFromString(ref tmp).ToArray(typeof(string));
			if (paramNames.Length != parms.Length)
				throw new Exception("InstanceExecuteReader " + sql + ": lunghezza parametri errata");
			Map map = new Map();
			for (int i = 0; i < paramNames.Length; i++)
				map[DbHelper.RemovePrefix(paramNames[i])] = parms[i];
			return InstanceExecuteReader(sql, map);
		}
		
		public int InstanceExecuteNonQuery(string sql, Map map) {
			Logger.Info("InstanceExecuteNonQuery " + sql); 
			Logger.Debug(map.ToShortJson());
			ServerFacade sf = this;
			if (sf.Connection == null)
				return 0;
			int ret = -1;
			foreach (string singleSql in sql.Split(';')) {
				string modifiedSql = singleSql;
				CommandType ct = CommandType.Text;
				string[] paramNames = sf.PrepareParameters(ref modifiedSql, ref ct);
				IDataParameter[] parms = sf.FillParameters(paramNames, map);
				if (sf.Transaction == null)
					ret = DbHelper.ExecuteNonQuery(sf.Connection, ct, modifiedSql, parms);
				else
					ret = DbHelper.ExecuteNonQuery(sf.Transaction, ct, modifiedSql, parms);
				foreach (IDataParameter p in parms)
					if (p.Direction == ParameterDirection.ReturnValue)
						ret = Convert.ToInt32(p.Value);
			}
			return ret;
		}

		public int InstanceExecuteNonQuery(string sql, params object[] parms) {
			string[] paramNames = (string[])DbHelper.ExtractParametersFromString(ref sql).ToArray(typeof(string));
			if (paramNames.Length != parms.Length)
				throw new Exception("InstanceExecuteNonQuery " + sql + ": lunghezza parametri errata");
			Map map = new Map();
			for (int i = 0; i < paramNames.Length; i++)
				map[paramNames[i]] = parms[i];
			return InstanceExecuteNonQuery(sql, map);
		}
		
		public static int ExecuteNonQuery(string sql, Map map) {
			Logger.Info("ExecuteNonQuery " + sql); 
			Logger.Debug(map.ToShortJson());
			ServerFacade sf = ServerFacade.Current;
			if (sf.Connection == null)
				return 0;
			string modifiedSql = sql;
			CommandType ct = CommandType.Text;
			string[] paramNames = sf.PrepareParameters(ref modifiedSql, ref ct);
			IDataParameter[] parms = sf.FillParameters(paramNames, map);
			int ret = -1;
			if (sf.Transaction == null)
				ret = DbHelper.ExecuteNonQuery(sf.Connection, ct, modifiedSql, parms);
			else
				ret = DbHelper.ExecuteNonQuery(sf.Transaction, ct, modifiedSql, parms);
			foreach (IDataParameter p in parms)
				if (p.Direction == ParameterDirection.ReturnValue)
					ret = Convert.ToInt32(p.Value);
			return ret;
		}

		public static int ExecuteNonQuery(string sql, params object[] parms) {
			Logger.Info("ExecuteNonQuery " + sql); 
			string[] paramNames = (string[])DbHelper.ExtractParametersFromString(ref sql).ToArray(typeof(string));
			if (paramNames.Length != parms.Length)
				throw new Exception("Lunghezza parametri errata");
			Map map = new Map();
			for (int i = 0; i < paramNames.Length; i++)
				map[paramNames[i]] = parms[i];
			return ExecuteNonQuery(sql, map);
		}
		
		#endregion

		#region Operazioni sul database usando il QueryStore
		public static IDataReader ExecuteReaderQs(string type, string query, Map map) {
			Logger.Info(String.Format("ExecuteReaderQs({0}, {1})", type, query));
			Logger.Debug(map.ToShortJson());
			SfCommand cmd;
			ServerFacade sf = ServerFacade.Current;
			if (sf.Connection == null)
				return null;
			if (!sf.GetFromQueryStore(type, query, out cmd))
				return null;
			IDataParameter[] parms = sf.FillParameters(cmd.GetParams(0), map);
			if (sf.Transaction == null)
				return DbHelper.ExecuteReader(sf.Connection, cmd.GetCommandType(0), cmd.GetText(0), parms);
			else
				return DbHelper.ExecuteReader(sf.Transaction, cmd.GetCommandType(0), cmd.GetText(0), parms);
		}

		public static IDataReader ExecuteReaderQs(string type, string query, params object[] paramValues) {
			Logger.Info(String.Format("ExecuteReaderQs({0}, {1})", type, query)); 
			SfCommand cmd;
			ServerFacade sf = ServerFacade.Current;
			if (sf.Connection == null)
				return null;
			if (!sf.GetFromQueryStore(type, query, out cmd))
				return null;
			IDataParameter[] parms = sf.FillParameters(cmd.GetParams(0), paramValues);
			if (sf.Transaction == null)
				return DbHelper.ExecuteReader(sf.Connection, cmd.GetCommandType(0), cmd.GetText(0), parms);
			else
				return DbHelper.ExecuteReader(sf.Transaction, cmd.GetCommandType(0), cmd.GetText(0), parms);
		}

		public static int ExecuteNonQueryQs(string type, string query, Map map) {
			Logger.Info(String.Format("ExecuteNonQueryQs({0}, {1})", type, query)); 
			Logger.Debug(map.ToShortJson());
			SfCommand cmd;
			ServerFacade sf = ServerFacade.Current;
			if (sf.Connection == null)
				return 0;
			if (!sf.GetFromQueryStore(type, query, out cmd))
				return 0;
			int ret = 0;
			for (int i = 0; i < cmd.GetCommandCount(); i++) {
				IDataParameter[] parms = sf.FillParameters(cmd.GetParams(i), map);
				if (sf.Transaction == null)
					ret = DbHelper.ExecuteNonQuery(sf.Connection, cmd.GetCommandType(i), cmd.GetText(i), parms);
				else
					ret = DbHelper.ExecuteNonQuery(sf.Transaction, cmd.GetCommandType(i), cmd.GetText(i), parms);
			}
			return ret;
		}

		public static int ExecuteNonQueryQs(string type, string query, params object[] paramValues) {
			Logger.Info(String.Format("ExecuteNonQueryQs({0}, {1})", type, query)); 
			SfCommand cmd;
			ServerFacade sf = ServerFacade.Current;
			if (sf.Connection == null)
				return 0;
			if (!sf.GetFromQueryStore(type, query, out cmd))
				return 0;
			int ret = 0;
			for (int i = 0; i < cmd.GetCommandCount(); i++) {
				IDataParameter[] parms = sf.FillParameters(cmd.GetParams(i), paramValues);
				if (sf.Transaction == null)
					ret = DbHelper.ExecuteNonQuery(sf.Connection, cmd.GetCommandType(i), cmd.GetText(i), parms);
				else
					ret = DbHelper.ExecuteNonQuery(sf.Transaction, cmd.GetCommandType(i), cmd.GetText(i), parms);
			}
			return ret;
		}
		#endregion
	}

	public class SqlServerServerFacade: ServerFacade {
		#region Implementazione metodi astratti
		public SqlServerServerFacade(string dsn, string rootFolder): base(dsn, rootFolder) {}
		public override IDataParameter[] CreateParameters(int count) { return new SqlParameter[count]; }
		public override IDbConnection CreateConnection(string dsn) { return new SqlConnection(dsn); }
		
		public override IDataParameter CreateParameter(string nam, Object val) { 
			return new SqlParameter("@" + nam, val); 
		}

		public override void SetParameterValue(IDataParameter param, Object val) {
			param.Value = val;
		}

		public override IDataParameter CreateParameter(string nam) { 
			SqlDbType dbt = SqlDbType.VarChar;
			bool retVal = false;
			if (nam.Length >= 2 && nam[0] == '_') {
				if (nam[1] == 'i')
					dbt = SqlDbType.Int;
				else if (nam.Substring(1,2) == "dt")
					dbt = SqlDbType.DateTime;
				else if (nam.Substring(1, 2) == "bl")
					dbt = SqlDbType.Image;
				else if (nam.Substring(1,2) == "ri") {
					dbt = SqlDbType.Int;
					retVal = true;
				}
			}
			SqlParameter param = new SqlParameter("@" + DbHelper.RemovePrefix(nam), dbt); 
			if (retVal)
				param.Direction = ParameterDirection.ReturnValue;
			return param;
		}

		public override string[] PrepareParameters(ref string sql, ref CommandType ct) {
			ArrayList paramNames;
			if (DbHelper.IsStoredProcedure(sql)) {
				paramNames = DbHelper.ExtractParametersForSp(ref sql);
				paramNames.Add("_riRisultato");
				ct = CommandType.StoredProcedure;
			}
			else {
				paramNames = DbHelper.ExtractParametersFromString(ref sql);
			}
			return (string[])paramNames.ToArray(typeof(string));
		}

		public override string DbType { get { return ServerFacade.SqlServer; }}
		public override bool UseOracle { get { return false; } }
		public override bool UseOleDb { get { return false; } }
		public override bool UseMsSql { get { return true; } }
		public override bool UseMsde { get { return false; } }
		public override bool UseSqlCE { get { return false; } }

		#endregion
	}

	public class MsdeServerFacade: SqlServerServerFacade {
		#region Implementazione metodi astratti
		public MsdeServerFacade(string dsn, string rootFolder) : base(dsn, rootFolder) { }

		// creazione connessione - verifica che sia effettivamente un Msde non un SqlServer
		public override IDbConnection CreateConnection(string dsn) {
			IDbConnection connection = new SqlConnection(dsn);

			int engineEdition = Convert.ToInt32(ExecuteScalar(connection, "SELECT SERVERPROPERTY('ENGINEEDITION')"));
			if (engineEdition == 4)
				return connection;
			if (engineEdition == 1) {
				string edition = Convert.ToString(ExecuteScalar(connection, "SELECT SERVERPROPERTY('EDITION')")).ToLower();
				if (edition == "desktop engine")
					return connection;
			}
			string version = Convert.ToString(ExecuteScalar(connection, "SELECT @@VERSION")).ToLower();
			if (version.IndexOf("msde") > -1 || version.IndexOf("desktop engine") > -1)
				return connection;
			throw new ApplicationError(ApplicationError.RegistrazioneErrata, new Map("errore", 11));
		}

		private object ExecuteScalar(IDbConnection connection, string sql) {
			try {
				return DbHelper.ExecuteScalar(connection, CommandType.Text, sql);
			} catch (Exception e) {
				Logger.Warn("ExecuteScalar('" + sql + "')", e);
				return null;
			}
		}

		public override string DbType { get { return ServerFacade.Msde; } }
		public override bool UseMsSql { get { return false; } }
		public override bool UseMsde { get { return true; } }

		public override string GetDBPath() { return ServerFacade.SqlServer; }
		#endregion
	}

	public class OracleServerFacade: ServerFacade {
		#region Implementazione metodi astratti
		public OracleServerFacade(string dsn, string rootFolder) : base(dsn, rootFolder) { }
		public override IDataParameter[] CreateParameters(int count) { return new OracleParameter[count]; }
		public override IDbConnection CreateConnection(string dsn) { return new OracleConnection(dsn); }
		
		public override IDataParameter CreateParameter(string nam, Object val) { 
			return new OracleParameter(nam, val); 
		}

		public override void SetParameterValue(IDataParameter param, Object val) {
			if (val is string && Convert.ToString(val) == "")
				val = "''";
			param.Value = val;
		}

		public override IDataParameter CreateParameter(string nam) { 
			OracleType dbt = OracleType.VarChar;
			bool retVal = false;
			if (nam.Length >= 2 && nam[0] == '_') {
				if (nam[1] == 'i')
					dbt = OracleType.Int32;
				else if (nam.Substring(1,2) == "dt")
					dbt = OracleType.DateTime;
				else if (nam.Substring(1, 2) == "bl")
					dbt = OracleType.Blob;
				else if (nam.Substring(1,2) == "ri") {
					dbt = OracleType.Int32;
					retVal = true;
				}
			}
			OracleParameter param = new OracleParameter(DbHelper.RemovePrefix(nam), dbt); 
			if (retVal)
				param.Direction = ParameterDirection.ReturnValue;
			return param;
		}

		public override string[] PrepareParameters(ref string sql, ref CommandType ct) {
			ArrayList paramNames;
			if (DbHelper.IsStoredProcedure(sql)) {
				paramNames = DbHelper.ExtractParametersForSp(ref sql);
				paramNames.Add("_riRisultato");
				ct = CommandType.StoredProcedure;
			}
			else {
				paramNames = DbHelper.ExtractParametersFromString(ref sql);
			}
			sql = sql.Replace('@', ':');
			return (string[])paramNames.ToArray(typeof(string));
		}

		public override string DbType { get { return ServerFacade.Oracle; } }
		public override bool UseOracle { get { return true; } }
		public override bool UseOleDb { get { return false; } }
		public override bool UseMsSql { get { return false; } }
		public override bool UseMsde { get { return false; } }
		public override bool UseSqlCE { get { return false; } }
		#endregion
	}

	public class OleDbServerFacade: ServerFacade {
		#region Implementazione metodi astratti
		public OleDbServerFacade(string dsn, string rootFolder) : base(dsn, rootFolder) { }
		public override IDataParameter[] CreateParameters(int count) { return new OleDbParameter[count]; }
		public override IDbConnection CreateConnection(string dsn) { return new OleDbConnection(dsn); }
		public override IDataParameter CreateParameter(string nam, Object val) { return new OleDbParameter(nam, val); }
		public override void SetParameterValue(IDataParameter param, Object val) { param.Value = val; }
		public override IDataParameter CreateParameter(string nam) { 
			OleDbType dbt = OleDbType.VarChar;
			if (nam.Length >= 2 && nam[0] == '_') {
				if (nam[1] == 'i')
					dbt = OleDbType.Integer;
				if (nam.Substring(1,2) == "dt")
					dbt = OleDbType.DBTimeStamp;
				if (nam.Substring(1, 2) == "bl")
					dbt = OleDbType.VarBinary;
			}
			return new OleDbParameter(DbHelper.RemovePrefix(nam), dbt); 
		}
		public override string[] PrepareParameters(ref string sql, ref CommandType ct) {
			// TODO: qua deve sostituire @parametro con ? nella query
			return new string[0];
		}

		public override string DbType { get { return ServerFacade.OleDb; } }
		public override bool UseOracle { get { return false; } }
		public override bool UseOleDb { get { return true; } }
		public override bool UseMsSql { get { return false; } }
		public override bool UseMsde { get { return false; } }
		public override bool UseSqlCE { get { return false; } }
		#endregion
	}


}
