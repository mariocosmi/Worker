using System;
using System.Collections;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Principal;
namespace Solari.Core {
	public class IpcHelper: MarshalByRefObject {
		static protected readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(IpcHelper));

		private static IpcServerChannel _serverChannel = null;
		private static IpcClientChannel _clientChannel = null;

		public static void InitializeServer(string objectUri, string portName, System.Type type) {
			if (_serverChannel != null) {
				Logger.Info("IpcHelper.InitializeServer chiude il canale aperto ...");
				ChannelServices.UnregisterChannel(_serverChannel);
			}

            Hashtable props = new Hashtable();
            props.Add("authorizedGroup", "Everyone");
            props.Add("portName", portName);
            props.Add("exclusiveAddressUse", "false");
			_serverChannel = new IpcServerChannel(props, null, null);
			ChannelServices.RegisterChannel(_serverChannel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(type, objectUri, WellKnownObjectMode.SingleCall);
			Logger.Info("CacheService inizializzato");
		}

		public static void InitializeClient(string objectUri, string portName, System.Type type) {
			if (_clientChannel != null) {
				Logger.Info("IpcHelper.InitializeClient chiude il canale aperto ...");
				ChannelServices.UnregisterChannel(_clientChannel);
			}
			Hashtable props = new Hashtable();
			props.Add("connectionTimeout", 100000);
			_clientChannel = new IpcClientChannel(props, null);
			ChannelServices.RegisterChannel(_clientChannel, true);
			RemotingConfiguration.RegisterWellKnownClientType(type, string.Format("ipc://{0}/{1}", portName, objectUri));
			Logger.Info("CacheClientHelper inizializzato");
		}

		public static Hashtable Marshal(ModelBase model) {
			Hashtable ret = new Hashtable();
			foreach (object k in model.Keys) {
				object value = model[k];
				if (value is ModelBase)
					value = Marshal(value as ModelBase);
				else if (value is ArrayList)
					value = Marshal(value as ArrayList);
				ret.Add(k, value);
			}
			return ret;
		}

		public static ArrayList Marshal(ArrayList list) {
			ArrayList ret = new ArrayList();
			foreach (object el in ret) {
				object value = el;
				if (value is ModelBase)
					value = Marshal(value as ModelBase);
				else if (value is ArrayList)
					value = Marshal(value as ArrayList);
				ret.Add(value);
			}
			return ret;
		}

		public static ModelBase Unmarshal(Hashtable map, System.Type type) {
			if (map == null)
				return null;
			ModelBase model = ModelHelper.Instance.New(type);
			foreach (object k in map.Keys)
				model[k] = map[k];
			return model;
		}

		public static Map Unmarshal(Hashtable map) {
			if (map == null)
				return null;
			Map ret = new Map();
			foreach (object k in map.Keys) {
				object x = map[k];
				if (x is Hashtable)
					ret.Add(k, Unmarshal(x as Hashtable));
				else if (x is ArrayList)
					ret.Add(k, Unmarshal(x as ArrayList));
				else
					ret.Add(k, x);
			}
			return ret;
		}

		public static ArrayList Unmarshal(ArrayList vett) {
			if (vett == null)
				return null;
			ArrayList ret = new ArrayList();
			foreach (object x in vett) {
				if (x is Hashtable)
					ret.Add(Unmarshal(x as Hashtable));
				else if (x is ArrayList)
					ret.Add(Unmarshal(x as ArrayList));
				else
					ret.Add(x);
			}
			return ret;
		}
	}
}
