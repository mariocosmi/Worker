using System.IO;
using Solari.Core;

namespace Worker.Units {
	[Plugin("ReadXML")]
	class ReadXML : UnitOfWork {
		public override Map Execute(Map input, ServerFacade sf, ConfigHelper cfg, out bool hasMore, out bool cut) {
			hasMore = cut = false;
			var strData = File.ReadAllText(this.GetString("infile"));
			return Map.FromXmlObject(strData).GetMap(this.GetString("path"));
		}
	}
}
