using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace MyTranslateExtension.Model
{
    internal class IntegerSetting : Setting<int>
    {
        public override Dictionary<string, object> ToDictionary()
        {
            throw new NotImplementedException();
        }

        public override string ToState()
        {
            throw new NotImplementedException();
        }

        public override void Update(JsonObject payload)
        {
            throw new NotImplementedException();
        }
    }
}
