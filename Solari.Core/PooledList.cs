using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Solari.Core {
	public interface UnitOfWork {
		void DoWork(bool bAsync, bool bScheduled);
		void OnClose();
	}

	public class PooledList {
		#region PooledElement
		private class PooledElement {
			#region Proprietà
			private UnitOfWork _unit;
			private bool _queryStop;
			private RegisteredWaitHandle _handle;
			private int _timer;
			private AutoResetEvent _event;
			private AutoResetEvent _returnEvent;
			#endregion

			public PooledElement(UnitOfWork unit, int timer) {
				_unit = unit;
				_timer = timer;
				_event = new AutoResetEvent(false);
				_returnEvent = null;
				_queryStop = false;
				_handle = ThreadPool.RegisterWaitForSingleObject(_event, new WaitOrTimerCallback(DoWork), this, _timer, false);
			}

			public UnitOfWork Unit { get { return _unit; }}

			public void StartNowAsync() {
				this._event.Set();
			}

			public void StartNowSync() {
				this._returnEvent = new AutoResetEvent(false);
				try {
					this._event.Set();
					_returnEvent.WaitOne();
				} finally {
					this._returnEvent.Close();
					this._returnEvent = null;
				}
			}

			public void Stop() {
				this._queryStop = true;
				this.StartNowSync();
			}

			private static void DoWork(object state, bool timedOut) {
				PooledElement pe = state as PooledElement;
				if (pe._queryStop) {
					pe._handle.Unregister(pe._returnEvent);
					pe._unit.OnClose();
					return;
				}
				pe._unit.DoWork(pe._returnEvent == null, timedOut);
				if (pe._returnEvent != null)
					pe._returnEvent.Set();
			}
		}
		#endregion
		Dictionary<int, PooledElement> _list = new Dictionary<int, PooledElement>();

		public UnitOfWork this[int idx] {
			get { return _list[idx].Unit; }
		}

		public void Clear() {
			for (int i = 0; i < _list.Count; i++)
				_list[i].Stop();
			_list.Clear();
		}

		public void Add(int key, UnitOfWork job, int timer) {
			_list.Add(key, new PooledElement(job, timer));
		}

		public int Count {
			get { return _list.Count; }
		}

		public void StartNowSync(int key) {
			_list[key].StartNowSync();
		}

		public void StartNowAsync(int key) {
			_list[key].StartNowAsync();
		}

		public void Remove(int key) {
			_list[key].Stop();
			_list.Remove(key);
		}
	}
}
