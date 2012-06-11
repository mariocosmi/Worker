using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;
using System.Web;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data.OleDb;

namespace Solari.Core {

	public sealed class DbHelper {
		#region private utility methods & constructors

		//Since this class provides only static methods, make the default constructor private to prevent 
		//instances from being created with "new DbHelper()".
		private DbHelper() {}

		/// <summary>
		/// This method is used to attach array of IDataParameters to a IDbCommand.
		/// 
		/// This method will assign a value of DbNull to any parameter with a direction of
		/// InputOutput and a value of null.  
		/// 
		/// This behavior will prevent default values from being used, but
		/// this will be the less common case than an intended pure output parameter (derived as InputOutput)
		/// where the user provided no input value.
		/// </summary>
		/// <param name="command">The command to which the parameters will be added</param>
		/// <param name="commandParameters">an array of IDataParameters tho be added to command</param>
		private static void AttachParameters(IDbCommand command, IDataParameter[] commandParameters) {
			foreach (IDataParameter p in commandParameters) {
				//check for derived output value with no value assigned
				if ((p.Direction == ParameterDirection.InputOutput) && (p.Value == null)) {
					p.Value = DBNull.Value;
				}
			
				command.Parameters.Add(p);
			}
		}

		/// <summary>
		/// This method assigns an array of values to an array of IDataParameters.
		/// </summary>
		/// <param name="commandParameters">array of IDataParameters to be assigned values</param>
		/// <param name="parameterValues">array of objects holding the values to be assigned</param>
		private static void AssignParameterValues(IDataParameter[] commandParameters, object[] parameterValues) {
			if ((commandParameters == null) || (parameterValues == null)) {
				//do nothing if we get no data
				return;
			}

			// we must have the same number of values as we pave parameters to put them in
			if (commandParameters.Length != parameterValues.Length) {
				throw new ArgumentException("Parameter count does not match Parameter Value count.");
			}

			//iterate through the IDataParameters, assigning the values from the corresponding position in the 
			//value array
			for (int i = 0, j = commandParameters.Length; i < j; i++) {
				commandParameters[i].Value = parameterValues[i];
			}
		}

		/// <summary>
		/// This method opens (if necessary) and assigns a connection, transaction, command type and parameters 
		/// to the provided command.
		/// </summary>
		/// <param name="command">the IDbCommand to be prepared</param>
		/// <param name="connection">a valid IDbConnection, on which to execute this command</param>
		/// <param name="transaction">a valid IDbTransaction, or 'null'</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of IDataParameters to be associated with the command or 'null' if no parameters are required</param>
		private static void PrepareCommand(IDbCommand command, IDbConnection connection, IDbTransaction transaction, CommandType commandType, string commandText, IDataParameter[] commandParameters) {
			//if the provided connection is not open, we will open it
			if (connection.State != ConnectionState.Open) {
				connection.Open();
			}

			//associate the connection with the command
			command.Connection = connection;

			//set the command text (stored procedure name or SQL statement)
			command.CommandText = commandText;

			//if we were provided a transaction, assign it.
			if (transaction != null) {
				command.Transaction = transaction;
			}

			//set the command type
			command.CommandType = commandType;

			//attach the command parameters if they are provided
			if (commandParameters != null) {
				AttachParameters(command, commandParameters);
			}

			return;
		}


		#endregion private utility methods & constructors

		#region ExecuteNonQuery

		/// <summary>
		/// Execute a IDbCommand (that returns no resultset) against the specified IDbConnection 
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="connection">a valid IDbConnection</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>an int representing the number of rows affected by the command</returns>
		public static int ExecuteNonQuery(IDbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {	
			//create a command and prepare it for execution
			IDbCommand cmd = connection.CreateCommand();
			PrepareCommand(cmd, connection, (IDbTransaction)null, commandType, commandText, commandParameters);
		
			//finally, execute the command.
			int retval = cmd.ExecuteNonQuery();
		
			// detach the IDataParameters from the command object, so they can be used again.
			cmd.Parameters.Clear();
			return retval;
		}

		/// <summary>
		/// Execute a IDbCommand (that returns no resultset and takes no parameters) against the provided IDbTransaction. 
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders");
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <returns>an int representing the number of rows affected by the command</returns>
		public static int ExecuteNonQuery(IDbTransaction transaction, CommandType commandType, string commandText) {
			//pass through the call providing null for the set of IDataParameters
			return ExecuteNonQuery(transaction, commandType, commandText, (IDataParameter[])null);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns no resultset) against the specified IDbTransaction
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>an int representing the number of rows affected by the command</returns>
		public static int ExecuteNonQuery(IDbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {
			//create a command and prepare it for execution
			IDbCommand cmd = transaction.Connection.CreateCommand();
			PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);
		
			//finally, execute the command.
			int retval = cmd.ExecuteNonQuery();
		
			// detach the IDataParameters from the command object, so they can be used again.
			cmd.Parameters.Clear();
			return retval;
		}

		#endregion ExecuteNonQuery

		#region ExecuteReader

		/// <summary>
		/// this enum is used to indicate whether the connection was provided by the caller, or created by DbHelper, so that
		/// we can set the appropriate CommandBehavior when calling ExecuteReader()
		/// </summary>
		private enum IDbConnectionOwnership {
			/// <summary>Connection is owned and managed by DbHelper</summary>
			Internal, 
			/// <summary>Connection is owned and managed by the caller</summary>
			External
		}

		/// <summary>
		/// Create and prepare a IDbCommand, and call ExecuteReader with the appropriate CommandBehavior.
		/// </summary>
		/// <remarks>
		/// If we created and opened the connection, we want the connection to be closed when the DataReader is closed.
		/// 
		/// If the caller provided the connection, we want to leave it to them to manage.
		/// </remarks>
		/// <param name="connection">a valid IDbConnection, on which to execute this command</param>
		/// <param name="transaction">a valid IDbTransaction, or 'null'</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of IDataParameters to be associated with the command or 'null' if no parameters are required</param>
		/// <param name="connectionOwnership">indicates whether the connection parameter was provided by the caller, or created by DbHelper</param>
		/// <returns>IDataReader containing the results of the command</returns>
		private static IDataReader ExecuteReader(IDbConnection connection, IDbTransaction transaction, CommandType commandType, string commandText, IDataParameter[] commandParameters, IDbConnectionOwnership connectionOwnership) {	
			//create a command and prepare it for execution
			IDbCommand cmd = connection.CreateCommand();
			PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters);
		
			//create a reader
			IDataReader dr;

			// call ExecuteReader with the appropriate CommandBehavior
			if (connectionOwnership == IDbConnectionOwnership.External) {
				dr = cmd.ExecuteReader();
			}
			else {
				dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
			}
		
			// detach the IDataParameters from the command object, so they can be used again.
			cmd.Parameters.Clear();
		
			return dr;
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a resultset and takes no parameters) against the provided IDbConnection. 
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  IDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders");
		/// </remarks>
		/// <param name="connection">a valid IDbConnection</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <returns>a IDataReader containing the resultset generated by the command</returns>
		public static IDataReader ExecuteReader(IDbConnection connection, CommandType commandType, string commandText) {
			//pass through the call providing null for the set of IDataParameters
			return ExecuteReader(connection, commandType, commandText, (IDataParameter[])null);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a resultset) against the specified IDbConnection 
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  IDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="connection">a valid IDbConnection</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>a IDataReader containing the resultset generated by the command</returns>
		public static IDataReader ExecuteReader(IDbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {
			//pass through the call to the private overload using a null transaction value and an externally owned connection
			return ExecuteReader(connection, (IDbTransaction)null, commandType, commandText, commandParameters, IDbConnectionOwnership.External);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a resultset and takes no parameters) against the provided IDbTransaction. 
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  IDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders");
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <returns>a IDataReader containing the resultset generated by the command</returns>
		public static IDataReader ExecuteReader(IDbTransaction transaction, CommandType commandType, string commandText) {
			//pass through the call providing null for the set of IDataParameters
			return ExecuteReader(transaction, commandType, commandText, (IDataParameter[])null);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a resultset) against the specified IDbTransaction
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///   IDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>a IDataReader containing the resultset generated by the command</returns>
		public static IDataReader ExecuteReader(IDbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {
			//pass through to private overload, indicating that the connection is owned by the caller
			return ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, IDbConnectionOwnership.External);
		}

		#endregion ExecuteReader

		#region ExecuteScalar
	
		/// <summary>
		/// Execute a IDbCommand (that returns a 1x1 resultset and takes no parameters) against the provided IDbConnection. 
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount");
		/// </remarks>
		/// <param name="connection">a valid IDbConnection</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <returns>an object containing the value in the 1x1 resultset generated by the command</returns>
		public static object ExecuteScalar(IDbConnection connection, CommandType commandType, string commandText) {
			//pass through the call providing null for the set of IDataParameters
			return ExecuteScalar(connection, commandType, commandText, (IDataParameter[])null);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a 1x1 resultset) against the specified IDbConnection 
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="connection">a valid IDbConnection</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>an object containing the value in the 1x1 resultset generated by the command</returns>
		public static object ExecuteScalar(IDbConnection connection, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {
			//create a command and prepare it for execution
			IDbCommand cmd = connection.CreateCommand();
			PrepareCommand(cmd, connection, (IDbTransaction)null, commandType, commandText, commandParameters);
		
			//execute the command & return the results
			object retval = cmd.ExecuteScalar();
		
			// detach the IDataParameters from the command object, so they can be used again.
			cmd.Parameters.Clear();
			return retval;
		
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a 1x1 resultset and takes no parameters) against the provided IDbTransaction. 
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount");
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <returns>an object containing the value in the 1x1 resultset generated by the command</returns>
		public static object ExecuteScalar(IDbTransaction transaction, CommandType commandType, string commandText) {
			//pass through the call providing null for the set of IDataParameters
			return ExecuteScalar(transaction, commandType, commandText, (IDataParameter[])null);
		}

		/// <summary>
		/// Execute a IDbCommand (that returns a 1x1 resultset) against the specified IDbTransaction
		/// using the provided parameters.
		/// </summary>
		/// <remarks>
		/// e.g.:  
		///  int orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new IDataParameter("@prodid", 24));
		/// </remarks>
		/// <param name="transaction">a valid IDbTransaction</param>
		/// <param name="commandType">the CommandType (stored procedure, text, etc.)</param>
		/// <param name="commandText">the stored procedure name or T-SQL command</param>
		/// <param name="commandParameters">an array of SqlParamters used to execute the command</param>
		/// <returns>an object containing the value in the 1x1 resultset generated by the command</returns>
		public static object ExecuteScalar(IDbTransaction transaction, CommandType commandType, string commandText, params IDataParameter[] commandParameters) {
			//create a command and prepare it for execution
			IDbCommand cmd = transaction.Connection.CreateCommand();
			PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);
		
			//execute the command & return the results
			object retval = cmd.ExecuteScalar();
		
			// detach the IDataParameters from the command object, so they can be used again.
			cmd.Parameters.Clear();
			return retval;
		}

		#endregion ExecuteScalar	

		#region Funzioni di comodità

		public static string ByteStreamToBase64String(Stream stm, int length) {
			using (BinaryReader sr = new BinaryReader(stm))
				return (Convert.ToBase64String(sr.ReadBytes(length)));
		}


		public static void ByteStreamToFile(Stream stm, int length, string path) {
			using (BinaryReader sr = new BinaryReader(stm))
				using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(path)))
					bw.Write(sr.ReadBytes(length));
		}
	
		public static byte[] StrToByteArray(string str) {
			System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
			return encoding.GetBytes(str);
		}

		public static string ByteArrayToStr(byte[] dBytes) {
			System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
			return enc.GetString(dBytes);
		}

		public static string StringToBase64String(string buffer) {
			return Convert.ToBase64String(new UTF8Encoding().GetBytes(buffer));
		}

		public static string Base64StringToString(string b64) {
			return new UTF8Encoding().GetString(Convert.FromBase64String(b64));
		}

		public static long ToJsonDate(DateTime dt) {
			DateTime dtJavascriptZero = new DateTime(1970, 01, 01);
			return (dt.ToUniversalTime().Ticks - dtJavascriptZero.Ticks) / 10000; // converte in millisecondi
		}

		public static DateTime FromJsonDate(long dt) {
			long ticks = new DateTime(1970, 01, 01).ToLocalTime().Ticks + dt * 10000;
			return new DateTime(ticks);
		}

		public static int ToZulianDate(DateTime dt) {
			DateTime dtRif = new DateTime(2006, 01, 01);
			int nRif = 732312; // data di riferimento: 01/01/2006 = 732312 zuliano
			return dt.Subtract(dtRif).Days + nRif;
		}

		public static int ToZulianTime(DateTime dt) { // secondi dalla mezzanotte
			return Convert.ToInt32(dt.TimeOfDay.TotalSeconds);
		}

		public static DateTime FromZulianTime(int dt) {
			return new DateTime(1900, 01, 01).AddSeconds(dt);
		}

		public static DateTime ToDateTime(string val) {
			if (val == "NOW")
				return DateTime.Now;
			else if (IsExprData(val))
				return ValutaExprData(val);
			else
				return Convert.ToDateTime(val);
		}

		#region interpete espressioni data
		
		private static DateTime PrevMonday(DateTime d) {
			//ritorna il lunedì precedente, all'ora 00:00
			DateTime temp;
			int delta = 0;
			switch (d.DayOfWeek) {
				case DayOfWeek.Sunday: delta = -6; break;
				case DayOfWeek.Monday: delta = 0; break;
				case DayOfWeek.Tuesday: delta = -1; break;
				case DayOfWeek.Wednesday: delta = -2; break;
				case DayOfWeek.Thursday: delta = -3; break;
				case DayOfWeek.Friday: delta = -4; break;
				case DayOfWeek.Saturday: delta = -5; break;
			}
			temp = d.AddDays(delta);
			return new DateTime(temp.Year, temp.Month, temp.Day, 0, 0, 0);
		}

		private static DateTime ValutaExprData(string s) {
			DateTime val = DateTime.Today;
			DateTime d = DateTime.Now;
			bool valInizio = (s[0] == 'I'); //può essere I o F
			char unita = s[1];
			int delta = 0;

			if (s.Length > 2) { //tipo IA-2 oppure IM3
				delta  = Convert.ToInt32(s.Substring(2));
				s = s.Substring(0, 2);
			}

			switch (unita) {
				case 'A': {
					d = d.AddYears(delta);
					if (valInizio)
						val = new DateTime(d.Year, 1, 1);
					else
						val = new DateTime(d.Year, 12, 31, 23, 59, 59);
					break;
				}
				case 'M': {
					d = d.AddMonths(delta);
					if (valInizio)
						val = new DateTime(d.Year, d.Month, 1);
					else {
						if (d.Month == 12)
							val = new DateTime(d.Year, d.Month, 31, 23, 59, 59);
						else
							val = new DateTime(d.Year, d.Month + 1, 1, 23, 59, 59).AddDays(-1);//ultimo del mese
					}
					break;
				}
				case 'S': {
					d = d.AddDays(7*delta);
					if (valInizio)
						val = PrevMonday(d);
					else {
						val = PrevMonday(d).AddDays(6);
						val = new DateTime(val.Year, val.Month, val.Day, 23, 59, 59);
					}
					break;
				}
				case 'G': {
					d = d.AddDays(delta);
					if (valInizio)
						val = new DateTime(d.Year, d.Month, d.Day);
					else
						val = new DateTime(d.Year, d.Month, d.Day, 23, 59, 59);
					break;
				}
				case 'H': {
					d = d.AddHours(delta);
					if (valInizio)
						val = new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);
					else
						val = new DateTime(d.Year, d.Month, d.Day, d.Hour, 59, 59);
					break;
				}
				case 'N': {
					d = d.AddMinutes(delta);
					if (valInizio)
						val = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
					else
						val = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 59);
					break;
				}
			}
			return val;
		}

		private static bool IsExprData(string s) {
			Regex parser = new Regex(@"[IF][AMSGHN]((-)?\d+)?");
			Match m = parser.Match(s);
			return (m.Length == s.Length); //matching completo
		}		 
		#endregion

		public static int ToInt(object val) {
			return (val == DBNull.Value || Convert.ToString(val) == "") ? 0: Convert.ToInt32(val);
		}

		private static object FromPrefix(string nam, string val) {
			if (val == "")
				return null;
			if (nam.Length > 2 && nam[0] == '_') {
				if (nam[1] == 'i')
					return Convert.ToInt32(val);
				if (nam.Substring(1,2) == "dt")
					return DbHelper.ToDateTime(val);
				if (nam.Substring(1, 2) == "bl") {
					return StrToByteArray(val);
				}
			}
			return val;
		}

		public static string RemovePrefix(string nam) {
			if (nam.StartsWith("?"))
				return nam.Substring(1);
			if (nam.Length > 2 && nam[0] == '_') {
				if (nam[1] == 'i')
					return nam.Substring(2);
				if (nam.Substring(1,2) == "dt")
					return nam.Substring(3);
				if (nam.Substring(1,2) == "ri")
					return nam.Substring(3);
				if (nam.Substring(1, 2) == "bt")
					return nam.Substring(3);
				if (nam.Substring(1, 2) == "bl")
					return nam.Substring(3);
			}
			return nam;
		}

		public static Hashtable FillHashtable(IDataRecord rec, Hashtable map) {
			for (int i = 0; i < rec.FieldCount; i++) {
				string nam = rec.GetName(i).ToLower();
				object val = null;
				try {
					val = rec.GetValue(i);
				} catch (Exception) {}
				if (val == null || val == DBNull.Value)
					map.Remove(nam);
				else
					map[nam] = val;
			}
			return map;
		}

		public static Hashtable FillHashtable(NameValueCollection coll, Hashtable map) {
			for (int i = 0; i < coll.Count ; i++) {
				if (coll.Keys[i] == null)
					continue;
				string nam = coll.Keys[i].ToLower();
				string newNam = RemovePrefix(nam);
				object newVal = FromPrefix(nam, coll[nam]);
				if (newVal == null)
					map.Remove(newNam);
				else if (newNam != "_")
					map[newNam] = newVal;
			}
			return map;
		}

		public static void CompleteFillHashtable(NameValueCollection coll, Hashtable map) {
			for (int i = 0; i < coll.Count ; i++) {
				string nam = coll.Keys[i];
				string newNam = RemovePrefix(nam);
				object newVal = FromPrefix(nam, coll[nam]);
				if (newNam != "_")
					map[newNam] = newVal;
			}
		}

		public static ArrayList ExtractRegexpFromString(string text, string pattern, bool unique, int offset) {
			Regex re = new Regex(pattern);
			MatchCollection col = re.Matches(text);
			ArrayList vett = new ArrayList();
			for (int i = 0; i < col.Count; i++) {
				if (unique) {
					int j;
					for (j = 0; j < vett.Count; j++)
						if (col[i].Value.Substring(offset).ToLower() == ((string)vett[j]).ToLower())
							break;
					if (j == vett.Count)
						vett.Add(col[i].Value.Substring(offset));
				}
				else
					vett.Add(col[i].Value.Substring(offset));
			}
			return vett;
		}

		public static string _dtRemover(Match match) {
			// toglie i primi caratteri da @_dtParametro -> @Parametro
			return "@" + match.Value.Substring(4);
		}

		public static string _iRemover(Match match) {
			return "@" + match.Value.Substring(3);
		}

		public static string _blRemover(Match match) {
			// toglie i primi caratteri da @_blParametro -> @Parametro
			return "@" + match.Value.Substring(4);
		}

		public static string _btRemover(Match match) {
			// toglie i primi caratteri da @_btParametro -> @Parametro
			return "@" + match.Value.Substring(4);
		}

		public static ArrayList ExtractParametersFromString(ref string sql) {
			// le istruzioni hanno la forma SELECT * FROM tabella WHERE id = @p1
			// estraggo tutti i termini che iniziano per @
			// ma non con @@ (es in Oracle @@spid)
			ArrayList al = ExtractRegexpFromString(sql, @"[^@]@(\w+)", true, 1);
			for (int i = 0; i < al.Count; i++)
				al[i] = ((string)al[i]).ToLower().Substring(1);
			// ora tolgo il prefisso di tipo ai parametri
			sql = Regex.Replace(sql, "@_dt(\\w+)", new MatchEvaluator(_dtRemover));
			sql = Regex.Replace(sql, "@_i(\\w+)", new MatchEvaluator(_iRemover));
			sql = Regex.Replace(sql, "@_bl(\\w+)", new MatchEvaluator(_blRemover));
			sql = Regex.Replace(sql, "@_bt(\\w+)", new MatchEvaluator(_btRemover));
			return al;
		}

		public static ArrayList ExtractParametersForSp(ref string sp) {
			ArrayList al = ExtractRegexpFromString(sp, "(\\w+)", false, 0);
			// estraggo tutte le parole, senza @
			// i parametri hanno sempre nomi diversi, perciò non serve il ciclo come in ExtractParametersFromString
			for (int i = 0; i < al.Count; i++)
				al[i] = ((string)al[i]).ToLower();
			sp = (string)al[0];
			al.RemoveAt(0);
			return al;
		}

		public static bool IsStoredProcedure(string text) {
			// le chiamate a s.p. hanno la forma procname(@p1,@p2)
			Match m = Regex.Match(text, @"\s*(\w+)\(\s*(@\w+\s*,\s*)*(@\w+\s*)*\)\s*");
			return text.Length == m.Length;
		}

		public static void ReadfirstRecord(IDataReader rdr, ref Map firstRecord) {
			using (rdr)
				if (rdr != null && rdr.Read()) {
					DbHelper.FillHashtable(rdr, firstRecord);
				}
		}

		public static ArrayList MakeArray(IDataReader rdr) {
			ArrayList ret = new ArrayList();
			using (rdr) {
				while (rdr != null && rdr.Read()) {
					//Map record = new Map(rdr);
					Map record = new Map();
					DbHelper.FillHashtable(rdr, record);
					ret.Add(record);
				}
			}
			return ret;
		}

		#endregion
	}
}
