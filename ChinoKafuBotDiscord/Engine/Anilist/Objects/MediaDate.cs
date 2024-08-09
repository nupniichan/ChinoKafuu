﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChinoBot.Engine.Anilist.Objects
{
    public class MediaDate
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? day { get; set; }
    }

    public class StartDate : MediaDate
    {
    }

    public class EndDate : MediaDate
    {
    }
}
