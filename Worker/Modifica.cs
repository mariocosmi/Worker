using System;
using System.Collections;
using System.Drawing;
using Solari.Core;

namespace Worker {
	class Modifica: Map {
		public static void CaricaDati(ServerFacade sf, out ArrayList ret, out int idPresenti) {
			ret = new ArrayList();
			idPresenti = 0;
			foreach (Map map in Utils.List(sf, "SELECT * FROM coda_escape ORDER BY ID", new Map())) {
				Modifica m = new Modifica(sf, map);
				ret.Add(m);
				if (m.IsModificaTimbratura && idPresenti == 0)
					idPresenti = m.GetInt("presenti.id");
			}
		}

		public Modifica(ServerFacade sf, Map map) {
			this.Copy(map);
			if (this.IsModificaAnagrafica || this.IsModificaProfilo) {
				Map anagrafica = Utils.Read(sf, "SELECT * FROM anagrafico a, profilo p WHERE a.id = @_iKey_int AND p.idUtilizzatore = a.id AND p.dataInizio < @_dtAdesso AND (p.DataFine IS NULL OR p.DataFine > @_dtAdesso)", this);
				this["anagrafica"] = anagrafica;
				this["ufficio"] = Utils.Read(sf, "SELECT * FROM UnitaPianta WHERE id = @_iIdUfficioVisitato", anagrafica);
			} else if (this.IsModificaImmagine) {
				Map anagrafica = Utils.Read(sf, "SELECT * FROM anagrafico a, profilo p WHERE a.id = @_iKey_int AND p.idUtilizzatore = a.id AND p.dataInizio < @_dtAdesso AND (p.DataFine IS NULL OR p.DataFine > @_dtAdesso)", this);
				Image img = Utils.ReadImageFile(anagrafica.GetString("imgdip"));
				if (img != null) {
					this["immagine"] = Utils.ResizeImage(img, 200, 200);
					this["immaginethumb"] = Utils.ResizeImage(img, 80, 80);
				}
			} else if (this.IsModificaVisite) {
				Map visita = Utils.Read(sf, "SELECT * FROM visite WHERE codicebadge = @Key_char AND datainiziobadge = @_dtKey_date", this);
				Map anagrafica = Utils.Read(sf, "SELECT a.*, p.* FROM badge b, anagrafico a, profilo p WHERE b.codicebadge = @Key_char AND b.datainiziobadge = @_dtKey_date AND b.idUtilizzatore = a.id AND p.idUtilizzatore = a.id AND p.dataInizio < @_dtAdesso AND (p.DataFine IS NULL OR p.DataFine > @_dtAdesso)", this);
				this["visita"] = visita;
				this["anagrafica"] = anagrafica;
				this["visitato"] = Utils.Read(sf, "SELECT * FROM anagrafico a, profilo p WHERE a.id = @_iIdVisitato AND p.idUtilizzatore = a.id AND p.dataInizio < @_dtAdesso AND (p.DataFine IS NULL OR p.DataFine > @_dtAdesso)", visita);
				this["ufficio"] = Utils.Read(sf, "SELECT * FROM UnitaPianta WHERE id = @_iIdUfficioVisitato", anagrafica);
			} else if (this.IsModificaTimbratura) {
				this["presenti"] = Utils.ReadValue(sf.InstanceExecuteReader("SELECT MAX(id) FROM PresentiPerArea", new Map()), 0);
			} else if (this.IsModificaUtenti) {
				this["utente"] = Utils.Read(sf, "SELECT * FROM Utenti WHERE id = @_iKey_int", this);
			}
		}

		#region Accesso ai dati

		private static string tipoAnagrafico = "ANAGRAFICO";
		private static string tipoImmagine = "DIPIMAGE";
		private static string tipoProfilo = "PROFILO";
		private static string tipoUtenti = "UTENTI";
		private static string tipoVisite = "VISITE";
		private static string tipoTimbratura = "TIMBACCESSI";

		public bool IsModificaAnagrafica {
			get { return this.GetString("tipo").ToUpper() == tipoAnagrafico; }
		}

		public bool IsModificaImmagine {
			get { return this.GetString("tipo").ToUpper() == tipoImmagine; }
		}

		public bool IsModificaProfilo {
			get { return this.GetString("tipo").ToUpper() == tipoProfilo; }
		}

		public bool IsModificaUtenti {
			get { return this.GetString("tipo").ToUpper() == tipoUtenti; }
		}

		public bool IsModificaVisite {
			get { return this.GetString("tipo").ToUpper() == tipoVisite; }
		}

		public bool IsModificaTimbratura {
			get { return this.GetString("tipo").ToUpper() == tipoTimbratura; }
		}
		#endregion


	}
}

