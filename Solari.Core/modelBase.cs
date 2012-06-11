using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Web;
using System.Reflection;

namespace Solari.Core {
	public abstract class ModelBase: Map {
		public static string evtPreInsert = "PreInsert";
		public static string evtPreUpdate = "PreUpdate";
		public static string evtPreDelete = "PreDelete";
		public static string evtInsert = "Insert";
		public static string evtUpdate = "Update";
		public static string evtDelete = "Delete";

		#region Member variables e costruttori
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(ModelBase));
		public abstract void Inizializza();

		protected bool _deletedFlag = false;
		public ModelBase() {}

		public ModelBase(NameValueCollection coll) {
			DbHelper.FillHashtable(coll, this);
		}

		public ModelBase(IDataRecord rec) {
			DbHelper.FillHashtable(rec, this);
		}
		#endregion

		#region Operazioni sugli oggetti
		public virtual bool IsDeleted() { return _deletedFlag; }
		public virtual void Delete() { _deletedFlag = true; }
		public abstract bool IsNew();

		public virtual void Save() {
			if (IsDeleted())
				DoDelete();
			else if (IsNew())
				DoInsert();
			else
				DoUpdate();
		}

		protected virtual void DoInsert() {
			this.DoInsert(true, true);
		}

		protected virtual void DoUpdate() {
			this.DoUpdate(true, true);
		}

		protected virtual void DoDelete() {
			this.DoDelete(true, true);
		}

		protected virtual void DoInsert(bool raisePre, bool raisePost) {
			if (raisePre)
				EventBus.Instance.Raise(this, ModelBase.evtPreInsert);
			if (ServerFacade.ExecuteNonQueryQs(ModelHelper.Instance.ShortName(this.GetType()).ToLower(), "insert", this) == 0) {
				Logger.Info(string.Format("Impossibile inserire il record {0}", this.ToJson()));
				throw new ApplicationError(ApplicationError.ModificheFallite);
			}
			if (raisePost)
				EventBus.Instance.Raise(this, ModelBase.evtInsert);
		}

		protected virtual void DoUpdate(bool raisePre, bool raisePost) {
			if (raisePre)
				EventBus.Instance.Raise(this, ModelBase.evtPreUpdate);
			if (ServerFacade.ExecuteNonQueryQs(ModelHelper.Instance.ShortName(this.GetType()).ToLower(), "update", this) == 0) {
				Logger.Info(string.Format("Impossibile aggiornare il record {0}", this.ToJson()));
				throw new ApplicationError(ApplicationError.ModificheFallite);
			}
			if (raisePost)
				EventBus.Instance.Raise(this, ModelBase.evtUpdate);
		}

		protected virtual void DoDelete(bool raisePre, bool raisePost) {
			if (raisePre)
				EventBus.Instance.Raise(this, ModelBase.evtPreDelete);
			if (ServerFacade.ExecuteNonQueryQs(ModelHelper.Instance.ShortName(this.GetType()).ToLower(), "delete", this) == 0) {
				Logger.Info(string.Format("Impossibile cancellare il record {0}", this.ToJson()));
				throw new ApplicationError(ApplicationError.ModificheFallite);
			}
			if (raisePost)
				EventBus.Instance.Raise(this, ModelBase.evtDelete);
		}

		#endregion

		public abstract object PrimaryKey { get; }
	}

	#region IdModelBase
	public class IdModelBase: ModelBase {
		public IdModelBase(): base() {}

		public IdModelBase(NameValueCollection coll): base(coll) {}

		public IdModelBase(IDataRecord rec): base(rec) {}

		public override void Inizializza() {
			this.Id = 0;
		}

		public int Id {
			get { return Convert.ToInt32(this["id"]); }
			set { this["id"] = value; }
		}

		public override bool IsNew() {
			return this.Id == 0;
		}

		public override object PrimaryKey {
			get { return this.Id; }
		}

	}
	#endregion
}
