using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace OpenNETCF.MQTT
{
    public interface IVariableHeader
    {
        byte[] Serialize();
    }

    public class VariableHeader<T> : IVariableHeader
        where T : HeaderData, new()
    {
        public T HeaderData { get; set; }


        public VariableHeader()
        {
            HeaderData = new T();
        }

        public byte[] Serialize()
        {
            var data = new List<byte>();

            data.AddRange(HeaderData.Serialize());

            return data.ToArray(); ;
        }
    }
}
