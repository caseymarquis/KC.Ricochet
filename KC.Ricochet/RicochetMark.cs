using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KC.Ricochet
{
    public class RicochetMark : System.Attribute
    {
        public RicochetMark(params string[] textValues) {
            textValues = textValues ?? new string[] { };
            this.TextValues = textValues;
        }

        public string[] TextValues { get; set; }
    }
}
