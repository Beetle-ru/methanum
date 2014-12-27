using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBFlex {
    interface IPatternGetter {
        void SetSource(string src);
        string GetPattern(string patternName);
    }
}
