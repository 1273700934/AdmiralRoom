﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Fiddler;

namespace Huoyaoyuan.AdmiralRoom.Officer
{
    public class Staff
    {
        private static Staff _current;
        public static Staff Current
        {
            get
            {
                if (_current == null) _current = new Staff();
                return _current;
            }
        }
        public Dispatcher Dispatcher { get; set; }
        public static Proxy Proxy { get; set; }
        public static UTF8Encoding Encoder = new UTF8Encoding();
        //private static Dictionary<string, Action<Session>> Handlers = new Dictionary<string, Action<Session>>();
        private static Dictionary<string, APIObservable> apisource = new Dictionary<string, APIObservable>();
        public static APIObservable API(string apiname)
        {
            APIObservable v;
            apisource.TryGetValue(apiname, out v);
            if (v == null) 
            {
                v = new APIObservable();
                apisource.Add(apiname, v);
            }
            return v;
        }
        public static void Start(int port = 39175)
        {
            FiddlerApplication.Startup(port, FiddlerCoreStartupFlags.ChainToUpstreamGateway);
            FiddlerApplication.BeforeRequest += SetSessionProxy;
            FiddlerApplication.AfterSessionComplete += AfterSessionComplete;

            Win32Helper.RefreshIESettings($"localhost:{port}");
        }

        private static void AfterSessionComplete(Session oSession)
        {
            if (oSession.PathAndQuery.StartsWith("/kcsapi") && oSession.oResponse.MIMEType.Equals("text/plain"))
            {
                Models.Status.Current.StatusText = "已读取" + oSession.url;
                //Thread th = new Thread(new ParameterizedThreadStart(Distribute));
                //th.Start(oSession);
                oSession.utilDecodeResponse();
                Distribute(oSession);
            }
        }

        private static void SetSessionProxy(Session oSession)
        {
            if (IsSessionHTTPS(oSession))
            {
                if (Config.Current.EnableProxyHTTPS && Config.Current.HTTPSProxy != null)
                {
                    oSession["X-OverrideGateway"] = $"[{Config.Current.HTTPSProxy.Host}]:{Config.Current.HTTPSProxy.Port}";
                    return;
                }
            }
            if (Proxy != null && Config.Current.EnableProxy)
            {
                oSession["X-OverrideGateway"] = $"[{Proxy.Host}]:{Proxy.Port}";
            }
        }

        public static bool IsSessionHTTPS(Session oSession)
        {
            if (oSession.isHTTPS) return true;
            if (oSession.url.Contains(":443")) return true;
            return false;
        }

        public static void Stop()
        {
            FiddlerApplication.BeforeRequest -= SetSessionProxy;
            FiddlerApplication.AfterSessionComplete -= AfterSessionComplete;
            FiddlerApplication.Shutdown();
        }

        private static void Distribute(object o)
        {
            Session oSession = o as Session;
            //System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
            foreach(string key in apisource.Keys)
            {
                if (oSession.PathAndQuery.EndsWith(key))
                {
                    var Handlers = apisource[key].Handler;
                    //Handlers[key].BeginInvoke(oSession, null, null);
                    foreach(var Handler in Handlers.GetInvocationList())
                    {
                        ExceptionCatcherDelegate.BeginInvoke(Handler as Action<Session>, oSession, null, null);
                    }
                    //ExceptionCatcherDelegate.BeginInvoke(Handlers[key], oSession, null, null);
                }
            }
        }

        private static readonly Action<Action<Session>, Session> ExceptionCatcherDelegate = ExceptionCatcher;
        private static void ExceptionCatcher(Action<Session> action, Session parameter)
        {
#if DEBUG == false
            try
            {
#endif
                action(parameter);
#if DEBUG == false
            }
            catch(Exception ex)
            {
                Current.Dispatcher.Invoke(() => System.Windows.MessageBox.Show(ex.StackTrace, ex.Message));
            }
#endif
        }
        public Admiral Admiral { get; } = new Admiral();
        public Homeport Homeport { get; } = new Homeport();
        public MasterData MasterData { get; } = new MasterData();
        public System.Timers.Timer Ticker { get; } = new System.Timers.Timer(1000) { Enabled = true };
        public Shipyard Shipyard { get; } = new Shipyard();
        public QuestManager Quests { get; } = new QuestManager();
        public class APIObservable
        {
            public Action<Session> Handler;
            public void Subscribe(Action<Session> handler) => Handler += handler;
            public void Subscribe<T>(Action<T> handler) => Subscribe(x => handler(x.Parse<T>().Data));
            public void Subscribe(Action<NameValueCollection> handler) => Subscribe(x => handler(x.Parse().Request));
            public void Subscribe<T>(Action<NameValueCollection, T> handler) => Subscribe(x =>
            {
                var svdata = x.Parse<T>();
                handler(svdata.Request, svdata.Data);
            });
            public SubObservable<T> Where<T>(Func<T, bool> selector) => new SubObservable<T>() { Parent = this, Selector = selector };
        }
        public class SubObservable<T>
        {
            public APIObservable Parent { get; set; }
            public Func<T,bool> Selector { get; set; }
            public void Subscribe(Action<T> handler) => Parent.Subscribe<T>(x => { if (Selector(x)) handler(x); });
        }
    }
}
