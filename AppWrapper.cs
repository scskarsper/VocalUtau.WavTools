using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VocalUtau.WavTools
{
    public class AppWrapper
    {
        public enum WrapperType
        {
            Pipe_Client,
            Pipe_Server,
            Buffer_Player,
            Wave_Appender,
            Args_Parser
        }
        object retObject = null;
        AppDomain Domain = null;
        string AsName = "";
        string CsName = "";
        public AppWrapper(string AppDomainFriendlyName,WrapperType ClassType)
        {
            try
            {
                Domain = AppDomain.CreateDomain(AppDomainFriendlyName);
                string TN = "VocalUtau.WavTools.Model.Pipe.Pipe_Client";
                switch (ClassType)
                {
                    case WrapperType.Args_Parser: TN = "Model.Args.ArgsParser"; break;
                    case WrapperType.Buffer_Player: TN = "Model.Player.BufferedPlayer"; break;
                    case WrapperType.Pipe_Client: TN = "Model.Pipe.Pipe_Client"; break;
                    case WrapperType.Pipe_Server: TN = "Model.Pipe.Pipe_Client"; break;
                    case WrapperType.Wave_Appender: TN = "Model.Wave.WavAppender"; break;
                }
                AsName = "VocalUtau.WavTools";
                CsName = AsName + "." + TN;
            }
            catch { ;}
        }
        private const BindingFlags bfi = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
        public object CreateObject(object[] args)
        {
            try
            {
                retObject = Domain.CreateInstance(AsName, CsName, false, bfi, null, args, null, null).Unwrap();
            }
            catch { ;}
            return retObject;
        }
        public T getInstanceObject<T>()
        {
                return (T)retObject;
        }
        ~AppWrapper()
        {
            Unload();
        }
        public void Unload()
        {
            try
            {
                AppDomain.Unload(Domain);
            }
            catch { ;}
            retObject = null;
        }
    }
}
